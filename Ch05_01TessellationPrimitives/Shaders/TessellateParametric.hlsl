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
#include "CommonTess.hlsl"

[domain("quad")] // Quad domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_ccw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(1)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_ParametricConstant")] // The constant hull shader function
DS_ControlPointInput HS_ParametricInteger( InputPatch<HullShaderInput, 1> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    return result;
}

[domain("quad")] // Quad domain for our shader
[partitioning("fractional_odd")] // Partitioning type
[outputtopology("triangle_ccw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(1)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_ParametricConstant")] // The constant hull shader function
DS_ControlPointInput HS_ParametricFractionalOdd( InputPatch<HullShaderInput, 1> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    return result;
}

[domain("quad")] // Quad domain for our shader
[partitioning("fractional_even")] // Partitioning type
[outputtopology("triangle_ccw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(1)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_ParametricConstant")] // The constant hull shader function
DS_ControlPointInput HS_ParametricFractionalEven( InputPatch<HullShaderInput, 1> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    return result;
}

[domain("quad")] // Quad domain for our shader
[partitioning("pow2")] // Partitioning type
[outputtopology("triangle_ccw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(1)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_ParametricConstant")] // The constant hull shader function
DS_ControlPointInput HS_ParametricPow2( InputPatch<HullShaderInput, 1> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    return result;
}

// Quad patch constant func (executes once for each patch)
HS_QuadPatchConstant HS_ParametricConstant(InputPatch<HullShaderInput, 1> patch)
{
    HS_QuadPatchConstant result = (HS_QuadPatchConstant)0;

    // Perform rounding
    float4 roundedEdgeTessFactor; 
    float2 roundedInsideTessFactor, insideTessFactor;
    Process2DQuadTessFactorsMax((float4)TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

    // Apply the edge and inside tessellation factors
    result.EdgeTessFactor[0] = roundedEdgeTessFactor.x;
    result.EdgeTessFactor[1] = roundedEdgeTessFactor.y;
    result.EdgeTessFactor[2] = roundedEdgeTessFactor.z;
    result.EdgeTessFactor[3] = roundedEdgeTessFactor.w;

    result.InsideTessFactor[0] = roundedInsideTessFactor.x;
    result.InsideTessFactor[1] = roundedInsideTessFactor.y;
    
    return result;
}


// This domain shader uses the SV_DomainLocation as input to an equation to render a parametric surface
[domain("quad")]
PixelShaderInput DS_Parametric( HS_QuadPatchConstant constantData, const OutputPatch<DS_ControlPointInput, 1> patch, float2 uv : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    float PI2 = 6.28318530;
    float PI = 3.14159265;

    float S = PI2 * uv.x;
    float T = PI2 * uv.y;
    float sinS, cosS, sinT, cosT;
    sincos(S, sinS, cosS);
    sincos(T, sinT, cosT);
    
    // Sphere
    float R = 1.0;
    float3 spherePos = float3(R * sinS * cosT, R * sinS * sinT, R * cosS);
    
    // Torus
    float R1 = 0.5; // radius of ring
    float R2 = 0.25;// radius of tube
    float R3 = (R1 + R2 * cosT);
    float3 torusPos = float3(R3 * cosS, R3 * sinS, R2 * sinT);

    float3 position = torusPos;
    float4 diffuse = float4(normalize(position), 1); // bias away from black
    float3 normal = normalize(position);

    // Prepare pixel shader input:
    // Transform world position to view-projection
    result.Position = mul( float4(position,1), WorldViewProjection );
    
    
    result.Diffuse = diffuse;
    result.TextureUV = uv * 4; // set UV coordinate
    result.WorldNormal = normal;
    result.WorldPosition = position;
    
    return result;
}