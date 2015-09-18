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
#include "CommonDecal.hlsl"

[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
[maxtessfactor(64.0f)] // allow full tessellation for examples
DS_ControlPointInput HS_TrianglesInteger( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    result.WorldTangent = patch[id].WorldTangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_odd")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
[maxtessfactor(64.0f)] // allow full tessellation for examples
DS_ControlPointInput HS_TrianglesFractionalOdd( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    result.WorldTangent = patch[id].WorldTangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_even")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
[maxtessfactor(64.0f)] // allow full tessellation for examples
DS_ControlPointInput HS_TrianglesFractionalEven( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    result.WorldTangent = patch[id].WorldTangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("pow2")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
[maxtessfactor(64.0f)] // allow full tessellation for examples
DS_ControlPointInput HS_TrianglesPow2( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Diffuse = patch[id].Diffuse;
    result.WorldTangent = patch[id].WorldTangent;
    return result;
}

HS_TrianglePatchConstant HS_TrianglesConstant(InputPatch<HullShaderInput, 3> patch)
{
    HS_TrianglePatchConstant result = (HS_TrianglePatchConstant)0;

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

    //// begin: displacement adaptive tessellation
    //float3 p[3];
    //[unroll]
    //for (uint j = 0; j < 3; j++)
    //    p[j] = patch[j].WorldPosition;
    //// Increase tessellation by 10 if decal
    //float3 ignoreEdges;
    //DecalTessellationFactor(p, roundedEdgeTessFactor/*ignoreEdges*/, roundedInsideTessFactor, 10);
    //// end: displacement adaptive tessellation

    // Apply the edge and inside tessellation factors
    result.EdgeTessFactor[0] = roundedEdgeTessFactor.x;
    result.EdgeTessFactor[1] = roundedEdgeTessFactor.y;
    result.EdgeTessFactor[2] = roundedEdgeTessFactor.z;
    result.InsideTessFactor = roundedInsideTessFactor;
    //result.InsideTessFactor = roundedInsideTessFactor * insideMultiplier;
    
    // Apply constant information
    [unroll]
    for (uint i = 0; i < 3; i++)
    {
        result.TextureUV[i] = patch[i].TextureUV;
        result.WorldNormal[i] = patch[i].WorldNormal;
    }

    return result;
}

// This domain shader applies control point weighting to the barycentric coords produced by the fixed function tessellator stage
[domain("tri")]
PixelShaderInput DS_Triangles( HS_TrianglePatchConstant constantData, const OutputPatch<DS_ControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Interpolate using barycentric coordinates
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    // Interpolate array of UV coordinates
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
    float4 diffuse = BarycentricInterpolate(patch[0].Diffuse, patch[1].Diffuse, patch[2].Diffuse, barycentricCoords);
    // Interpolate array of normals
    float3 normal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);
    float4 tangent = BarycentricInterpolate(patch[0].WorldTangent, patch[1].WorldTangent, patch[2].WorldTangent, barycentricCoords);

    // Perform displacement
    normal = normalize(normal);
    position += CalculateDisplacement(UV, normal);
    
    // Perform decal displacement
    position += DecalDisplacement(normal, position, result.DecalUV);

    // Transform world position to view-projection
    result.Position = mul( float4(position,1), ViewProjection );
    
    result.Diffuse = diffuse;
    result.TextureUV = UV;
    result.WorldNormal = normal;
    result.WorldPosition = position;
    result.WorldTangent = tangent;

    return result;
}