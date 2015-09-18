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

// Per frame particle system constants
cbuffer ParticleConstants : register(b0)
{
    float3 DomainBoundsMin;
    float ForceStrength;
    float3 DomainBoundsMax;
    float MaxLifetime;
    float3 ForceDirection;
    uint MaxParticles;
    float3 Attractor;
    float Radius;
};

cbuffer ParticleFrame : register(b1)
{
    float Time;
    float FrameTime;
    uint RandomSeed;
    uint ParticleCount;
}

//cbuffer ParticleCount : register(b2)
//{
//    uint ParticleCount;
//};



// Represents a particle
struct Particle {
    float3 Position;
    float Radius;
    float3 OldPosition;
    float Energy;
};


// Pixel shader input
struct PS_Input {
    float4 Position : SV_Position;
    float2 UV: TEXCOORD0;
    float Energy: ENERGY;
};

// Geometry shader input
struct GS_Input {
    float4 Position : SV_Position;
    float Radius : RADIUS;
    float Energy : ENERGY;
};