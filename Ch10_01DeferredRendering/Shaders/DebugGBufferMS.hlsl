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

Texture2DMS<float4> Texture0 : register(t0);
Texture2DMS<uint> Texture1 : register(t1);
Texture2DMS<float4> Texture2 : register(t2);
Texture2DMS<float> TextureDepth : register(t3);

#include "Common.hlsl"
#include "GBuffer.hlsl"

struct PixelIn
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
};

struct GBufferAttributes
{
    float3 Position;
    float3 Normal;
    float3 Diffuse;
    float SpecularInt; // specular intensity
    float3 Emissive;
    float SpecularPower;
};

void ExtractGBufferAttributes(in PixelIn pixel, 
                            in Texture2DMS<float4> t0,
                            in Texture2DMS<uint> t1,
                            in Texture2DMS<float4> t2,
                            in Texture2DMS<float> t3,
                            in int sampleIndex,
                           out GBufferAttributes attrs)
{
    int3 screenPos = int3(pixel.Position.xy, 0);

    float depth = t3.Load(screenPos, sampleIndex).x;
    attrs.Diffuse = t0.Load(screenPos, sampleIndex).xyz;
    attrs.SpecularInt = t0.Load(screenPos, sampleIndex).w;
    attrs.Normal = UnpackNormal(t1.Load(screenPos, sampleIndex));
    attrs.Emissive = t2.Load(screenPos, sampleIndex).xyz;
    attrs.SpecularPower = t2.Load(screenPos, sampleIndex).w * 50;

    // http://mynameismjp.wordpress.com/2009/03/10/reconstructing-position-from-depth/
    // Calculate projected position from viewport position and depth
    // Convert UV coords to normalized device coordinates
    float x = pixel.UV.x * 2 - 1;
    float y = (1 - pixel.UV.y) * 2 - 1;
    // Unproject -> transform by inverse projection
    float4 posVS = mul(float4(x, y, depth, 1.0f), InverseProjection);  
    // Perspective divide to get the final view-space position
    attrs.Position = posVS.xyz / posVS.w;
}

float4 GBufferDiffuse(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4(attrs.Diffuse, 1.0f);
    }
    discard;
    return 0;
}

float4 GBufferNormalPacked(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4(attrs.Normal, 1.0f);
    }
    discard;
    return 0;
}

float4 GBufferSpecularInt(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;

    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4((float3)attrs.SpecularInt, 1.0f);
    }
    discard;
    return 0;
}

float4 GBufferEmissive(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4(attrs.Emissive, 1.0f);
    }
    discard;
    return 0;
}

float4 GBufferSpecularPower(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4((float3)attrs.SpecularPower/50, 1.0f);
    }
    discard;
    return 0;
}

float4 GBufferDepth(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        int3 screenPos = int3(pixel.Position.xy, 0);
        return float4(TextureDepth.Load(screenPos, sampleIndex).x, 0, 0, 1.0);
    }
    discard;
    return 0;    
}

float4 GBufferPosition(PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    GBufferAttributes attrs;
    // Is sample covered
    if (coverage & (1 << sampleIndex))
    {
        ExtractGBufferAttributes(pixel,
            Texture0, Texture1,
            Texture2, TextureDepth,
            sampleIndex,
            attrs);

        return float4(attrs.Position, 1.0f);
    }
    discard;
    return 0;
}

// Example of using a loop instead of SV_SampleIndex
//#define MULTISAMPLECOUNT 4
//float4 GBufferPositionMSLoop(PixelIn pixel, uint coverage: SV_Coverage) : SV_Target
//{
//    GBufferAttributes attrs;
//    float3 position = (float3)0;
//    int samplesUsed = 0;
//    [unroll]
//    for (int i = 0; i < MULTISAMPLECOUNT; i++)
//    {
//        // Is sample covered
//        if (coverage & (1 << i))
//        {
//            ExtractGBufferAttributes(pixel,
//                Texture0, Texture1,
//                Texture2, TextureDepth,
//                i,
//                attrs);
//
//            position += attrs.Position;
//            samplesUsed++;
//        }
//    }
//    position /= samplesUsed;
//
//    return float4(position, 1.0f);
//}