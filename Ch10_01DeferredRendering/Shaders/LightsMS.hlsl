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

struct LightStruct
{
    float3 Direction;
    uint Type; // 0=Ambient, 1=Direction, 2=Point, 3=TODO:Spot
    
    float3 Position;
    float Range;
    
    float3 Color;
    
    //float ColorA;
    //float SpotInnerCosine;
    //float SpotOuterCosine;
};

cbuffer PerLight : register(b4)
{
    LightStruct LightParams;
};

struct VertexIn
{
    float4 Position : SV_Position;
};

struct PixelIn
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

PixelIn VSLight(VertexShaderInput vertex)
{
    PixelIn result = (PixelIn)0;

    vertex.Position.w = 1.0f;
    
    result.Position = mul(vertex.Position, WorldViewProjection);

    // Determine UV from device coords
    result.UV.xy = result.Position.xy / result.Position.w;
    // The UV coordinates are top-left 0,0 bottom-right 1,1
    result.UV.x = result.UV.x * 0.5 + 0.5;
    result.UV.y = result.UV.y * -0.5 + 0.5;

    return result;
}

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

    float depth = t3.Load(screenPos, sampleIndex);
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

static const float pi = 3.14159265f;

float DiffuseConservation()
{
    return 1.0 / pi;
}
float SpecularConservation(float power)
{
    // http://www.rorydriscoll.com/2009/01/25/energy-conservation-in-games/
    return 0.0397436 * power + 0.0856832;
    //return (power+8)/(8*pi);
}

// Basic Lambert and BlinnPhong light contribution
float3 LightContribution(GBufferAttributes attrs, float3 V, float3 L, float3 H, float3 D, float attenuation)
{
    float NdotL = saturate(dot(attrs.Normal, L));
    if (NdotL <= 0) {
        discard;
	return 0;
    }
    float NdotH = saturate(dot(attrs.Normal, H));
    // Lambert diffuse
    float3 diffuse = NdotL * LightParams.Color * attrs.Diffuse;// * DiffuseConservation();
    // BlinnPhong specular term
    float specPower = max(attrs.SpecularPower,0.00001f);
    float3 specular = pow(NdotH, specPower) * attrs.SpecularInt * LightParams.Color;// * SpecularConservation(specPower);

    //return specular;
    //return H;//LightParams.Color * attrs.Diffuse;
    return (diffuse + specular) * attenuation + attrs.Emissive;
}

// Prepares the common LightContribution inputs
void PrepareLightInputs(in float3 camera, in float3 position, in float3 N, in LightStruct light,
                        out float3 V, out float3 L, out float3 H, out float D, out float attenuation)
{
    V = camera - position;
    L = light.Position - position;
    D = length(L);
    //float3 D2 = dot(L, L);
    L /= D;
    H = normalize(L + V);
    //float range2 = 1/light.Range*light.Range;
    //range2 *= range2;
    //// attenuation falloff - unrealistic but easy
    //attenuation = max(1-D2 * range2, 0);
    //attenuation *= attenuation;
    
    // Simple attenuation
    attenuation = max(1-D/light.Range, 0);
    attenuation *= attenuation;
}

float3 ComputeSpotLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3)0;
    
    float3 V, L, H;
    float D, attenuation, NdotL, NdotH;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    // Apply additional spotlight falloff for cone
    // http://msdn.microsoft.com/en-us/library/windows/desktop/bb174697(v=vs.85).aspx
    // pow(dot(-L,LDir)-cos(Phi)/2 / cos(Theta)/2-cos(Phi)/2, falloff) <- where falloff usually 1.0 so ignored
    //attenuation *= saturate((dot(-L, LightParams.Direction)-LightParams.SpotInnerCosine)/(LightParams.SpotOuterCosine/2-LightParams.SpotInnerCosine));

    return LightContribution(attrs, V, L, H, D, attenuation);
}

float3 ComputePointLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3)0;
    
    float3 V, L, H;
    float D, attenuation, NdotL, NdotH;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    return LightContribution(attrs, V, L, H, D, attenuation);
}

float3 ComputeDirectionLight(GBufferAttributes attrs, float3 camPos)
{
    float3 result = (float3)0;
    
    float3 V, L, H;
    float D, attenuation;
    PrepareLightInputs(camPos, attrs.Position, attrs.Normal, LightParams,
        V, L, H, D, attenuation);

    L = normalize(-LightParams.Direction);
    H = normalize(L + V);
    attenuation = 1.0f;
    //return attrs.Position;
    return LightContribution(attrs, V, L, H, D, attenuation);
}

float4 PSSpotLight(in PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
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

        return float4(ComputeSpotLight(attrs, (float3)0), 1.0f);
    }
    discard;
    return 0;
}

float4 PSPointLight(in PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
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

        float3 V, L, H;
        float D, attenuation;
        PrepareLightInputs((float3)0, attrs.Position, attrs.Normal, LightParams,
            V, L, H, D, attenuation);

        return float4(LightContribution(attrs, V, L, H, D, attenuation), 1.0f);
    }
    discard;
    return 0;
}

float4 PSDirectionalLight(in PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
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

        float3 V, L, H;
        float D, attenuation;
        PrepareLightInputs((float3)0, attrs.Position, attrs.Normal, LightParams,
            V, L, H, D, attenuation);

        L = normalize(-LightParams.Direction);
        H = normalize(L + V);
        attenuation = 1.0f;

        return float4(LightContribution(attrs, V, L, H, D, attenuation), 1.0f);
    }
    discard;
    return 0;
}

float4 PSAmbientLight(in PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
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

        return float4(attrs.Diffuse * LightParams.Color, 1.0f);
    }
    discard;
    return 0;
}

float4 PSDebugLight(in PixelIn pixel, uint coverage: SV_Coverage, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    float4 result = (float4)0;
    result.xyz = Light.Color;
    result.w = 1.0f;
    return result;
}