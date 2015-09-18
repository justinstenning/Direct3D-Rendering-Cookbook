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

// Implementation based on the Gerstner Wave formula from
// The elements of nature: interactive and realistic techniques - "Simulating Ocean Water - Jerry Tessendorf"
// http://dl.acm.org/citation.cfm?id=1103900.1103932&coll=DL&dl=GUIDE&CFID=240542608&CFTOKEN=74987873
void GerstnerWaveTessendorf(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float3 position, inout float3 result, inout float3 normal, inout float3 tangent)
{
    float L = waveLength; // wave crest to crest length in metres
    float A = amplitude; // amplitude - wave height (crest to trough)
    float k = 2.0 * 3.1416 / L; // wave length
    float kA = k*A;
    float2 D = normalize(direction); // normalized direction
    float2 K = D * k; // wave vector and magnitude (direction)

    // peak/crest steepness high means steeper, but too much 
    // can cause the wave to become inside out at the top
    float Q = steepness;//max(steepness, 0.1); 

    // Original formula, however is more difficult to control speed
    //float w = sqrt(9.82*k); // frequency (speed)
    //float wt = w*Time;
    
    float S = speed * 0.5; // Speed 1 =~ 2m/s so halve first
    float w = S * k; // Phase/frequency
    float wT = w * Time;

    // Unoptimized:
    // float2 xz = position.xz - K/k*Q*A*sin(dot(K,position.xz)- wT);
    // float y = A*cos(dot(K,position.xz)- wT);

    // Calculate once instead of 4 times
    float KPwT = dot(K, position.xz)-wT;
    float S0 = sin(KPwT);
    float C0 = cos(KPwT);

    // Calculate the vertex offset along the X and Z axes
    float2 xz = position.xz - D*Q*A*S0;
    // Calculate the vertex offset along the Y (up/down) axis
    float y = A*C0;

    // Calculate the tangent/bitangent/normal
    // Bitangent
    float3 B = float3(
        1-(Q * D.x * D.x * kA * C0),
        D.x * kA * S0,
        -(Q*D.x * D.y * kA * C0));
    // Tangent
    float3 T = float3(
        -(Q * D.x * D.y * kA * C0),
        D.y * kA * S0,
        1-(Q*D.y * D.y * kA * C0)
        );

    B = normalize(B);
    T = normalize(T);
    float3 N = cross(T, B);

    // Append the results
    result.xz += xz;
    result.y += y;
    normal += N;
    tangent += T;
}

// Alternative formula as described within GPU Gems
// The quality is not as good as the Tessendorf version
void GerstnerWave(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float3 position, inout float3 result, inout float3 normal, inout float3 tangent)
{
    // http://http.developer.nvidia.com/GPUGems/gpugems_ch01.html
    float L = waveLength;//4.0; // wave crest to crest length: 2m
    float S = speed;//0.5; // wave speed 0.2m/s (the distance the crest moves forward per second)
    float A = amplitude;//0.2; // amplitude (the height of the water plane to the wave crest)
    float2 D = normalize(direction);//float2(1, 1); // horizontal direction along which crest travels
    float w = 2.0 * 3.1416 / L; // frequency (speed)
    float p = S * w; // Phase
    float pT = p*Time;
    float Q = (steepness / w * A); // steepness (Qi/wi*Ai where Qi is between 0-1)
    
    float3 P;
    P.xz = Q*A * D * cos(w * dot(D, position.xz) + pT);
    P.y = A * sin(w * dot(D, position.xz) + pT);
    
    // Calculate partial derivatives for bitangent/tangent/normal vectors
    float wA = w*A;
    float S0 = sin(w * dot(D, position.xz) + pT);
    float C0 = cos(w * dot(D, position.xz) + pT);
    // Bitangent
    float3 B = float3(
        1-(Q * D.x * D.x * wA * S0),
        D.x * wA * C0,
        -(Q*D.x * D.y * wA * S0));
    // Tangent
    float3 T = float3(
        -(Q * D.x * D.y * wA * S0),
        D.y * wA * C0,
        1-(Q*D.y * D.y * wA * S0)
        );
    // Normal
    //float3 N = float3(
    //    -(D.x * wA * C0),
    //    1-(Q * wA * S0),
    //    -(D.y * wA * C0)
    //    );
    
    B = normalize(B);
    T = normalize(T);
    float3 N = cross(T, B);

    // Results;
    result += P;
    normal += N;
    tangent += T;
}

// Vertex shader main function
PixelShaderInput VSMain(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Change the position vector to be 4 units for matrix transformation
    vertex.Position.w = 1.0;

    float3 N = (float3)0; // normal
    float3 T = (float3)0; // tangent
    float3 waveOffset = (float3)0; // vertex xyz offset
    float2 direction = float2(1, 0);

    // GerstnerWaveTessendorf(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float4 position, inout float3 result, inout float3 normal, inout float3 tangent)

    // Single wave
    //GerstnerWaveTessendorf(10, 2, 2.5, 0.5, direction, vertex.Position, waveOffset, N, T);

    // Gentle ocean waves
    GerstnerWaveTessendorf(8, 0.5, 0.3, 1, direction, vertex.Position, waveOffset, N, T);
    GerstnerWaveTessendorf(4, 0.5, 0.4, 1, direction + float2(0, 0.5), vertex.Position, waveOffset, N, T);
    GerstnerWaveTessendorf(3, 0.5, 0.3, 1, direction + float2(0, 1), vertex.Position, waveOffset, N, T);
    GerstnerWaveTessendorf(2.5, 0.5, 0.2, 1, direction, vertex.Position, waveOffset, N, T);

    // Choppy ocean waves
    //GerstnerWaveTessendorf(10, 2, 2.5, 0.5, direction, vertex.Position.xyz, waveOffset, N, T);
    //GerstnerWaveTessendorf(5, 1.2, 2, 1, direction, vertex.Position.xyz, waveOffset, N, T);
    //GerstnerWaveTessendorf(4, 2, 2, 1, direction + float2(0, 1), vertex.Position.xyz, waveOffset, N, T);
    //GerstnerWaveTessendorf(4, 1, 0.5, 1, direction + float2(0, 1), vertex.Position.xyz, waveOffset, N, T);
    //GerstnerWaveTessendorf(2.5, 2, 0.5, 1, direction + float2(0, 0.5), vertex.Position.xyz, waveOffset, N, T);
    //GerstnerWaveTessendorf(2, 2, 0.5, 1, direction, vertex.Position.xyz, waveOffset, N, T);

    // Alternative algorithm (more rounded waves)
    //GerstnerWave(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float4 position, inout float3 result, inout float3 normal, inout float3 tangent)
    //GerstnerWave(8, 0.5, 0.1, 1, direction, vertex.Position, waveOffset, N, T);
    //GerstnerWave(4, 0.5, 0.2, 0.5, direction + float2(0, 0.5), vertex.Position, waveOffset, N, T);
    //GerstnerWave(2.5, 0.5, 0.15, 0.2, direction + float2(0, 0.5), vertex.Position, waveOffset, N, T);
    //GerstnerWave(2, 0.5, 0.05, 0, direction, vertex.Position, waveOffset, N, T);
    
    vertex.Position.xyz += waveOffset;
    vertex.Normal = normalize(N);
    vertex.Tangent.xyz = normalize(T);

    result.Position = mul(vertex.Position, WorldViewProjection);
    result.Diffuse = vertex.Color * MaterialDiffuse;
    // Apply material UV transformation
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;
    
    
    // We use the inverse transpose of the world so that if there is non uniform
    // scaling the normal is transformed correctly. We also use a 3x3 so that 
    // the normal is not affected by translation (i.e. a vector has the same direction
    // and magnitude regardless of translation)
    result.WorldNormal = mul(vertex.Normal, (float3x3)WorldInverseTranspose);
    result.WorldTangent = float4(mul(vertex.Tangent.xyz, (float3x3)WorldInverseTranspose), vertex.Tangent.w);

    result.WorldPosition = mul(vertex.Position, World).xyz;
    
    
    
    return result;
}