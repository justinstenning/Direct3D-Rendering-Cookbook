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
[outputcontrolpoints(16)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_BezierConstant")] // The constant hull shader function
DS_ControlPointInput HS_BezierInteger( InputPatch<HullShaderInput, 16> patch, 
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
[outputcontrolpoints(16)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_BezierConstant")] // The constant hull shader function
DS_ControlPointInput HS_BezierFractionalOdd( InputPatch<HullShaderInput, 16> patch, 
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
[outputcontrolpoints(16)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_BezierConstant")] // The constant hull shader function
DS_ControlPointInput HS_BezierFractionalEven( InputPatch<HullShaderInput, 16> patch, 
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
[outputcontrolpoints(16)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_BezierConstant")] // The constant hull shader function
DS_ControlPointInput HS_BezierPow2( InputPatch<HullShaderInput, 16> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    return result;
}

HS_BezierPatchConstant HS_BezierConstant(InputPatch<HullShaderInput, 16> patch)
{
    HS_BezierPatchConstant result = (HS_BezierPatchConstant)0;

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
    
    // Apply constant information
    [unroll]
    for (uint i = 0; i < 16; i++)
    {
        result.TextureUV[i] = patch[i].TextureUV;
    }

    return result;
}

// Applies control point weighting using Bezier bicubic interpolation
[domain("quad")]
PixelShaderInput DS_Bezier( HS_BezierPatchConstant constantData, const OutputPatch<DS_ControlPointInput, 16> patch, float2 uv : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Colors
    float3 c[16];
    // Control points
    float3 p[16];
    [unroll]
    for(uint i=0;i<16;i++) {
        p[i] = patch[i].Position;
        c[i] = patch[i].Diffuse.rgb;
    }
    float3 position, normal;
    // Perform De Casteljau bicubic interpolation of positions and output position and normal
    DeCasteljauBicubic(uv, p, position, normal);
    // Perform De Casteljau bicubic interpolation of UV coordinates
    DeCasteljauBicubic(uv, constantData.TextureUV, result.TextureUV); 
    
    // NOTE: it isn't possible to record the UV and colors in the constant data
    // either one or the other is used, or the color can be passed in as a DS_ControlPointInput

    // Calculate diffuse color with consideration of all 16 control points
    // (using alpha from only the first control point)
    float3 color, c1;
    DeCasteljauBicubic(uv, c, color, c1);
    float4 diffuse = float4(color, patch[0].Diffuse.a);

    // Alternative for determining color:
    // Bilerp diffuse color based on each corner
    //float4 c[4];
    //c[0] = constantData.Diffuse[0];
    //c[1] = constantData.Diffuse[3];
    //c[2] = constantData.Diffuse[12];
    //c[3] = constantData.Diffuse[15];
    // float4 diffuse = Bilerp(c, uv);

    // Prepare pixel shader input:
    // Transform world position to view-projection
    result.Position = mul( float4(position,1), ViewProjection );
    
    
    result.Diffuse = diffuse;
    //result.TextureUV = UV;
    result.WorldNormal = normal;
    result.WorldPosition = position;
    
    return result;
}