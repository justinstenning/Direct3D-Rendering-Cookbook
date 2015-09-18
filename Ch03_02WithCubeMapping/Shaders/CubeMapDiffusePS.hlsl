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

// Globals for texture sampling
TextureCube CubeMap : register(t0);
SamplerState SamplerClamp : register(s0);

#include "Common.hlsl"

// A basic diffuse lighting pixel shader
float4 PSMain(PixelShaderInput pixel) : SV_Target
{
    // Important: After interpolation the values are not necessarily normalized
    float3 normal = normalize(pixel.WorldNormal);
    float3 toEye = normalize(CameraPosition - pixel.WorldPosition);
    float3 toLight = normalize(-Light.Direction);
    
    // Texture sample here (use white if no texture)
    float4 sample = CubeMap.Sample(SamplerClamp, normal);

    float3 ambient = MaterialAmbient.rgb;
    float3 emissive = MaterialEmissive.rgb;
    float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);

    // Calculate final color component
    float3 color = (saturate(ambient+diffuse) * sample.rgb) * Light.Color.rgb + emissive;
    // We saturate ambient+diffuse to ensure there is no over-
    // brightness on the texture sample if the sum is greater than 1
    
    // Calculate final alpha value
    float alpha = pixel.Diffuse.a * sample.a;

    // Return result
    return float4(color, alpha);
}