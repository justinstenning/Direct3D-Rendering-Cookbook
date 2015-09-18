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

Texture2D<float4> Texture0 : register(t0);
Texture2D<uint> Texture1 : register(t1);
Texture2D<float4> Texture2 : register(t2);
Texture2D<float> TextureDepth : register(t3);

SamplerState Sampler : register(s0);

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
                            in Texture2D<float4> t0,
                            in Texture2D<uint> t1,
                            in Texture2D<float4> t2,
                            in Texture2D<float> t3,
                           out GBufferAttributes attrs)
{
    int3 screenPos = int3(pixel.Position.xy, 0);

    float depth = t3.Load(screenPos).x;
    attrs.Diffuse = t0.Load(screenPos).xyz;
    attrs.SpecularInt = t0.Load(screenPos).w;
    attrs.Normal = UnpackNormal(t1.Load(screenPos));
    attrs.Emissive = t2.Load(screenPos).xyz;
    attrs.SpecularPower = t2.Load(screenPos).w * 50;

    // http://mynameismjp.wordpress.com/2009/03/10/reconstructing-position-from-depth/
    // Calculate projected position from viewport position and depth
    // Convert UV coords to normalized device coordinates
    float x = pixel.UV.x * 2 - 1;
    float y = (1 - pixel.UV.y) * 2 - 1;
    // Unproject -> transform by inverse projection
    float4 posVS = mul(float4(x, y, depth, 1.0f), InverseProjection);  
    // Perspective divide to get the final view-space position
    attrs.Position = posVS.xyz / posVS.w;
    
    // Other methods for reconstruction of position from depth by MJP @ http://mynameismjp.wordpress.com/2010/09/05/position-from-depth-3/
    // These methods get you coordinates that are normalized to a Depth of 1.0f
    // View ray from camera (0,0,0) to current pixel on far clip plane
    //float3 viewRay = float3(pixel.PositionVS.xy / pixel.PositionVS.z, 1.0f);
    
    // Reconstruct the view-space position
    // Method 1: View Ray * -QZn / (depth - Q) 
    //           where Q => (Zf/Zf-Zn)
    
    // Method 1A: calculating Q and -QZn of projection matrix from Znear and Zfar.
    // http://msdn.microsoft.com/en-us/library/windows/desktop/bb147302(v=vs.85).aspx
    //float Q = -(100 / (100 - 2)); // right-handed
    //float negQZn = -(-Q * 2);     // right-handed
    //// Convert depth to linear view space Z
    //float linearDepth = negQZn / (depth - Q);
    //attrs.Position = pixel.Ray * linearDepth;

    // Method 1B: retrieve -QZn and Q from proj matrix
    //float linearDepth = Projection[3][2] / (depth - Projection[2][2]);
    //attrs.Position = viewRay * linearDepth;
}

float4 GBufferDiffuse(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel,
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    return float4(attrs.Diffuse, 1);
}

float4 GBufferNormalPacked(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, 
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);
    
    return float4(attrs.Normal, 1);
}

float4 GBufferSpecularInt(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, 
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    return float4((float3)attrs.SpecularInt, 1.0f);
}

float4 GBufferEmissive(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, 
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    return float4(attrs.Emissive, 1.0f);
}

float4 GBufferSpecularPower(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, 
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    return float4((float3)attrs.SpecularPower/50, 1.0f);
}

float4 GBufferDepth(PixelIn pixel) : SV_Target
{
    //GBufferAttributes attrs;
    //ExtractGBufferAttributes(pixel, 
    //    Texture0, Texture1,
    //    Texture2, TextureDepth,
    //    attrs);
    int3 screenPos = int3(pixel.Position.xy, 0);
    return float4(TextureDepth.Load(screenPos).r, 0, 0, 1.0);
}

float4 GBufferPosition(PixelIn pixel) : SV_Target
{
    GBufferAttributes attrs;
    ExtractGBufferAttributes(pixel, 
        Texture0, Texture1,
        Texture2, TextureDepth,
        attrs);

    return float4(attrs.Position, 1.0f);
}