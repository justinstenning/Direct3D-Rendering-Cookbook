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

Texture2D<float4> input : register(t0);
RWTexture2D<float4> output : register(u0);
RWByteAddressBuffer outputByteBuffer : register(u0);

// Linear Sampler
//SamplerState linSamp : register(s0);

cbuffer ComputeConstants : register(b0)
{
    float LerpT;
};

// Luminance Coefficients
// Used to calculate the relative luminance for a color 
// (linear combination of RGB with weighting for each color) 
// http://en.wikipedia.org/wiki/Luminance_%28relative%29
// Note: there are other coefficients that can be used depending on color space
// http://en.wikipedia.org/wiki/Grayscale
//#define LUMINANCE float3(0.3, 0.59, 0.11); // used for YUV / YIQ color models
#define LUMINANCE_RGB float3(0.2125, 0.7154, 0.0721) // used for RGB/sRGB color models
#define LUMINANCE(_V) dot(_V.rgb, LUMINANCE_RGB)

// The total group size
static const uint GROUPSIZE = THREADSX * THREADSY;
// Helper methods to preserve alpha for lerp
float4 lerpKeepAlpha(float4 source, float3 target, float T)
{
    return float4(lerp(source.rgb, target, T), source.a);
}

float4 lerpKeepAlpha(float3 source, float4 target, float T)
{
    return float4(lerp(source, target.rgb, T), target.a);
}

// Desaturate the input
// The result is returned in output
[numthreads(THREADSX, THREADSY, 1)]
void DesaturateCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Calculate the relative luminance
    float3 target = (float3)LUMINANCE(sample.rgb);
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, LerpT);
}

// Saturate the input, exact same as DesaturateCS except in the opposite direction
[numthreads(THREADSX, THREADSY, 1)]
void SaturateCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Calculate the relative luminance
    float3 target = (float3)LUMINANCE(sample.rgb);
    output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, LerpT);
}

// Create Negative of the input
// The result is returned in output
[numthreads(THREADSX, THREADSY, 1)]
void NegativeCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Create negative by subtracting the color from White
    float3 target = float3(1.0,1.0,1.0)-sample.rgb;
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, LerpT);
}

[numthreads(THREADSX, THREADSY, 1)]
void ContrastCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Adjust contrast by moving towards or away from Gray
    // Note: if LerpT == -1, we achieve a negative image
    float3 target = float3(0.5,0.5,0.5);
    output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, LerpT);
}

[numthreads(THREADSX, THREADSY, 1)]
void BrightnessCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Adjust brightness by adding or removing Black
    float3 target = float3(0,0,0);
    output[dispatchThreadId.xy] = lerpKeepAlpha(target, sample, LerpT);
}

[numthreads(THREADSX, THREADSY, 1)]
void SepiaCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];

    float3 target;
    target.r = saturate(dot(sample.rgb, float3(0.393, 0.769, 0.189)));
    target.g = saturate(dot(sample.rgb, float3(0.349, 0.686, 0.168)));
    target.b = saturate(dot(sample.rgb, float3(0.272, 0.534, 0.131)));

    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, LerpT);
}

#define FILTERTAP 9
#define FILTERRADIUS ((FILTERTAP-1)/2)

// Shared memory for storing thread group data for filters
// with enough room for GROUPSIZE + (THREADSY * FILTERRADIUS*2)
// Note: at a thread group size of 1024, the maximum FILTERTAP possible is 33
// Max size of groupshared is 32KB
groupshared float4 FilterGroupMemX[GROUPSIZE + (THREADSY * FILTERRADIUS*2)];
groupshared float4 FilterGroupMemY[GROUPSIZE + (THREADSX * FILTERRADIUS*2)];

// The blur kernel. The middle element represents the current pixel
//static const float BlurKernel[FILTERTAP] = (float[FILTERTAP])(1.0/(FILTERTAP));
static const float BlurKernel[FILTERTAP] = 
{
//    -0.333333, 1.666666, -0.333333 // Sharpen the image
    //0.2740686, 0.4518628, 0.2740686 // 3-tap Gaussian
//     0.1524691, 0.2218413, 0.2513791, 0.2218413, 0.1524691 // 5-tap Gaussian
     0.08167442, 0.1016454, 0.1188356, 0.1305153, 0.1346584, 0.1305153, 0.1188356, 0.1016454, 0.08167442 // 9-tap Gaussian
//     0.06634167, 0.07942539, 0.09136095, 0.1009695, 0.1072131, 0.1093789, 0.1072131, 0.1009695, 0.09136095, 0.07942539, 0.06634167 // 11-tap Gaussian
};

// Blur filter - loads all samples for a group 
// into FilterGroupMem, then performs blur
[numthreads(THREADSX, THREADSY, 1)]
void BlurFilterHorizontalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    // Note: groupIndex = groupThreadId.z * 1 * 1 + groupThreadId.y * THREADSY + groupThreadId.x

    uint offsetGroupIndex = groupIndex + (groupThreadId.y * 2 * FILTERRADIUS) + FILTERRADIUS;

    // Group Memory layout:
    // Given FILTERTAP == 7 && GROUPSIZE == 1024 (i.e. 32x32 thread group size)
    // groupThreadId.x -> 0...THREADSX
    // [ -RADIUS   ][-----GroupIndex------][  +RADIUS  ]
    // [-3 |-2 |-1 ][ 0| 1| 2|...|29|30|31][ 1 | 2 | 3 ]
    // [-3 |-2 |-1 ][32|33|34|...|61|62|63][ 1 | 2 | 3 ]
    //               ...               ...
    // [-3 |-2 |-1 ][992| |  |...| | |1023][ 1 | 2 | 3 ]
    // [<--------extra sample]...[extra sample-------->]

    // 1. sample the texel for this thread
    // Clamp out of bound samples that occur at image borders (i.e. if xy > image.Length.xy - 1, set to image.Length.xy-1)
    // Note: the clamping will only occur if the number of threads created is too large for the provided image
	FilterGroupMemX[offsetGroupIndex] = input[min(dispatchThreadId.xy, input.Length.xy - 1)];

    // 2. If thread is within FILTERRADIUS of thread group boundary, sample an additional texel.
    // 2a. additional texel @ dispatchThreadId.x – FILTERTAP
    
    // A thread group runs GROUPSIZE threads.  To get the extra 2 * FILTERTAP pixels 
    // for the border cases, have the 2 * FILTERTAP threads near the edge of the 
    // the thread group X bounds sample an extra texel each.
	if (groupThreadId.x < FILTERRADIUS)
	{
		// Clamp out of bound samples that occur at image borders (i.e. if x < 0, set to 0).
		int x = dispatchThreadId.x - FILTERRADIUS;
		FilterGroupMemX[offsetGroupIndex - FILTERRADIUS] = input[int2(max(x, 0), dispatchThreadId.y)];
	}

	if(groupThreadId.x >= THREADSX - FILTERRADIUS)
	{
		// Clamp out of bound samples that occur at image borders (i.e. if x > imageWidth-1, set to imageWidth-1)
		int x = dispatchThreadId.x + FILTERRADIUS;
		FilterGroupMemX[offsetGroupIndex + FILTERRADIUS] = input[int2(min(x, input.Length.x - 1), dispatchThreadId.y)];
	}

    // 3. Wait for all threads in group to complete sampling
    GroupMemoryBarrierWithGroupSync();

    // 4. Apply blur kernel to the current texel using the 
    //    samples we have already loaded for this thread group
	float4 result = float4(0, 0, 0, 0);
	
    int centerPixel = offsetGroupIndex;
	[unroll]
	for(int i = -FILTERRADIUS; i <= FILTERRADIUS; ++i)
	{
 		int j = centerPixel + i;
		result += BlurKernel[i + FILTERRADIUS] * FilterGroupMemX[j];
 	}
	output[dispatchThreadId.xy] = lerp(FilterGroupMemX[offsetGroupIndex], result, LerpT);
}

[numthreads(THREADSX, THREADSY, 1)]
void BlurFilterVerticalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    // Group Memory layout:
    // Given FILTERRADIUS == 3 && GROUPSIZE == 1024 (i.e. 32x32 thread group size)
    // groupThreadId.x -> 0...THREADSX
    // [                     ] 3
    // [  -RADIUS*THREADSX   ] 2
    // [                     ] 1
    // [ 0| 1| 2|...|29|30|31]
    // [32|33|34|...|61|62|63]
    //  ------GroupIndex-----
    // [992| |  |...| | |1023]
    // [                     ] 1
    // [   +RADIUS*THREADSX  ] 2
    // [                     ] 3
    
    // Note: groupIndex = groupThreadId.z * 1 * 1 + groupThreadId.y * THREADSY + groupThreadId.x
    uint yOffset = FILTERRADIUS * THREADSX;
    uint offsetGroupIndex = groupIndex + yOffset;
    
    // 1. Sample the current texel (clamp to max input coord)
    // Clamp out of bound samples that occur at image borders (i.e. if xy > image.Length.xy - 1, set to image.Length.xy-1)
    // Note: the clamping will only occur if the number of threads created is too large for the provided image
    //       using clamping means that we don't have to worry about getting the exactly correct dimensions for
    //       the thread dispatch.
    FilterGroupMemY[offsetGroupIndex] = input[min(dispatchThreadId.xy, input.Length.xy - 1)];

    // 2. If thread is within FILTERRADIUS of thread group boundary, sample an additional texel.
    // A thread group runs GROUPSIZE threads.  To get the extra 2 * FILTERTAP pixels 
    // for the border cases, have the 2 * FILTERRADIUS threads near the edge of the 
    // the thread group Y boundary sample an extra texel each.

    // 2a. additional texel @ dispatchThreadId.y – FILTERRADIUS
    if (groupThreadId.y < FILTERRADIUS)
    {
	    // Clamp out of bound samples that occur at image borders (i.e. if y < 0, set to 0).
	    int y = dispatchThreadId.y - FILTERRADIUS;
	    FilterGroupMemY[offsetGroupIndex - yOffset] = input[int2(dispatchThreadId.x, max(y, 0))];
    }

    // 2b. additional texel @ dispatchThreadId.y + FILTERRADIUS 
    if(groupThreadId.y >= THREADSY - FILTERRADIUS)
    {
	    // Clamp out of bound samples that occur at image borders (i.e. if y > imageWidth-1, set to imageWidth-1)
	    int y = dispatchThreadId.y + FILTERRADIUS;
	    FilterGroupMemY[offsetGroupIndex + yOffset] = input[int2(dispatchThreadId.x, min(y, input.Length.y - 1))];
    }

    // 3. Wait for all threads in group to complete sampling
    GroupMemoryBarrierWithGroupSync();

    // 4. Apply blur kernel to the current texel using the 
    //    samples we have already loaded for this thread group
    float4 result = float4(0, 0, 0, 0);
    int index = offsetGroupIndex - yOffset;
    [unroll]
    for(int i = -FILTERRADIUS; i <= FILTERRADIUS; ++i)
    {
	    result += BlurKernel[i + FILTERRADIUS] * FilterGroupMemY[index];
        index += THREADSX;
    }
    // Write the result to the output
    output[dispatchThreadId.xy] = lerp(FilterGroupMemY[offsetGroupIndex], result, LerpT);
}


float3 BoxFilter_3Tap(float3 c1, float3 c2, float3 c3)
{
    return (c1 + c2 + c3) / 3;
}

[numthreads(THREADSX, THREADSY, 1)]
void BoxFilter3TapHorizontalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(-1.0, 0)].rgb;
    float4 p2 = input[dispatchThreadId.xy];
    float3 p3 = input[dispatchThreadId.xy + float2(1.0, 0)].rgb;

    output[dispatchThreadId.xy] = float4(BoxFilter_3Tap(p1, p2.rgb, p3), p2.a);
}

[numthreads(THREADSX, THREADSY, 1)]
void BoxFilter3TapVerticalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(0, -1.0)].rgb;
    float4 p2 = input[dispatchThreadId.xy];
    float3 p3 = input[dispatchThreadId.xy + float2(0, 1.0)].rgb;

    output[dispatchThreadId.xy] = float4(BoxFilter_3Tap(p1, p2.rgb, p3), p2.a);
}

float3 BoxFilter_5Tap(float3 c1, float3 c2, float3 c3, float3 c4, float3 c5)
{
    return (c1 + c2 + c3 + c4 + c5) / 5;
}

[numthreads(THREADSX, THREADSY, 1)]
void BoxFilter5TapHorizontalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(-2.0, 0)].rgb;
    float3 p2 = input[dispatchThreadId.xy + float2(-1.0, 0)].rgb;
    float4 p3 = input[dispatchThreadId.xy];
    float3 p4 = input[dispatchThreadId.xy + float2(1.0, 0)].rgb;
    float3 p5 = input[dispatchThreadId.xy + float2(2.0, 0)].rgb;

    output[dispatchThreadId.xy] = float4(BoxFilter_5Tap(p1, p2, p3.rgb, p4, p5), p3.a);
}

[numthreads(THREADSX, THREADSY, 1)]
void BoxFilter5TapVerticalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(0, -2.0)].rgb;
    float3 p2 = input[dispatchThreadId.xy + float2(0, -1.0)].rgb;
    float4 p3 = input[dispatchThreadId.xy];
    float3 p4 = input[dispatchThreadId.xy + float2(0, 1.0)].rgb;
    float3 p5 = input[dispatchThreadId.xy + float2(0, 2.0)].rgb;

    output[dispatchThreadId.xy] = float4(BoxFilter_5Tap(p1, p2, p3.rgb, p4, p5), p3.a);
}

float3 FindMedian_3Tap(float3 c1, float3 c2, float3 c3)
{
    float3 median; 
    if (c1.x < c2.x) 
    { 
        if(c2.x < c3.x) 
            median = c2; 
        else if (c1.x < c3.x) 
            median = c3; 
        else
            median = c1; 
    } 
    else 
    { 
        if(c1.x < c3.x) 
            median = c1; 
        else if (c2.x < c3.x) 
            median = c3; 
        else
            median = c2; 
    }
    return median; 
}

[numthreads(THREADSX, THREADSY, 1)]
void ApproxMedianVerticalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(0, -1.0)].rgb;
    float4 p2 = input[dispatchThreadId.xy];
    float3 p3 = input[dispatchThreadId.xy + float2(0, 1.0)].rgb;

    output[dispatchThreadId.xy] = float4((float3)FindMedian_3Tap(p1, p2.rgb, p3), p2.a);
}

[numthreads(THREADSX, THREADSY, 1)]
void ApproxMedianHorizontalCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 p1 = input[dispatchThreadId.xy + float2(-1.0, 0)].rgb;
    float4 p2 = input[dispatchThreadId.xy];
    float3 p3 = input[dispatchThreadId.xy + float2(1.0, 0)].rgb;

    output[dispatchThreadId.xy] = float4((float3)FindMedian_3Tap(p1, p2.rgb, p3), p2.a);
}

/*
Justin Stenning
2013-08-30 - Median Filter adapted from 
---------------------------------------
3x3 Median optimized for GeForce 8800

Copyright (c) Morgan McGuire and Williams College, 2006
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/
#define s2(a, b)                  temp = a; a = min(a, b); b = max(temp, b);
#define mn3(a, b, c)              s2(a, b); s2(a, c);
#define mx3(a, b, c)              s2(b, c); s2(a, c);

#define exMnMx3(a, b, c)          mx3(a, b, c); s2(a, b);                                   // 3 exchanges
#define exMnMx4(a, b, c, d)       s2(a, b); s2(c, d); s2(a, c); s2(b, d);                   // 4 exchanges
#define exMnMx5(a, b, c, d, e)    s2(a, b); s2(c, d); mn3(a, c, e); mx3(b, d, e);           // 6 exchanges
#define exMnMx6(a, b, c, d, e, f) s2(a, d); s2(b, e); s2(c, f); mn3(a, b, c); mx3(d, e, f); // 7 exchanges

[numthreads(THREADSX, THREADSY, 1)]
void Median3x3TapSinglePassCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float3 v[6];

    // Median 3x3 Tap convolution filter
    //
    // v0  v1  v2
    // v3 (v4) v5
    // v6  v7  v8 -> v6,7,8 are each loaded into v5
    // 

    // Load the first 2 rows of the convolution filter
    v[0] = input[dispatchThreadId.xy + float2(-1.0, -1.0)].rgb;
    v[1] = input[dispatchThreadId.xy + float2( 0.0, -1.0)].rgb;
    v[2] = input[dispatchThreadId.xy + float2(+1.0, -1.0)].rgb;
    v[3] = input[dispatchThreadId.xy + float2(-1.0,  0.0)].rgb;
    v[4] = input[dispatchThreadId.xy + float2( 0.0,  0.0)].rgb;
    v[5] = input[dispatchThreadId.xy + float2(+1.0,  0.0)].rgb;

    // Starting with a subset of size 6, remove the min and max each time
    // The min ends up in v[0], and max ends up in v[5] each time
    // We discard v[0], and load another sample into v[5]
    // for the next 3 iterations.
    float3 temp;
    exMnMx6(v[0], v[1], v[2], v[3], v[4], v[5]);
    //       Min                           Max

    v[5] = input[dispatchThreadId.xy + float2(-1.0, +1.0)].rgb;
    exMnMx5(v[1], v[2], v[3], v[4], v[5]);
    //       Min                     Max

    v[5] = input[dispatchThreadId.xy + float2( 0.0, +1.0)].rgb;
    exMnMx4(v[2], v[3], v[4], v[5]);
    //       Min               Max

    v[5] = input[dispatchThreadId.xy + float2(+1.0, +1.0)].rgb;
    exMnMx3(v[3], v[4], v[5]);
    //      Min  Median  Max

    output[dispatchThreadId.xy] = float4(v[4], 1);
}

// Execute Sobel Edge Filter (3x3 tap) for current pixel
// coord (the current pixel), 
// threshold (higher -> more noise), 
// thickness (how far to get pixels)
float SobelEdge(float2 coord, float threshold, float thickness)
{
    // Sobel 3x3 tap filter: approximate magnitude
    // Cheaper than the full Sobel kernel evaluation
    // http://homepages.inf.ed.ac.uk/rbf/HIPR2/sobel.htm
    // ------------------------------
    // p1  p2  p3      00  01  02   | x
    // p4 (p5) p6 <==> 10 (11) 12   | convolution kernel
    // p7  p8  p9      20  21  22   |
    // ------------------------------
    // Gx  = (p1 + 2 * p2 + p3) - (p7 + 2 * p8 + p9)
    // ------------------------------
    // p3  p6  p9      02  12  22   | y (x rotated anti-clockwise)
    // p2 (p5) p8 <==> 01 (11) 21   | convolution kernel
    // p1  p4  p7      00  10  20   |
    // ------------------------------
    // Gy  = (p3 + 2 * p6 + p9) - (p1 + 2 * p4 + p7)
    // ------------------------------
    // Formula:
    // |G| = |Gx| + |Gy| => pow(G,2) = Gx*Gx + Gy*Gy
    // |G| = |(p1 + 2 * p2 + p3) - (p7 + 2 * p8 + p9)| + |(p3 + 2 * p6 + p9) - (p1 + 2 * p4 + p7)|
    //
    // p5 == current pixel, 
    // sample the neighbouring pixels to create 3x3 convolution kernel
    float p1 = LUMINANCE(input[coord + float2(-thickness, -thickness)]);
    float p2 = LUMINANCE(input[coord + float2( 0, -thickness)]);
    float p3 = LUMINANCE(input[coord + float2( thickness, -thickness)]);
    float p4 = LUMINANCE(input[coord + float2(-thickness, 0)]);
    float p6 = LUMINANCE(input[coord + float2( thickness, 0)]);
    float p7 = LUMINANCE(input[coord + float2(-thickness, thickness)]);
    float p8 = LUMINANCE(input[coord + float2( 0, thickness)]);
    float p9 = LUMINANCE(input[coord + float2( thickness, thickness)]);

    //float sobelX = (p1 + 2 * p2 + p3) - (p7 + 2 * p8 + p9);
    //float sobelY = (p3 + 2 * p6 + p9) - (p1 + 2 * p4 + p7);
    float sobelX = mad(2, p2, p1 + p3) - mad(2, p8, p7 + p9);
    float sobelY = mad(2, p6, p3 + p9) - mad(2, p4, p1 + p7);


    float edgeSqr = (sobelX * sobelX + sobelY * sobelY);
    float result = 1.0 - (edgeSqr > threshold * threshold); // if (edgeSqr > threshold * threshold) { edge }
    return result; // black (0) = edge, otherwise white (1)
}

[numthreads(THREADSX, THREADSY, 1)]
void SobelEdgeOverlayCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];

    float threshold = 0.4f;
    float thickness = 1;

    float3 target = sample.rgb * SobelEdge(dispatchThreadId.xy, threshold, thickness);
    output[dispatchThreadId.xy] = lerpKeepAlpha(sample, target, LerpT);
}

[numthreads(THREADSX, THREADSY, 1)]
void SobelEdgeCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float threshold = 0.4f;
    float thickness = 1;

    output[dispatchThreadId.xy] = float4((float3)SobelEdge(dispatchThreadId.xy, threshold, thickness), 1);
}

// Calculate the luminance of the input
// Output to outputByteBuffer
[numthreads(THREADSX, THREADSY, 1)]
void HistogramCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    // Calculate the Relative luminance (and adjust to 0-255 range)
    float luminance = LUMINANCE(sample.xyz) * 255.0;
    
    // Addressable as bytes, so multiply by 4 to store 32-bit integers
    // Atomic increment of value at address.
    outputByteBuffer.InterlockedAdd((uint)luminance * 4, 1);
}

// Example of an erroneous version of HistogramGroupShared trying to use groupshared memory
groupshared uint SharedHistogram[256] = (uint[256])0;
[numthreads(THREADSX, THREADSY, 1)]
void HistogramGroupSharedCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    float4 sample = input[dispatchThreadId.xy];
    float luminance = LUMINANCE(sample.xyz) * 255.0;
    
    // IMPORTANT: We can't do this as a thread can only write to its own region
    //            of the shared memory.
    SharedHistogram[luminance]++;

    // Wait for all threads in group to complete up to this point
    GroupMemoryBarrierWithGroupSync();

    // Calculate the id of the thread within the group
    const uint threadId = groupThreadId.x + groupThreadId.y;
    // If the first thread for this group, copy shared result into output
    if (threadId == 0)
    {
        [unroll(256)]
        for (int i = 0; i < 256; i++)
        {
            outputByteBuffer.InterlockedAdd((uint)i * 4, SharedHistogram[i]);
        }
    }
}


// Fast(er) RGB<->HSV conversion functions by Ian Taylor
// http://chilliant.blogspot.com.au/2010/11/rgbhsv-in-hlsl.html
float3 RGBtoHSV(in float3 RGB)
{
    float3 HSV = 0;
//#if NO_ASM
    HSV.z = max(RGB.r, max(RGB.g, RGB.b));
    float M = min(RGB.r, min(RGB.g, RGB.b));
    float C = HSV.z - M;
//#else
//    float4 RGBM = RGB.rgbr;
//    asm { max4 HSV.z, RGBM };
//    asm { max4 RGBM.w, -RGBM };
//    float C = HSV.z + RGBM.w;
//#endif
    if (C != 0)
    {
        HSV.y = C / HSV.z;
        float3 Delta = (HSV.z - RGB) / C;
        Delta.rgb -= Delta.brg;
        Delta.rg += float2(2,4);
        if (RGB.r >= HSV.z)
            HSV.x = Delta.b;
        else if (RGB.g >= HSV.z)
            HSV.x = Delta.r;
        else
            HSV.x = Delta.g;
        HSV.x = frac(HSV.x / 6);
    }
    return HSV;
}

float3 Hue(float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 HSVtoRGB(in float3 HSV)
{
    return ((Hue(HSV.x) - 1) * HSV.y + 1) * HSV.z;
}


/**************************************************************/
// If the shader must simultaneously read and write to a UAV
// of a texture, then it is necessary to use the UINT format 
// (i.e. R32_Typeless). An in-place editing version
// of the DesaturateCS below shows how to use this instead of 
// input and output resources.
RWTexture2D<uint> inputRW : register(u0);
// Include the DX SDK 32-bit pack/unpacking code
// See also: http://msdn.microsoft.com/en-us/library/windows/desktop/ff728749(v=vs.85).aspx
#include "D3DX_DXGIFormatConvert.inl"
// Version of DesaturateCS that works with inputRW for input and output
[numthreads(THREADSX, THREADSY, 1)]
void InplaceDesaturateCS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    // Use the D3DX DXGI format conversion function
    float4 sample = D3DX_R8G8B8A8_UNORM_to_FLOAT4(inputRW[dispatchThreadId.xy]);
    // Calculate the Relative luminance
    sample.rgb = (float3)LUMINANCE(sample.rgb);
    inputRW[dispatchThreadId.xy] = D3DX_FLOAT4_to_R8G8B8A8_UNORM(sample);
}
