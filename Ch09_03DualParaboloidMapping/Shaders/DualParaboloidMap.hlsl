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
// Adapted from code Copyright (c) 2003-2011 Jason Zink
//-------------------------------
//The MIT License
//
//Copyright (c) 2003-2011 Jason Zink
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
//-------------------------------
// IMPORTANT: When creating a new shader file use "Save As...", "Save with encoding", 
// and then select "Western European (Windows) - Codepage 1252" as the 
// D3DCompiler cannot handle the default encoding of "UTF-8 with signature"
//-------------------------------

#include "Common.hlsl"
#include "EnvironmentMap.hlsl"

struct GS_DualMapInput
{
    float4 Position : SV_Position;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV: TEXCOORD0;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
    
    // Normalized Z for Dual Paraboloid
    float DualMapZ: TEXCOORD4;
};

// Pixel Shader input structure (from CubeMap Geometry Shader)
struct GS_DualMapOutput
{
    float4 Position : SV_Position;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV: TEXCOORD0;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;

    // Normalized Z for Dual Paraboloid
    float DualMapZ: TEXCOORD4;

    // Allows us to write to multiple render targets
    uint RTIndex : SV_RenderTargetArrayIndex;
    // Allows specifying a viewport for each instance
    //uint VPIndex : SV_ViewportArrayIndex;
};

// Vertex shader DPM function
GS_DualMapInput VS_DualMap(VertexShaderInput vertex)
{
    GS_DualMapInput result = (GS_DualMapInput)0;

    // Change the position vector to be 4 units for matrix transformation
    vertex.Position.w = 1.0;
    result.Position = mul(vertex.Position, WorldViewProjection);
    result.Diffuse = vertex.Color * MaterialDiffuse;
    // Apply material UV transformation
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;
    
    // We use the inverse transpose of the world so that if there is non uniform
    // scaling the normal is transformed correctly. We also use a 3x3 so that 
    // the normal is not affected by translation (i.e. a vector has the same direction
    // and magnitude regardless of translation)
    result.WorldNormal = mul(vertex.Normal, (float3x3)WorldInverseTranspose);
    result.WorldPosition = mul(vertex.Position, World).xyz;
    
    
    // We are using the Paraboloid's view within the WorldViewProjection
    // with an Identity matrix for the projection. 
    
    // We are now relative to the DPM's view, the length of result.Position 
    // is distance from Paraboloid's origin to Position
    float L = length(result.Position); // length/distance => depth
    result.Position = result.Position / L; // normalize
    result.DualMapZ = result.Position.z; // Keep original normalized Z
    result.Position.z = (L - NearClip) / (FarClip - NearClip); // Scale depth to [0, 1]
    result.Position.w = 1.0f; // No perspective distortion

    return result;
}

[maxvertexcount(3)] // Outgoing vertex count (1 triangle)
[instance(2)] // Number of times to execute for each input
void GS_DualMap(triangle GS_DualMapInput input[3], uint instanceId: SV_GSInstanceID, inout TriangleStream<GS_DualMapOutput> stream)
{
    // Output the input triangle and calculate whether
    // the vertex is in the +ve or -ve half of the
    // Dual paraboloid map.
    GS_DualMapOutput output = (GS_DualMapOutput)0;

    // Assign the render target instance
    // i.e. 0 = 1st texture array, 1 = 2nd texture array
    output.RTIndex = instanceId;
    
    // Direction (front or back)
    float direction = 1.0f - instanceId*2;

    // Vertex winding
    uint3 indx = uint3(0,2,1);
    if (direction < 0)
        indx = uint3(0,1,2);

    [unroll]
    // for each input vertex
    for (int v = 0; v < 3; v++)
    {
        // Calculate the projection for the the DPM, taking 
        // into consideration which half of the DPM we are 
        // rendering.
        float projection = input[indx[v]].DualMapZ * direction + 1.0f;
        output.Position.xy = input[indx[v]].Position.xy / projection;
        output.Position.z = input[indx[v]].Position.z;
        output.Position.w = 1;
        output.DualMapZ = input[indx[v]].DualMapZ * direction;

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
SamplerState Sampler : register(s0);

float4 PS_DualMap(GS_DualMapOutput pixel) : SV_Target
{
    // Ignore this pixel if located behind
    // We have to add a little additional to ensure that the
    // two halves of the dual paraboloid meet at the seams.
    clip(pixel.DualMapZ + 0.4f);

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
        //color.rgb += SampleEnvMap(Sampler, reflection).rgb * ReflectionAmount;
        color = lerp(color, SampleEnvMap(Sampler, reflection), ReflectionAmount);
    }

    // Calculate final alpha value
    float alpha = pixel.Diffuse.a * sample.a;

    // Return result
    return float4(color, alpha);
}