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

[domain("tri")] // Triangle domain for our shader
[partitioning("integer")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
DS_ControlPointInput HS_TrianglesInteger( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Normal = patch[id].Normal;
    result.Tangent = patch[id].Tangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_odd")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
DS_ControlPointInput HS_TrianglesFractionalOdd( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Normal = patch[id].Normal;
    result.Tangent = patch[id].Tangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("fractional_even")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
DS_ControlPointInput HS_TrianglesFractionalEven( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Normal = patch[id].Normal;
    result.Tangent = patch[id].Tangent;
    return result;
}

[domain("tri")] // Triangle domain for our shader
[partitioning("pow2")] // Partitioning type
[outputtopology("triangle_cw")] // The vertex winding order of the generated triangles
[outputcontrolpoints(3)] // Number of times this part of the hull shader will be called for each patch
[patchconstantfunc("HS_TrianglesConstant")] // The constant hull shader function
DS_ControlPointInput HS_TrianglesPow2( InputPatch<HullShaderInput, 3> patch, 
                    uint id : SV_OutputControlPointID,
                    uint patchID : SV_PrimitiveID )
{
    DS_ControlPointInput result = (DS_ControlPointInput)0;
    result.Position = patch[id].WorldPosition;
    result.Normal = patch[id].Normal;
    result.Tangent = patch[id].Tangent;
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
    //float3 view = normalize(patch[0].WorldPosition - CameraPosition);

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

    float3 roundedEdgeTessFactor; float roundedInsideTessFactor, insideTessFactor;
	ProcessTriTessFactorsMax((float3)TessellationFactor, 1.0, roundedEdgeTessFactor, roundedInsideTessFactor, insideTessFactor);

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
        result.Diffuse[i] = patch[i].Diffuse;
        result.TextureUV[i] = patch[i].TextureUV;
        result.WorldNormal[i] = patch[i].WorldNormal;
    }

    return result;
}

// This domain shader applies contol point weighting to the barycentric coords produced by the fixed function tessellator stage
[domain("tri")]
PixelShaderInput DS_Triangles( HS_TrianglePatchConstant constantData, const OutputPatch<DS_ControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Interpolate using barycentric coordinates
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
    float4 diffuse = BarycentricInterpolate(constantData.Diffuse, barycentricCoords);
    float3 worldNormal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);
    float3 normal = BarycentricInterpolate(patch[0].Normal, patch[1].Normal, patch[2].Normal, barycentricCoords);
    float4 tangent = BarycentricInterpolate(patch[0].Tangent, patch[1].Tangent, patch[2].Tangent, barycentricCoords);

    // Perform displacement
    worldNormal = normalize(worldNormal);
    position += CalculateDisplacement(UV, worldNormal);

    // Transform world position to view-projection
    result.Position = mul( float4(position,1), ViewProjection);
    
    
    result.Diffuse = diffuse;
    result.TextureUV = UV;
    result.WorldNormal = worldNormal;
    result.WorldPosition = position;
    result.Normal = normalize(normal);

    // Create TBN matrix - note: the transpose of TBN is the equivalent of inverse because it is orthogonal
    float3x3 worldToTangent = mul((float3x3)WorldInverse, transpose(CreateTBNMatrix(result.Normal, tangent)));

    // Calculate ToCamera/ToLight
    result.ToCameraTangentSpace = normalize(mul(CameraPosition - result.WorldPosition, worldToTangent));
    result.ToLightTangentSpace = normalize(mul(-Light.Direction, worldToTangent));

    return result;
}