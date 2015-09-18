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

// http://aras-p.info/texts/CompactNormalStorage.html
// Lambert azimuthal equal-area projection
// http://en.wikipedia.org/wiki/Lambert_azimuthal_equal-area_projection
// Lambert azimuthal equal-area projection
// with normalized N is equivalent to 
// Spheremap Transform but slightly faster
float2 EncodeAzimuthal(in float3 N)
{
    // Lambert azimuthal equal-area projection
    // with normalized N equivalent to Spheremap Transform
    // but couple of ops less
    float f = sqrt(8*N.z+8);
    return N.xy / f + 0.5;
}

uint PackNormal(in float3 n)
{
    float2 smN = EncodeAzimuthal(n);

    // Pack float2 into uint
    uint result = 0;
    result = f32tof16(smN.x);
    result |= f32tof16(smN.y) << 16;

    return result;
}

float3 DecodeAzimuthal(in float2 enc)
{
    // Lambert azimuthal equal-area projection
    float2 fenc = enc*4-2;
    float f = dot(fenc,fenc);
    float g = sqrt(1-f/4);
    float3 n;
    n.xy = fenc*g;
    n.z = 1-f/2;
    return n;
}

float3 UnpackNormal(in uint packedN)
{
	// Unpack uint to float2
    float2 unpack;
	unpack.x = f16tof32(packedN);
	unpack.y = f16tof32(packedN >> 16);
    // Decode spheremap xfrm
    return DecodeAzimuthal(unpack);
}

