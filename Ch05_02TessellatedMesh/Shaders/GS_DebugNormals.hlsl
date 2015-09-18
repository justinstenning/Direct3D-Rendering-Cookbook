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

// Total output size is 1024bytes, therefore we have to reduce the maxvertexcount each time the PixelShaderInput
// structure increases in size.
[maxvertexcount(30)]
void GSMain(triangle PixelShaderInput input[3], inout LineStream<PixelShaderInput> OutputStream)
{
    PixelShaderInput output = (PixelShaderInput)0;

    // now create three new normal rectangles, one for each vertex
    for (uint j=0; j < 3; j++)
    {
        float3 pos = input[j].WorldPosition;
        float3 normal = normalize(input[j].WorldNormal);

        // calculate tangent
        float3 tangent; 
        float3 bitangent;

        float3 c1 = cross(normal, float3(0.0, 0.0, 1.0)); 
        float3 c2 = cross(normal, float3(0.0, 1.0, 0.0)); 

        if( length(c1) > length(c2) )
        {
            tangent = c1;
        }
        else
        {
            tangent = c2;	
        }

        tangent = normalize(tangent);
        bitangent = normalize(cross(tangent, normal));
        
        // Set the new geometry width and height
        float3 nl = normal * 0.02;// full height
        float3 tf = tangent * 0.02; // 1/2 width (tangent direction)
        float3 btf = bitangent * 0.02; // 1/2 width (bitangent direction)

        float3 p[6];
        p[0] = pos + tf;
        p[1] = pos;
        p[2] = pos + btf;
        p[3] = pos;
        p[4] = pos + nl;
        p[5] = pos;

        output = (PixelShaderInput)0;
        output.Diffuse = float4(1,0,0,1);

        output.Position = mul(float4(p[0], 1), ViewProjection);
        OutputStream.Append(output);
        output.Position = mul(float4(p[1], 1), ViewProjection);
        OutputStream.Append(output);
        OutputStream.RestartStrip();

        output.Diffuse = float4(0,1,0,1);
        output.Position = mul(float4(p[2], 1), ViewProjection);
        OutputStream.Append(output);
        output.Position = mul(float4(p[3], 1), ViewProjection);
        OutputStream.Append(output);
        OutputStream.RestartStrip();

        output.Diffuse = float4(0,0,1,1);
        output.Position = mul(float4(p[4], 1), ViewProjection);
        OutputStream.Append(output);
        output.Position = mul(float4(p[5], 1), ViewProjection);
        OutputStream.Append(output);
        OutputStream.RestartStrip();
    }
}

//void GSMain(triangle PixelShaderInput input[3], inout TriangleStream<PixelShaderInput> OutputStream)
//{
//    PixelShaderInput output = (PixelShaderInput)0;
//
//    // output original triangle unchanged
//    for (uint i=0; i < 3; i++)
//    {
//        OutputStream.Append(input[i]);
//    }
//
//    OutputStream.RestartStrip();
//
//    // now create three new normal rectangles, one for each vertex
//    for (uint j=0; j < 3; j++)
//    {
//        float3 pos = input[j].WorldPosition;
//        float3 normal = normalize(input[j].WorldNormal);
//
//        // calculate tangent
//        float3 tangent; 
//        float3 bitangent;
//
//        float3 c1 = cross(normal, float3(0.0, 0.0, 1.0)); 
//        float3 c2 = cross(normal, float3(0.0, 1.0, 0.0)); 
//
//        if( length(c1) > length(c2) )
//        {
//            tangent = c1;
//        }
//        else
//        {
//            tangent = c2;	
//        }
//        tangent = normalize(tangent);
//        bitangent = normalize(cross(tangent, normal));
//        
//        /* Create 12 new triangles to represent the rectangular prism of the normal
//        Triangle strip:
//        *2 *0(start)
//        |\ |                   
//        | \|                  <= top
//        |  *1    0   2   3
//        *3  *5  *7  *9  *11
//        |  /|  /|  /|  /|     <= body of rectangular prism
//        | / | / | / | / |     
//        |/  |/  |/  |/  | 4
//        *4  *6  *8  *10 *12  6
//                        |  *14
//                        | /|  <= bottom (excluded due to 1024 scalar limit on GS output stream)
//                      10|/ | 8
//                      13*  *15 (end)
//        */
//
//        // Set the new geometry width and height
//        float3 nl = normal * 0.1;// full height
//        float3 tf = tangent * 0.00125; // 1/2 width (tangent direction)
//        float3 btf = bitangent * 0.00125; // 1/2 width (bitangent direction)
//
//        float3 p[16];
//        p[0] = pos + nl + tf + btf;
//        p[1] = pos + nl + tf - btf;
//        p[2] = pos + nl - tf + btf;
//        p[3] = pos + nl - tf - btf; // TOP
//
//        p[4] = pos - tf - btf;
//        p[5] = p[1];
//        p[6] = pos + tf - btf;
//        p[7] = p[0];
//        p[8] = pos + tf + btf;
//        p[9] = p[2];
//        p[10] = pos - tf + btf;
//        p[11] = p[3];
//        
//        p[12] = p[4];  
//        /*p[13] = p[10]; // bottom
//        p[14] = p[6];
//        p[15] = p[8];*/
//
//        for (uint k = 0; k < 13; k++)
//        {
//            output = (PixelShaderInput)0;
//            output.Position = mul(float4(p[k], 1), ViewProjection);
//            output.Diffuse = float4(0,1,0,1);
//            OutputStream.Append(output);
//        }
//
//        OutputStream.RestartStrip();
//    }
//}