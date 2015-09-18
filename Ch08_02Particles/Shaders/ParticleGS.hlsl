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
#include "Particle.hlsl"

// Generate a quad for each point (particle)
[maxvertexcount(4)]
void PointToQuadGS(point GS_Input p[1], inout TriangleStream<PS_Input> outStream)
{
    // Create billboard (quad always facing the camera)
    float3 toEye = CameraPosition.xyz - p[0].Position.xyz;
    float3 up    = float3(0.0f, 1.0f, 0.0f);
    float3 right = normalize(cross(toEye, up));
    up           = normalize(cross(toEye, right));

    //  2  0 
    //  |\ |   Triangle strip layout
    //  | \|   p.Position represents center point
    //  3  1
    // Build triangle strip around center point
    float radius = p[0].Radius;
    float3 center = p[0].Position.xyz;
    float3 rightR = right * radius;
    float3 upR = up * radius;

    PS_Input p1 = (PS_Input)0;
    p1.Energy = p[0].Energy;
    // Point 0
    p1.Position = float4(center+rightR+upR, 1);
    p1.UV = float2(1.0f, 0.0f);
    p1.Position = mul(p1.Position, WorldViewProjection);
    outStream.Append(p1);
    // Point 1
    p1.Position = float4(center+rightR-upR, 1);
    p1.UV = float2(1.0f, 1.0f);
    p1.Position = mul(p1.Position, WorldViewProjection);
    outStream.Append(p1);
    // Point 2
    p1.Position = float4(center-rightR+upR, 1);
    p1.UV = float2(0.0f, 0.0f);
    p1.Position = mul(p1.Position, WorldViewProjection);
    outStream.Append(p1);
    // Point 3
    p1.Position = float4(center-rightR-upR, 1);
    p1.UV = float2(0.0f, 1.0f);
    p1.Position = mul(p1.Position, WorldViewProjection);
    outStream.Append(p1);
    // End of strip
    outStream.RestartStrip();
}