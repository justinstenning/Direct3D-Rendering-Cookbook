// Copyright (c) 2013 Justin Stenning
// This software contains source code provided by NVIDIA Corporation.
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

struct HullShaderInput
{
    float3 WorldPosition : POSITION;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;
    // Interpolation of vertex UV texture coordinate
    float2 TextureUV: TEXCOORD0;
    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
};

// Max 32 outputs
struct DS_ControlPointInput {
    float3 Position : BEZIERPOS;
    float4 Diffuse : COLOR0;
};

// Max 32 outputs
struct HS_TrianglePatchConstant {
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;
    
    float2 TextureUV[3]: TEXCOORD0;
    float3 WorldNormal[3] : TEXCOORD3;
};

// Max 32 outputs
struct HS_QuadPatchConstant {
    float EdgeTessFactor[4] : SV_TessFactor;
    float InsideTessFactor[2] : SV_InsideTessFactor;

    float2 TextureUV[4]: TEXCOORD0;
    float3 WorldNormal[4] : TEXCOORD4;
};

// Max 32 outputs
struct HS_BezierPatchConstant {
    float EdgeTessFactor[4] : SV_TessFactor;
    float InsideTessFactor[2] : SV_InsideTessFactor;

    float2 TextureUV[16]: TEXCOORD0;
};

//*********************************************************
// TRIANGLE interpolation (using Barycentric coordinates)
/*
    barycentric.xyz == uvw
    w=1-u-v
    P=w*A+u*B+v*C
  C ______________ B
    \.    w    . /
     \  .    .  / 
      \    P   /
       \u  . v/
        \  . /
         \ ./
          \/
          A
*/
float2 BarycentricInterpolate(float2 v0, float2 v1, float2 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float2 BarycentricInterpolate(float2 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float3 BarycentricInterpolate(float3 v0, float3 v1, float3 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float3 BarycentricInterpolate(float3 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

float4 BarycentricInterpolate(float4 v0, float4 v1, float4 v2, float3 barycentric)
{
    return barycentric.z * v0 + barycentric.x * v1 + barycentric.y * v2;
}

float4 BarycentricInterpolate(float4 v[3], float3 barycentric)
{
    return BarycentricInterpolate(v[0], v[1], v[2], barycentric);
}

//*********************************************************
// QUAD bilinear interpolation
float2 Bilerp(float2 v[4], float2 uv)
{
    // bilerp the float2 values
    float2 side1 = lerp( v[0], v[1], uv.x );
    float2 side2 = lerp( v[3], v[2], uv.x );
    float2 result = lerp( side1, side2, uv.y );
	
    return result;    
}

float3 Bilerp(float3 v[4], float2 uv)
{
    // bilerp the float3 values
    float3 side1 = lerp( v[0], v[1], uv.x );
    float3 side2 = lerp( v[3], v[2], uv.x );
    float3 result = lerp( side1, side2, uv.y );
	
    return result;    
}

float4 Bilerp(float4 v[4], float2 uv)
{
    // bilerp the float4 values
    float4 side1 = lerp( v[0], v[1], uv.x );
    float4 side2 = lerp( v[3], v[2], uv.x );
    float4 result = lerp( side1, side2, uv.y );
	
    return result;    
}

//*********************************************************
// BEZIER bicubic interpolation (using de Casteljau's algorithm - u => t)
// Based on code Copyright (c) 2011 NVIDIA Corporation. All rights reserved.
// Calculate point upon Bezier curve, returning the point
void DeCasteljau(float u, float3 p0, float3 p1, float3 p2, float3 p3, out float3 p)
{
    float3 q0 = lerp(p0, p1, u);
    float3 q1 = lerp(p1, p2, u);
    float3 q2 = lerp(p2, p3, u);
    float3 r0 = lerp(q0, q1, u);
    float3 r1 = lerp(q1, q2, u);

    p = lerp(r0, r1, u);
}
// Used by PN-Triangles
void DeCasteljau(float u, float3 p0, float3 p1, float3 p2, out float3 p)
{
    float3 q0 = lerp(p0, p1, u);
    float3 q1 = lerp(p1, p2, u);
    
    p = lerp(q0, q1, u);
}
// Calculate point upon Bezier curve and return tangent
void DeCasteljau(float u, float3 p0, float3 p1, float3 p2, float3 p3, out float3 p, out float3 t)
{
    float3 q0 = lerp(p0, p1, u);
    float3 q1 = lerp(p1, p2, u);
    float3 q2 = lerp(p2, p3, u);
    float3 r0 = lerp(q0, q1, u);
    float3 r1 = lerp(q1, q2, u);
    
    t = r0 - r1; // tangent
    p = lerp(r0, r1, u);
}
// Bicubic interpolation of cubic Bezier surface
void DeCasteljauBicubic(float2 uv, float3 p[16], out float3 result, out float3 normal)
{
    // Interpolated values (e.g. points)
    float3 p0, p1, p2, p3;
    // Tangents (derivatives)
    float3 t0, t1, t2, t3;
    // Calculate tangent and positions along each curve
    DeCasteljau(uv.x, p[ 0], p[ 1], p[ 2], p[ 3], p0, t0);
    DeCasteljau(uv.x, p[ 4], p[ 5], p[ 6], p[ 7], p1, t1);
    DeCasteljau(uv.x, p[ 8], p[ 9], p[10], p[11], p2, t2);
    DeCasteljau(uv.x, p[12], p[13], p[14], p[15], p3, t3);
    // Calculate final position and tangents across surface
    float3 du, dv;
    DeCasteljau(uv.y, p0, p1, p2, p3, result, dv);
    DeCasteljau(uv.y, t0, t1, t2, t3, du);
    // du represents tangent
    // dv represents bitangent
    normal = normalize(cross(du, dv));
}

// DeCasteljau for float2's
void DeCasteljau(float u, float2 p0, float2 p1, float2 p2, float2 p3, out float2 p)
{
    float2 q0 = lerp(p0, p1, u);
    float2 q1 = lerp(p1, p2, u);
    float2 q2 = lerp(p2, p3, u);
    float2 r0 = lerp(q0, q1, u);
    float2 r1 = lerp(q1, q2, u);

    p = lerp(r0, r1, u);
}
// Used by PN-Triangles
void DeCasteljau(float u, float2 p0, float2 p1, float2 p2, out float2 p)
{
    float2 q0 = lerp(p0, p1, u);
    float2 q1 = lerp(p1, p2, u);
    
    p = lerp(q0, q1, u);
}
void DeCasteljau(float u, float2 p0, float2 p1, float2 p2, float2 p3, out float2 p, out float2 dp)
{
    float2 q0 = lerp(p0, p1, u);
    float2 q1 = lerp(p1, p2, u);
    float2 q2 = lerp(p2, p3, u);
    float2 r0 = lerp(q0, q1, u);
    float2 r1 = lerp(q1, q2, u);
    
    dp = r0 - r1;
    p = lerp(r0, r1, u);
}
void DeCasteljauBicubic(float2 uv, float2 p[16], out float2 result)
{

    // Interpolated values (e.g. points)
    float2 p0, p1, p2, p3;
    // Calculate value along each curve
    DeCasteljau(uv.x, p[ 0] , p[ 1] , p[ 2] , p[ 3] , p0);
    DeCasteljau(uv.x, p[ 4] , p[ 5] , p[ 6] , p[ 7] , p1);
    DeCasteljau(uv.x, p[ 8] , p[ 9] , p[10] , p[11] , p2);
    DeCasteljau(uv.x, p[12] , p[13] , p[14] , p[15] , p3);

    // Calculate position across surface
    DeCasteljau(uv.y, p0, p1, p2, p3, result);
}