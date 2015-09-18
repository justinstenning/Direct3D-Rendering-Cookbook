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

// Constant buffer to be updated by application per object
cbuffer PerObject : register(b0)
{
    // WorldViewProjection matrix
    float4x4 WorldViewProjection;
    
    // We need the world matrix so that we can
    // calculate the lighting in world space
    float4x4 World;
    
    // Inverse transpose of world, used for
    // bringing normals into world space, especially
    // necessary where non-uniform scaling has been applied
    float4x4 WorldInverseTranspose;
};

// Constant buffer - updated once per frame
// Note: HLSL data is packed in such a
// way that it does not cross a 16-byte boundary
cbuffer PerFrame: register (b1)
{
    float3 CameraPosition;
};

// Vertex Shader input structure (from Application)
struct VertexShaderInput
{
    float4 Position : SV_Position;// Position - xyzw
    float3 Normal : NORMAL;    // Normal - for lighting and mapping operations
    float4 Color : COLOR;     // Color - vertex color, used to generate a diffuse color
};

// Pixel Shader input structure (from Vertex Shader)
struct PixelShaderInput
{
    float4 Position : SV_Position;
    // Interpolation of combined vertex and material diffuse
    float4 Diffuse : COLOR;

    // We need the World Position and normal for light calculations
    float3 WorldNormal : NORMAL;
    float3 WorldPosition : WORLDPOS;
};
