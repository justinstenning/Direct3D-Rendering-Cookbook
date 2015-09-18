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

#include "Particle.hlsl"

AppendStructuredBuffer<Particle>  NewState     : register( u0 );
ConsumeStructuredBuffer<Particle> CurrentState : register( u1 );

//uint rand_xorshift(inout uint rng_state)
//{
//    // Xorshift algorithm from George Marsaglia's paper
//    // http://core.kmi.open.ac.uk/download/pdf/6250138.pdf
//    rng_state ^= (rng_state << 13);
//    rng_state ^= (rng_state >> 17);
//    rng_state ^= (rng_state << 5);
//    return rng_state;
//}
uint rand_lcg(inout uint rng_state)
{
    // LCG values from Numerical Recipes
    rng_state = 1664525 * rng_state + 1013904223;
    return rng_state;
}
uint wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

[numthreads(THREADSX, 1, 1)]
void Generator(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint indx = dispatchThreadId.x + dispatchThreadId.y * THREADSX;
    Particle p = (Particle)0;

    // Initialize random seed
    uint rng_state = wang_hash(RandomSeed + indx);

    // Random float between [0, 1]
    float f0 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    float f1 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    float f2 = float(rand_lcg(rng_state)) * (1.0 / 4294967296.0);
    
    p.Radius = Radius;
	p.Position.x = DomainBoundsMin.x + f0 * ((DomainBoundsMax.x - DomainBoundsMin.x) + 1);
    p.Position.z = DomainBoundsMin.z + f1 * ((DomainBoundsMax.z - DomainBoundsMin.z) + 1);
    p.Position.y = (DomainBoundsMax.y - 2) + f2 * ((DomainBoundsMax.y - (DomainBoundsMax.y-2)) + 1);
    p.OldPosition = p.Position;
    p.Energy = MaxLifetime;
	
	// Append the new particle to the output buffer
	NewState.Append(p);
}

// Implementation based on the Gerstner Wave formula from
// The elements of nature: interactive and realistic techniques - "Simulating Ocean Water - Jerry Tessendorf"
// http://dl.acm.org/citation.cfm?id=1103900.1103932&coll=DL&dl=GUIDE&CFID=240542608&CFTOKEN=74987873
void GerstnerWaveTessendorf(float waveLength, float speed, float amplitude, float steepness, float2 direction, in float3 position, inout float3 result)
{
    float L = waveLength; // wave crest to crest length in metres
    float A = amplitude; // amplitude - wave height (crest to trough)
    float k = 2.0 * 3.1416 / L; // wave length
    float kA = k*A;
    float2 D = normalize(direction); // normalized direction
    float2 K = D * k; // wave vector and magnitude (direction)

    // peak/crest steepness high means steeper, but too much 
    // can cause the wave to become inside out at the top
    float Q = steepness;//max(steepness, 0.1); 

    float S = speed * 0.5; // Speed 1 =~ 2m/s so halve first
    float w = S * k; // Phase/frequency
    float wT = w * Time;

    // Calculate once instead of 4 times
    float KPwT = dot(K, position.xz)-wT;
    float S0 = sin(KPwT);
    float C0 = cos(KPwT);

    // Calculate the vertex offset along the X and Z axes
    float2 xz = position.xz - D*Q*A*S0;
    // Calculate the vertex offset along the Y (up/down) axis
    float y = A*C0;

    // Append the results
    result.xz += xz;
    result.y += y;
}

// Apply ForceDirection with ForceStrength to particle
void ApplyForces(inout Particle particle)
{
    // Forces
    float3 force = (float3)0;

    // Directional force
    force += normalize(ForceDirection) * ForceStrength; //ForceDirection * ForceStrength;
    
    // Damping
    float speedCoeff = 0.9;
    
    force *= speedCoeff;

    particle.OldPosition = particle.Position;

    // Integration step
    particle.Position += force * FrameTime;
}

// The result is returned in output
[numthreads(THREADSX, THREADSY, 1)]
void CS(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint indx = dispatchThreadId.x + dispatchThreadId.y * THREADSX;
    // Skip out of bounds threads
    if (indx < ParticleCount)
    {
        // Load particle
        Particle p = CurrentState.Consume();
    
        ApplyForces(p);
    
        // Count down the particles time to live
        p.Energy -= FrameTime;

        if (p.Energy > 0) {
            // Update particle
            NewState.Append(p);
        }
    }
}


[numthreads(THREADSX, THREADSY, 1)]
void Waves(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint indx = dispatchThreadId.x + dispatchThreadId.y * THREADSX;
    // Skip out of bounds threads
    if (indx >= ParticleCount)
        return;
    // Load/Consume particle
    Particle p = CurrentState.Consume();

    p.Position = (float3)0;
    float2 direction = float2(1,0);
    GerstnerWaveTessendorf(10, 2, 2.5, 0.5, direction, p.OldPosition, p.Position);
    GerstnerWaveTessendorf(5, 1.2, 2, 0.5, direction, p.OldPosition, p.Position);
    GerstnerWaveTessendorf(4, 2, 2, 0.5, direction + float2(0, 1), p.OldPosition, p.Position);
    GerstnerWaveTessendorf(4, 1, 0.5, 0.5, direction + float2(0, 1), p.OldPosition, p.Position);
    GerstnerWaveTessendorf(2.5, 2, 0.5, 0.5, direction + float2(0, 0.5), p.OldPosition, p.Position);
    GerstnerWaveTessendorf(2, 2, 0.5, 0.5, direction, p.OldPosition, p.Position);
    //p.Position = p.OldPosition + p.Position;
    
    // Count down the particles time to live
    p.Energy -= FrameTime;

    if (p.Energy > 0) {
        // Update particle
        NewState.Append(p);
    }
}

[numthreads(THREADSX, THREADSY, 1)]
void Snowfall(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint indx = dispatchThreadId.x + dispatchThreadId.y * THREADSX;
    // Skip out of bounds threads
    if (indx >= ParticleCount)
        return;

    // Load particle
    Particle p = CurrentState.Consume();

    ApplyForces(p);

    // TODO - sample a depth texture here of ortho looking
    // down upon scene and set new height to max of pos or depth
    p.Position.y = max(p.Position.y, DomainBoundsMin.y);

    // Count down the particles time to live
    p.Energy -= FrameTime;

    // If no longer falling only let it 
    // sit on the ground for a second
    if (p.Position.y == p.OldPosition.y && p.Energy > 1.0f) {
        p.Energy = 1.0f;
    }

    if (p.Energy > 0) {
        // Update particle
        NewState.Append(p);
    }
}

[numthreads(THREADSX, THREADSY, 1)]
void Sweeping(uint groupIndex: SV_GroupIndex, uint3 groupId : SV_GroupID, uint3 groupThreadId: SV_GroupThreadID, uint3 dispatchThreadId : SV_DispatchThreadID)
{
    uint indx = dispatchThreadId.x + dispatchThreadId.y * THREADSX;
    // Skip out of bounds threads
    if (indx >= ParticleCount)
        return;

    // Load particle
    Particle p = CurrentState.Consume();

	float3 a;
	float3 newPos;

	if(all(Attractor)){//.x != 0 && attractor.y != 0 && attractor.z != 0){
		a = Attractor - p.Position - float3(1,-1, 0);
		a = normalize(a)*5*length(p.Position);
	} else {
		a = Attractor - float3(0,0.5,0);
	}

	newPos = 2*p.Position - p.OldPosition + a*FrameTime*FrameTime;
	p.OldPosition = p.Position;
	p.Position = newPos;
			
	//Keep all the particles inside bounds
    if(length(p.Position) > distance(DomainBoundsMax,DomainBoundsMin)) {
		float3 norm = normalize(p.Position);
		p.Position = norm*distance(DomainBoundsMax,DomainBoundsMin);
	}
    
    // Count down the particles time to live
    p.Energy -= FrameTime;

    if (p.Energy > 0) {
        // Update particle
        NewState.Append(p);
    }
}