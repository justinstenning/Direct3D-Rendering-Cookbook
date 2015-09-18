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

// To support decal displacement mapping
Texture2D DecalDisplacementMap : register(t2);
Texture2D DecalDiffuse : register(t3);
Texture2D DecalNormalMap : register(t4);
// Assumes that SamplerState Sampler : register(s0); exists

// Controls the decal displacement
cbuffer DecalBuffer : register(b4) {
    float DecalDisplaceScale;
    float3 DecalNormal;    // If normal is 0 then decal not applied
    float3 DecalTangent;
    float3 DecalBitangent;
    float3 DecalPosition;
    float DecalRadius;
}


float3 DecalNormalSample(float2 decalUV)
{
    return DecalNormalMap.Sample(Sampler, decalUV).rgb;
}

float4 DecalDiffuseSample(float2 decalUV)
{
    return DecalDiffuse.Sample(Sampler, decalUV).rgba;
}

// The float3 result should be added to the vertex position
float3 DecalDisplacement(float3 worldNormal, float3 worldPosition, out float3 decalUV)
{
    float3 result = (float3)0;
    
    // Note: if the decalUV.z == 0 the pixel shader will assume no decal map needs to be queried
    decalUV = (float3)0;

    // Skip displacement sampling if 0 multiplier or if the decal normal is not set
    if (DecalDisplaceScale == 0 || (DecalNormal.x == 0.0 && DecalNormal.y == 0.0 && DecalNormal.z == 0))
        return result;

    float3 decalPosWorld = mul(float4(DecalPosition, 1), World).xyz;
    float distanceToDecal = distance(worldPosition, decalPosWorld);

    // if the distance to the decal position is within the radius
    // then we need to perform displacement based on decal
    if (distanceToDecal <= DecalRadius)
    {
        // 1. Calculate the decal texture coordinate
        // Important TODO: using tangent space is more efficient
        //float3x3 worldToDecalTangent = float3x3(DecalTangent, DecalBitangent, DecalNormal); // if using tangent space
        
        // Convert to world space
        float3 dT = normalize(mul(DecalTangent, (float3x3)WorldInverseTranspose));
        float3 dB = normalize(mul(DecalBitangent, (float3x3)WorldInverseTranspose));
        float3 dN = normalize(mul(DecalNormal, (float3x3)WorldInverseTranspose));
        float3x3 worldToDecal = float3x3(dT, dB, dN);
        
        //decalUV = mul(worldToDecalTangent, worldPosition - DecalPosition);
        decalUV = mul(worldToDecal, worldPosition - decalPosWorld);
        // Remap to range between 0 and 1
        decalUV /= 2 * DecalRadius; // (-0.5,0.5)
        decalUV += 0.5; // (0,1)
        // z=1 tells the pixel shader that there is a decal texture to sample
        decalUV.z = 1.0; 

        // 2. Sample the displacement map
        // Choose the most detailed mipmap level
	    const float mipLevel = 1.0f;
	
	    // Sample height map - using R channel
	    float height = DecalDisplacementMap.SampleLevel(Sampler, decalUV.xy, mipLevel).r;
    
        // remap height from 0 to 1, to -1 to 1 (with midlevel offset)
        float midLevel = 0.5; // TODO: make this configurable
        if (height > midLevel)
            // Remap the range between midLevel and 1 to 0 and 1
            height = (height-midLevel) / (1 - midLevel);
        else
            // Remap the range between 0 and midLevel to -1 and 0
            height = height / midLevel - 1;

        // Return offset along DecalNormal. This allows the decal to be applied 
        // at an angle to the surface, e.g. to allow the direction of a bullet to decide the deformation
	    result = height * DecalDisplaceScale * dN;// worldNormal;
    }

    return result;
}

// Determine vector magnitude
float magnitudeSqrd(float3 x)
{
    // Shader Model 5: mad(m,a,b) { return  m*a+b; }
    return mad(x.x, x.x, mad(x.y, x.y, x.z * x.z));
}


// Copyright (c) 2013 Justin Stenning
// This function is adapted from DirectX 11 SDK sample code.
//-------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//-------------------------------
void DecalTessellationFactor(float3 p[3], inout float3 edgeTessFactor, inout float insideTessFactor, float tessellation)
{
    // Don't do anything if there is no decal or scale is 0
    if (DecalDisplaceScale == 0.0 || (DecalNormal.x == 0.0 && DecalNormal.y == 0.0 && DecalNormal.z == 0.0))
    {
        return;
    }

    float3 pos0 = p[2];
    float3 pos1 = p[1];
    float3 pos2 = p[0];

    // The distance calculation is based on the vector formula for the distance from a point to a line.
    // Given a line from point A to point B, and a point in space P, the perpendicular distance to the
    // line is found by projecting the vector from A to P onto the line A to B, or more accurately onto 
    // the vector formed by the line from A to B. A right triangle can be constructed by taking the line
    // from P to A as the hypotenuse, the projection of the hypotenuse onto the line AB as the second edge,
    // and the perpendicular line from the point P to the line AB as the third edge. The distance from the
    // point to the line is the distance of this third edge. The pythagorean theorm gives us the distance.
    // We can use the squared distance to avoid taking epensive square roots. Therefore,
    // 
    // distanceSquared = (squared length of PA) - (squared length of projection of PA onto AB)
    
    // calculate edges and squared magnitude of edges
    float3 edge0 = pos1 - pos0;
    float3 edge1 = pos2 - pos0;
    float3 edge2 = pos2 - pos1;
    float3 faceNormal = normalize(cross(edge0, edge2));
    float magSqrdEdge0 = magnitudeSqrd(edge0);
    float magSqrdEdge1 = magnitudeSqrd(edge1);
    float magSqrdEdge2 = magnitudeSqrd(edge2);

    // Create vectors from the decal location to all 3 vertices, then compute the squared distance (i.e. magnitude)
    float3 decalPos = mul(float4(DecalPosition, 1), World).xyz;

    float3 decalEdge0 = decalPos - pos0;
    float3 decalEdge1 = decalPos - pos1;
    float3 decalEdge2 = decalPos - pos2;

    float magSqrdDecalEdge0 = magnitudeSqrd(decalEdge0);
    float magSqrdDecalEdge1 = magnitudeSqrd(decalEdge1);
    float magSqrdDecalEdge2 = magnitudeSqrd(decalEdge2);

    bool edgeTessellated = false;
    float decalRadiusSqrd = DecalRadius * DecalRadius;

    float3 projected;
    float magSqrdProjected;
    float distanceSqrd;

    // Edge 0
    // Check if the patch vertices for edge0 are within range of the decal
    if (magSqrdDecalEdge0 <= decalRadiusSqrd || magSqrdDecalEdge1 <= decalRadiusSqrd)
    {
        edgeTessFactor.x += tessellation;
        edgeTessellated = true;
    }
    else 
    {
        // If the distance from the decal to either of the endpoints is greater than the radius,
        // then part of the edge may still be within the a radius distance from the decal location. To
        // determine this we need to calculate the distance from the decal location to the edge.
        projected = (dot(decalEdge0, edge0) / magSqrdEdge0) * edge0;
        magSqrdProjected = magnitudeSqrd(projected);

        distanceSqrd = magSqrdDecalEdge0 - magSqrdProjected;

        // See if the distance squared is less than or equal to the radius squared. Also
        // check to see if the the perpendicular distance is within the line segment. This
        // is done by testing the direction of the projection with the edge direction (negative
        // means it's on the line in the opposite direction. Also if the length of the projection
        // is greater than the edge, then the distance is measured to a point beyond the segment
        // in either case we don't want to tessellate.
        if ((magSqrdProjected <= magSqrdEdge0) && (distanceSqrd <= decalRadiusSqrd) && dot(projected, edge0) >= 0)
        {
            edgeTessFactor.x += tessellation;
            edgeTessellated = true;
        }
    }

    // Edge 1
    // Check if the patch vertices for edge0 are within range of the decal
    if (magSqrdDecalEdge1 <= decalRadiusSqrd || magSqrdDecalEdge2 <= decalRadiusSqrd)
    {
        edgeTessFactor.y += tessellation;
        edgeTessellated = true;
    }
    else 
    {
        projected = (dot(decalEdge1, edge1) / magSqrdEdge1) * edge1;
        magSqrdProjected = magnitudeSqrd(projected);

        distanceSqrd = magSqrdDecalEdge1 - magSqrdProjected;

        if ((magSqrdProjected <= magSqrdEdge1) && (distanceSqrd <= decalRadiusSqrd) && dot(projected, edge1) >= 0)
        {
            edgeTessFactor.y += tessellation;
            edgeTessellated = true;
        }
    }

    // Edge 2
    // Check if the patch vertices for edge0 are within range of the decal
    if (magSqrdDecalEdge2 <= decalRadiusSqrd || magSqrdDecalEdge0 <= decalRadiusSqrd)
    {
        edgeTessFactor.z += tessellation;
        edgeTessellated = true;
    }
    else 
    {
        projected = (dot(decalEdge1, edge2) / magSqrdEdge2) * edge2;
        magSqrdProjected = magnitudeSqrd(projected);

        distanceSqrd = magSqrdDecalEdge2 - magSqrdProjected;

        if ((magSqrdProjected <= magSqrdEdge2) && (distanceSqrd <= decalRadiusSqrd) && dot(projected, edge2) >= 0)
        {
            edgeTessFactor.z += tessellation;
            edgeTessellated = true;
        }
    }

    // Inside

    // There are 2 reasons to enable tessellation for the inside. One
    // reason is if any of the edges of the patch need tessellation. The
    // other reason is if the hit location is on the triangle, but the
    // radius is too small to touch any of the edges.
    // First check to see if the distance from the hit location
    // to the plane formed by the triangle is within the decal radius.
    // Use the dot product to find the distance between a point and face normal
    float distanceToPlane = abs(dot(faceNormal, edge0));

    // If the distance to the triangle plane is within the decal radius,
    // check to see if the intersection point is inside the triangle
    if (distanceToPlane <= DecalRadius)
    {
        float dot0DecalEdge0 = dot(edge0, decalEdge0);
        float dot2DecalEdge0 = dot(edge2, decalEdge0);
        float dot00 = dot(edge0, edge0);
        float dot02 = dot(edge0, edge2);
        float dot22 = dot(edge2, edge2);
        float invDenominator = 1.0 / (dot00 * dot22 - dot02 * dot02);
        
        // Calculate barycentric coordinates to determine if the point is in the triangle
        float u = (dot22 * dot0DecalEdge0 - dot02 * dot2DecalEdge0) * invDenominator;
        float v = (dot00 * dot2DecalEdge0 - dot02 * dot0DecalEdge0) * invDenominator;
        // barycentric rule: u+v+w == 1, w = 1-u+v, therefore if u+v < 1, point is inside triangle
        if (u > 0 && v > 0 && (u+v < 1) || edgeTessellated)
        {
            insideTessFactor += tessellation;
        }
    }
}