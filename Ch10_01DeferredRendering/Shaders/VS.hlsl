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

void SkinVertex(float4 weights, uint4 bones, inout float4 position, inout float3 normal, inout float4 tangent)
{
    // If there are skin weights apply vertex skinning
    if (weights.x != 0)
    {
        // Calculate the skin transform from up to four bones and weights
        float4x4 skinTransform = Bones[bones.x] * weights.x +
            Bones[bones.y] * weights.y +
            Bones[bones.z] * weights.z +
            Bones[bones.w] * weights.w;
   
        // Apply skinning to vertex and normal
        position = mul(position, skinTransform);
        
        // We assume here that the skin transform includes only uniform scaling (if any)
        normal = mul(normal, (float3x3)skinTransform);
        // also for the tangent (the w component contains the handedness used to when calculating bitangent)
        tangent = float4(mul(tangent.xyz, (float3x3)skinTransform), tangent.w);
    }
}

PixelShaderInput VSMain(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Apply vertex skinning if any
    SkinVertex(vertex.SkinWeights, vertex.SkinIndices, vertex.Position, vertex.Normal, vertex.Tangent);

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
