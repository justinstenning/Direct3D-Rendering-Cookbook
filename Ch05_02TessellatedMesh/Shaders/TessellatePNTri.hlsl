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

// Implementation of PN-Triangles as per "VLACHOS, A. et al. Curved PN triangles. Proceedings of the 2001 symposium on Interactive 3D graphics: ACM: 159-166 p. 2001."
// A PN-triangle is a special three-sided, or triangular Bezier patch (three-sided patches were invented by de Casteljau)
/*
    PN-Triangle Control Points (coefficients)     PN-Triangle Normal coefficients
    b003_____b012____b021_____b030                n002_________n011_________n020
       \      /\      /\      /                      \          /\          /
        \    /  \    /  \    /                        \        /  \        /
         \  /    \  /    \  /                          \      /    \      /
      b102\/______\/______\/b120                        \    /      \    /
           \      /\b111  /                              \  /        \  /
            \    /  \    /                            n101\/__________\/n110
             \  /    \  /                                  \          /
          b201\/______\/b210                                \        /
               \      /                                      \      /
                \    /                                        \    /
                 \  /                                          \  /
                  \/                                            \/
                 b300                                          n200

    Vertices: b300, b030, b003
    Tangents: b210, b120, b021, b012, b102, b201
    Center:   b111

    Normals: n200, n020, n002
    Mid-edges: n110, n011, n101
*/

struct HS_PNTrianglePatchConstant
{
    float EdgeTessFactor[3] : SV_TessFactor;
    float InsideTessFactor : SV_InsideTessFactor;

    float3 B210: POSITION3;
    float3 B120: POSITION4;
    float3 B021: POSITION5;
    float3 B012: POSITION6;
    float3 B102: POSITION7;
    float3 B201: POSITION8;
    float3 B111: CENTER;
    
    float3 N200: NORMAL0;
    float3 N020: NORMAL1;
    float3 N002: NORMAL2;

    float3 N110: NORMAL3;      
    float3 N011: NORMAL4;
    float3 N101: NORMAL5;
};

struct DS_PNControlPointInput
{
    float3 Position: POSITION;
    float3 WorldNormal: NORMAL;
    float4 Diffuse : COLOR;
    float2 TextureUV: TEXCOORD;
};

[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_PNTrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS_PNTrianglesInteger( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_PNControlPointInput result = (DS_PNControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.Diffuse = patch[id].Diffuse;
    result.TextureUV = patch[id].TextureUV;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_odd")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_PNTrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS_PNTrianglesFractionalOdd( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_PNControlPointInput result = (DS_PNControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.Diffuse = patch[id].Diffuse;
    result.TextureUV = patch[id].TextureUV;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_even")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_PNTrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS_PNTrianglesFractionalEven( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_PNControlPointInput result = (DS_PNControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.Diffuse = patch[id].Diffuse;
    result.TextureUV = patch[id].TextureUV;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("pow2")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_PNTrianglesConstant")] // The constant hull shader function
DS_PNControlPointInput HS_PNTrianglesPow2( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_PNControlPointInput result = (DS_PNControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.WorldNormal = patch[id].WorldNormal;
    result.Diffuse = patch[id].Diffuse;
    result.TextureUV = patch[id].TextureUV;
    return result;
}

HS_PNTrianglePatchConstant HS_PNTrianglesConstant(InputPatch<HullShaderInput, 3> patch)
{
    HS_PNTrianglePatchConstant result = (HS_PNTrianglePatchConstant)0;

    //// Backface culling - using face normal
    //// Calculate face normal
    //float3 edge0 = patch[1].WorldPosition - patch[0].WorldPosition;
    //float3 edge2 = patch[2].WorldPosition - patch[0].WorldPosition;
    //float3 faceNormal = normalize(cross(edge2, edge0));
    //float3 view = normalize(CameraPosition - patch[0].WorldPosition);

    //if (dot(view, faceNormal) < -0.25) {
    //    result.EdgeTessFactor[0] = 0;
    //    result.EdgeTessFactor[1] = 0;
    //    result.EdgeTessFactor[2] = 0;
    //    result.InsideTessFactor = 0;
    //    return result; // culled, so no further processing
    //}
    //// end: backface culling

    //// Backface culling - using Vertex normals
    //bool backFacing = true;
    ////float insideMultiplier = 0.125; // default inside multiplier
    //[unroll]
    //for (uint j = 0; j < 3; j++)
    //{
    //    float3 view = normalize(CameraPosition - patch[j].WorldPosition);
    //    float a = dot(view, patch[j].WorldNormal);
    //    if (a >= -0.125) {
    //        backFacing = false;
    //        //if (a <= 0.125)
    //        //{
    //        //    // Is near to silhouette so keep full tessellation
    //        //    insideMultiplier = 1.0;
    //        //}
    //    }
    //}
    //if (backFacing) {
    //    result.EdgeTessFactor[0] = 0;
    //    result.EdgeTessFactor[1] = 0;
    //    result.EdgeTessFactor[2] = 0;
    //    result.InsideTessFactor = 0;
    //    return result; // culled, so no further processing
    //}
    //// end: backface culling

    float3 roundedEdgeTessFactor;
    float roundedInsideTessFactor, insideTessFactor;
    ProcessTriTessFactorsMax((float3)TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

    // Apply the edge and inside tessellation factors
    result.EdgeTessFactor[0] = roundedEdgeTessFactor.x;
    result.EdgeTessFactor[1] = roundedEdgeTessFactor.y;
    result.EdgeTessFactor[2] = roundedEdgeTessFactor.z;
    result.InsideTessFactor = roundedInsideTessFactor;
    //result.InsideTessFactor = roundedInsideTessFactor * insideMultiplier;

    //************************************************************
    // Calculate PN-Triangle coefficients
    // Refer to Vlachos 2001 for the original formula
    float3 p1 = patch[0].WorldPosition;
    float3 p2 = patch[1].WorldPosition;
    float3 p3 = patch[2].WorldPosition;

    //B300 = p1;
    //B030 = p2;
    //float3 b003 = p3;
    
    float3 n1 = patch[0].WorldNormal;
    float3 n2 = patch[1].WorldNormal;
    float3 n3 = patch[2].WorldNormal;
    
    //N200 = n1;
    //N020 = n2;
    //N002 = n3;

    // Calculate control points
    float w12 = dot ((p2 - p1), n1);
    result.B210 = (2.0f * p1 + p2 - w12 * n1) / 3.0f;

    float w21 = dot ((p1 - p2), n2);
    result.B120 = (2.0f * p2 + p1 - w21 * n2) / 3.0f;

    float w23 = dot ((p3 - p2), n2);
    result.B021 = (2.0f * p2 + p3 - w23 * n2) / 3.0f;
    
    float w32 = dot ((p2 - p3), n3);
    result.B012 = (2.0f * p3 + p2 - w32 * n3) / 3.0f;

    float w31 = dot ((p1 - p3), n3);
    result.B102 = (2.0f * p3 + p1 - w31 * n3) / 3.0f;
    
    float w13 = dot ((p3 - p1), n1);
    result.B201 = (2.0f * p1 + p3 - w13 * n1) / 3.0f;
    
    float3 e = (result.B210 + result.B120 + result.B021 + 
                result.B012 + result.B102 + result.B201) / 6.0f;
    float3 v = (p1 + p2 + p3) / 3.0f;
    result.B111 = e + ((e - v) / 2.0f);
    
    // Calculate normals
    float v12 = 2.0f * dot ((p2 - p1), (n1 + n2)) / 
                          dot ((p2 - p1), (p2 - p1));
    result.N110 = normalize ((n1 + n2 - v12 * (p2 - p1)));

    float v23 = 2.0f * dot ((p3 - p2), (n2 + n3)) /
                          dot ((p3 - p2), (p3 - p2));
    result.N011 = normalize ((n2 + n3 - v23 * (p3 - p2)));

    float v31 = 2.0f * dot ((p1 - p3), (n3 + n1)) /
                          dot ((p1 - p3), (p1 - p3));
    result.N101 = normalize ((n3 + n1 - v31 * (p1 - p3)));

    return result;
}

// This domain shader applies contol point weighting to the barycentric coords produced by the fixed function tessellator stage
[domain("tri")]
PixelShaderInput DS_PNTriangles( HS_PNTrianglePatchConstant constantData, const OutputPatch<DS_PNControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Prepare barycentric ops (xyz=uvw,   w=1-u-v,   u,v,w>=0)
    float u = barycentricCoords.x;
    float v = barycentricCoords.y;
    float w = barycentricCoords.z;
    float uu = u * u;
    float vv = v * v;
    float ww = w * w;
    float uu3 = 3.0f * uu;
    float vv3 = 3.0f * vv;
    float ww3 = 3.0f * ww;

    // Interpolate using barycentric coordinates and PN Triangle control points
    float3 position = 
        patch[0].Position * w * ww + //B300
        patch[1].Position * u * uu + //B030
        patch[2].Position * v * vv + //B003
        constantData.B210 * ww3 * u +
        constantData.B120 * uu3 * w +
        constantData.B201 * ww3 * v +
        constantData.B021 * uu3 * v +
        constantData.B102 * vv3 * w +
        constantData.B012 * vv3 * u +
        constantData.B111 * 6.0f * w * u * v;
    float3 normal = 
        patch[0].WorldNormal * ww + //N200
        patch[1].WorldNormal * uu + //N020
        patch[2].WorldNormal * vv + //N002
        constantData.N110 * w * u +
        constantData.N011 * u * v +
        constantData.N101 * w * v;

    // Interpolate using barycentric coordinates as per Tri
    float2 UV = BarycentricInterpolate(patch[0].TextureUV, patch[1].TextureUV, patch[2].TextureUV, barycentricCoords);
    float4 diffuse = BarycentricInterpolate(patch[0].Diffuse, patch[1].Diffuse, patch[2].Diffuse, barycentricCoords);
    
    // Transform world position to view-projection
    result.Position = mul( float4(position,1), ViewProjection);
    
    
    result.Diffuse = diffuse;
    result.TextureUV = UV;
    result.WorldNormal = normal;
    result.WorldPosition = position;
    
    return result;
}