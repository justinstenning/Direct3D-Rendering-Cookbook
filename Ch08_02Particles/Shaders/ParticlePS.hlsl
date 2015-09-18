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

#include "Particle.hlsl"

// Particle texture
Texture2D ParticleTexture : register(t0);
// Bilinear sampler
SamplerState linearSampler : register(s0);

float4 PSMain(PS_Input pixel) : SV_Target
{
    float4 result = ParticleTexture.Sample(linearSampler, pixel.UV);
    
    //// Cycle colors over time
    //result.x = result.x * (cos(0.2*pixel.Energy)*0.5+0.5) + 0.2;
    //result.y = result.y * (cos(0.6*pixel.Energy)*0.5+0.5) + 0.6;
    //result.z = result.z * (sin(0.3*pixel.Energy)*0.5+0.5) + 0.3;

    return float4(result.xyz, saturate(pixel.Energy) * result.w * pixel.Position.z * pixel.Position.z);
}