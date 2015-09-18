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

// Vertex shader main function
PixelShaderInput VSMain(VertexShaderInput vertex)
{
    PixelShaderInput result = (PixelShaderInput)0;

    // Change the position vector to be 4 units for matrix transformation
    vertex.Position.w = 1.0;
    
    // If there are skin weights apply vertex skinning
    if (vertex.SkinWeights.x != 0)
    {
        // Calculate the skin transform from up to four bones and weights
        float4 weights = vertex.SkinWeights;
        uint4 bones = vertex.SkinIndices;
        float4x4 skinTransform = Bones[bones.x] * weights.x +
            Bones[bones.y] * weights.y +
            Bones[bones.z] * weights.z +
            Bones[bones.w] * weights.w;
   
        // Apply skinning to vertex and normal
        vertex.Position = mul(vertex.Position, skinTransform);
        
        // We assume here that the skin transform includes only uniform scaling (if any)
        vertex.Normal = mul(vertex.Normal, (float3x3)skinTransform);
    }

    result.Position = mul(vertex.Position, WorldViewProjection);
    result.Diffuse = vertex.Color * MaterialDiffuse;
    // Apply material UV transformation
    result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;
    
    
    // We use the inverse transpose of the world so that if there is non uniform
    // scaling the normal is transformed correctly. We also use a 3x3 so that 
    // the normal is not affected by translation (i.e. a vector has the same direction
    // and magnitude regardless of translation)
    result.WorldNormal = mul(vertex.Normal, (float3x3)WorldInverseTranspose);
    
    result.WorldPosition = mul(vertex.Position, World).xyz;
    
    
    
    return result;
}