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

// Implementation of Phong Tessellation as per 
// Boubekeur, T. and M. Alexa (2008). Phong Tessellation. ACM SIGGRAPH Asia 2008 papers. Singapore, ACM: 1-5.

#include "Common.hlsl"
#include "CommonTess.hlsl"

// NOTE: Phong Tessellation reuses one of the existing triangle hull shaders

// Orthogonal projection on to plane
// Where v1 is a point on the plane, and n is the plane normal
// v2_projected = v2 - dot(v2-v1, n) * n;
// e.g. ProjectOntoPlane(float3(0.98,0.19,0), float3(1,2,1), float3(3,2,1)) results in:
// (1.0792, 1.6276, 1) == (3,2,1) - dot((3,2,1)-(1,2,1), (0.98,0.19,0)) * (0.98,0.19,0) => (3,2,1) - 1.96 * (0.98,0.19, 0) => (1.0792, 1.6276, 1)
float3 ProjectOntoPlane(float3 planeNormal, float3 planePoint, float3 pointToProject)
{
    return pointToProject - dot(pointToProject-planePoint, planeNormal) * planeNormal;
}

// Phong Tessellation Domain Shader
[domain("tri")]
PixelShaderInput DS_PhongTessellation(HS_TrianglePatchConstant constantData, const OutputPatch<DS_ControlPointInput, 3> patch, float3 barycentricCoords : SV_DomainLocation )
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Interpolate using barycentric coordinates
    float3 position = BarycentricInterpolate(patch[0].Position, patch[1].Position, patch[2].Position, barycentricCoords);
    float2 UV = BarycentricInterpolate(constantData.TextureUV, barycentricCoords);
    float4 diffuse = BarycentricInterpolate(constantData.Diffuse, barycentricCoords);
    float3 worldNormal = BarycentricInterpolate(constantData.WorldNormal, barycentricCoords);
    float3 normal = BarycentricInterpolate(patch[0].Normal, patch[1].Normal, patch[2].Normal, barycentricCoords);
    float4 tangent = BarycentricInterpolate(patch[0].Tangent, patch[1].Tangent, patch[2].Tangent, barycentricCoords);

    // BEGIN Phong Tessellation
    // Orthogonal projection in the tangent planes
    float3 posProjectedU = ProjectOntoPlane(constantData.WorldNormal[0], patch[0].Position, position);
    float3 posProjectedV = ProjectOntoPlane(constantData.WorldNormal[1], patch[1].Position, position);
    float3 posProjectedW = ProjectOntoPlane(constantData.WorldNormal[2], patch[2].Position, position);

    // Interpolate the projected points
    position = BarycentricInterpolate(posProjectedU, posProjectedV, posProjectedW, barycentricCoords);
    // END Phong Tessellation

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