// Copyright (c) 2013 Justin Stenning
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//-------------------------------
// IMPORTANT: When creating a new shader file use "Save As...", "Save with encoding", 
// and then select "Western European (Windows) - Codepage 1252" as the 
// D3DCompiler cannot handle the default encoding of "UTF-8 with signature"
//-------------------------------

#include "Common.hlsl"

// Cube map ViewProjections for each face
cbuffer PerEnvironmentMap : register(b4)
{
    float4x4 CubeFaceViewProj[6];
};

// Use the PixelShaderInput as the GeometryShaderInput structure
#define GeometryShaderInput PixelShaderInput

// Pixel Shader input structure (from CubeMap Geometry Shader)
struct GS_CubeMapOutput
{
    float4 Position : SV_Position;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV: TEXCOORD0;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;

    // Allows us to write to multiple render targets
    uint RTIndex : SV_RenderTargetArrayIndex;
};


// Vertex shader cubemap function
GeometryShaderInput VS_CubeMap(VertexShaderInput vertex)
{
    GeometryShaderInput result = (GeometryShaderInput)0;

    // Change the position vector to be 4 units for matrix transformation
    vertex.Position.w = 1.0;
    
    // SNIP: vertex skinning here

    // Only world transform
    result.Position = mul(vertex.Position, World);
    result.Diffuse = vertex.Color * MaterialDiffuse;
    // Apply material UV transformation
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;
    result.WorldNormal = mul(vertex.Normal, (float3x3)WorldInverseTranspose);
    result.WorldPosition = result.Position.xyz;
    
    
    return result;
}

[maxvertexcount(3)] // Outgoing vertex count (1 triangle)
[instance(6)] // Number of times to execute for each input
void GS_CubeMap(triangle GeometryShaderInput input[3], uint instanceId: SV_GSInstanceID, inout TriangleStream<GS_CubeMapOutput> stream)
{
    // Output the input triangle using the  View/Projection 
    // of the cube face identified by instanceId
    float4x4 viewProj = CubeFaceViewProj[instanceId];
    GS_CubeMapOutput output;

    // Assign the render target instance
    // i.e. 0 = +X face, 1 = -X face and so on
    output.RTIndex = instanceId;
    
    // In order to render correctly into a TextureCube we
    // must either:
    // 1) using a left-handed view/projection; OR
    // 2) using a right-handed view/projection with -1 X-
    //    axis scale
    // Our meshes assume a right-handed coordinate system
    // therefore both cases above require vertex winding
    // to be switched.
    uint3 indx = uint3(0,2,1);
    [unroll]
    for (int v = 0; v < 3; v++)
    {
        // Apply cube face view/projection
        output.Position = mul(input[indx[v]].Position, viewProj);
        // Copy other vertex properties as is
        output.WorldPosition = input[indx[v]].WorldPosition;
        output.Diffuse = input[indx[v]].Diffuse;
        output.WorldNormal = input[indx[v]].WorldNormal;
        output.TextureUV = input[indx[v]].TextureUV;

        // Append to the stream
        stream.Append(output);
    }
    stream.RestartStrip();
}

// Globals for texture sampling
Texture2D Texture0 : register(t0);
TextureCube Reflection : register(t1);
SamplerState Sampler : register(s0);

float4 PS_CubeMap(GS_CubeMapOutput pixel) : SV_Target
{
    // Normalize our vectors as they are not 
    // guaranteed to be unit vectors after interpolation
    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
    float3 toLight = normalize(-Light.Direction);

    // Texture sample here (use white if no texture)
    float4 sample = (float4)1.0;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float3 ambient = MaterialAmbient.rgb;
    float3 emissive = MaterialEmissive.rgb;
    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
    float3 specular = SpecularBlinnPhong(normal, toLight, toEye);

    // Calculate final color component
    float3 color = (saturate(ambient+diffuse) * sample.rgb + specular) * Light.Color.rgb + emissive;
    // We saturate ambient+diffuse to ensure there is no over-
    // brightness on the texture sample if the sum is greater than 1
    
    // Calculate reflection (if any)
    if (IsReflective) {
        float3 reflection = reflect(-toEye, normal);
        color = lerp(color, Reflection.Sample(Sampler, reflection).rgb, ReflectionAmount);
    }

    // Calculate final alpha value
    float alpha = pixel.Diffuse.a * sample.a;

    // Return result
    return float4(color, alpha);
}