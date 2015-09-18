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

Texture2D Texture0 : register(t0);
Texture2D NormalMap: register(t1);
Texture2D SpecularMap: register(t2);
SamplerState Sampler : register(s0);

#include "Common.hlsl"

// From Vertex shader to PSFillGBuffer
struct GBufferPixelIn
{
    float4 Position : SV_Position;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV: TEXCOORD0;
    
    // We need the WorldNormal for displacement etc..
    float3 ViewNormal : TEXCOORD1;

    // tangent vector
    float4 ViewTangent : TANGENT;
};

// Pixel Shader output structure (from Pixel Shader)
struct GBufferOutput
{
    float4 Target0 : SV_Target0;
    uint   Target1 : SV_Target1;
    float4 Target2 : SV_Target2;
    //float4 Target3 : SV_Target3;
    // | -----------32 bits-----------|
    // | Diffuse (RGB)  | SpecInt (A) | RT0
    // | Packed Normal--------------->| RT1
    // | Emissive (RGB) | SpecPwr (A) | RT2
};

GBufferPixelIn VSFillGBuffer(VertexShaderInput vertex)
{
    GBufferPixelIn result = (GBufferPixelIn)0;

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
    // Transform normal/tangent into world view-space
    result.ViewNormal = mul(vertex.Normal, (float3x3)WorldInverseTranspose);
    result.ViewNormal = mul(result.ViewNormal, (float3x3)View);
    result.ViewTangent = float4(mul(vertex.Tangent.xyz, (float3x3)WorldInverseTranspose), vertex.Tangent.w);
    result.ViewTangent.xyz = mul(result.ViewTangent.xyz, (float3x3)View);

    return result;
}


// Lambert azimuthal equal-area projection
// http://en.wikipedia.org/wiki/Lambert_azimuthal_equal-area_projection
// as implemented @ http://aras-p.info/texts/CompactNormalStorage.html
float2 EncodeAzimuthal(in float3 N)
{
    // Lambert azimuthal equal-area projection
    // with normalized N is equivalent to 
    // Spheremap Transform but slightly faster
    float f = sqrt(8*N.z+8);
    return N.xy / f + 0.5;
}

uint PackNormal(in float3 N)
{
    float2 encN = EncodeAzimuthal(N);
    // Pack float2 into uint
    uint result = 0;
    result = f32tof16(encN.x);
    result |= f32tof16(encN.y) << 16;
    return result;
}

GBufferOutput PSFillGBuffer(GBufferPixelIn pixel)
{
    // Normalize our vectors as they are not 
    // guaranteed to be unit vectors after interpolation
    float3 normal = normalize(pixel.ViewNormal);

    // If there is a normal map, apply it
    if (HasNormalMap)
        normal = ApplyNormalMap(normal, pixel.ViewTangent, NormalMap.Sample(Sampler, pixel.TextureUV).rgb);

    // Texture sample here (use white if no texture)
    float4 sample = (float4)1.0;
    if (HasTexture)
        sample = Texture0.Sample(Sampler, pixel.TextureUV);

    float specIntensity = 1.0f;
    if (HasSpecMap)
        specIntensity = SpecularMap.Sample(Sampler, pixel.TextureUV).r;
    else
        specIntensity = dot(MaterialSpecular.rgb, float3(0.2125, 0.7154, 0.0721));

    float3 diffuse = saturate(MaterialAmbient+pixel.Diffuse) * sample.rgb;

    GBufferOutput result = (GBufferOutput)0;
    result.Target0.xyz = diffuse;
    result.Target0.w = specIntensity;
    result.Target1 = PackNormal(normal);
    result.Target2.xyz = MaterialEmissive.rgb;
    result.Target2.w = MaterialSpecularPower / 50; // Normalized to 0-50 range

    // Return result
    return result;
}