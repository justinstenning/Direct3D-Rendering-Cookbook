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

// Globals for texture sampling
Texture2D ShaderTexture : register(t0);
SamplerState Sampler : register(s0);

// Constant buffer to be updated by application per object
cbuffer PerObject : register(b0)
{
    // WorldViewProjection matrix
    float4x4 WorldViewProj;
};

// Vertex Shader input structure with position 
// and texture coordinate
struct VertexShaderInput
{
    float4 Position : SV_Position;
    float2 TextureUV : TEXCOORD0;
};

// Vertex Shader output structure consisting of the
// transformed position and texture coord
// This is also the pixel shader input
struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float2 TextureUV : TEXCOORD0;
};

// Vertex shader main function
VertexShaderOutput VSMain(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    // Transform the position from object space to homogeneous 
	// projection space
    output.Position = mul(input.Position, WorldViewProj);
    // Pass through the texture coord of the vertex
    output.TextureUV = input.TextureUV;

    return output;
}

// A simple Pixel Shader that simply passes through the interpolated color
float4 PSMain(VertexShaderOutput input) : SV_Target
{
    // Sample the pixel color using the sampler and texture 
    // using the input texture coordinate
    return ShaderTexture.Sample(Sampler, input.TextureUV);
}