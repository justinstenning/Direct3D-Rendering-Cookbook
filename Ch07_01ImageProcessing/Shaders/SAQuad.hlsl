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


Texture2DMS<float4> TextureMS0 : register(t0);
Texture2D<float4> Texture0 : register(t0);
SamplerState Sampler : register(s0);

struct VertexIn
{
    float4 Position : SV_Position;// Position - xyzw
};

struct PixelIn
{
    float4 Position : SV_POSITION;// Position - xyzw
    float2 UV : TEXCOORD0;
};

// Vertex shader main function
PixelIn VSMain(VertexIn vertex)
{
    PixelIn result = (PixelIn)0;
    
    // The input quad is expected in device coordinates 
    // (i.e. 0,0 is center of screen, -1,1 top left, 1,-1 bottom right)
    // Therefore no transformation!
    result.Position = vertex.Position;

    // The UV coordinates are top-left 0,0, bottom-right 1,1
    
    result.UV.x = mad(result.Position.x, 0.5, 0.5);  // UV.x = position.x * 0.5 + 0.5;
    result.UV.y = mad(result.Position.y, -0.5, 0.5); // UV.y = position.x * -0.5 + 0.5;

    return result;
}

float4 PSMain(PixelIn input) : SV_Target
{
    return Texture0.Sample(Sampler, input.UV);
}

float4 PSMainMultisample(PixelIn input, uint sampleIndex: SV_SampleIndex) : SV_Target
{
    int2 screenPos = int2(input.Position.xy);
    return TextureMS0.Load(screenPos, sampleIndex);
}