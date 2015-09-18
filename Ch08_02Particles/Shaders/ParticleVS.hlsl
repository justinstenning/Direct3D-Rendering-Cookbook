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

// Access to the particle buffer
StructuredBuffer<Particle> particles : register(t0);

GS_Input VSMain(in uint vertexID : SV_VertexID)
{
    GS_Input result = (GS_Input)0;
    // Load particle using vertex Id
    Particle p = particles[vertexID];
    
    result.Position = float4(p.Position, 1); //float4(0,0,0, 1);
    result.Radius = p.Radius;
    result.Energy = p.Energy;
    return result;
}

#include "Common.hlsl"

static const float4 vertexUVPos[4] =
{
    { 0.0, 1.0, -1.0, -1.0 },
    { 0.0, 0.0, -1.0, +1.0 },
    { 1.0, 1.0, +1.0, -1.0 },
    { 1.0, 0.0, +1.0, +1.0 },
};

float4 ComputePosition(in float3 pos, in float size, in float2 vPos)
{
    // Create billboard (quad always facing the camera)
    float3 toEye = normalize(CameraPosition.xyz - pos);
    float3 up    = float3(0.0f, 1.0f, 0.0f);
    float3 right = cross(toEye, up);
    up           = cross(toEye, right);
    pos += (right * size * vPos.x) + (up * size * vPos.y);
    return mul(float4(pos, 1), WorldViewProjection);
}

PS_Input VSMainInstance(in uint vertexID : SV_VertexID, in uint instanceID : SV_InstanceID)
{
    PS_Input result = (PS_Input)0;

    // Load particle using vertex instance Id
    Particle p = particles[instanceID];
    // Vertex strip layout
    // 0-1
    //  /
    // 2-3
    result.UV = vertexUVPos[vertexID].xy;
    result.Position = ComputePosition(p.Position, p.Radius, vertexUVPos[vertexID].zw);
    //result.UV = float2( vertexID & 1, (vertexID & 2) >> 1 );
    //result.Position = ComputePosition(p.Position, p.Radius, result.UV * float2(2, -2) + float2(-1, 1));
    result.Energy = p.Energy;
    return result;
}