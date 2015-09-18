using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch08_02Particles
{
    public class ParticleRenderer : RendererBase
    {
        // Structure for particle
        public struct Particle
        {
            public Vector3 Position;
            public float Radius;
            public Vector3 OldPosition;
            public float Energy;
        }

        // Constant buffer for compute shaders
        public struct ParticleConstants
        {
            public Vector3 DomainBoundsMin;
            public float ForceStrength;
            public Vector3 DomainBoundsMax;
            public float MaxLifetime;
            public Vector3 ForceDirection;
            public int MaxParticles;
            public Vector3 Attractor;
            public float Radius;
        }
        // Per frame particle constant buffer
        public struct ParticleFrame
        {
            public float Time;
            public float FrameTime;
            public uint RandomSeed;
            // we use CopyStructureCount for fourth component
            uint _padding0; 
        }

        public ParticleConstants Constants;
        public ParticleFrame Frame;

        // Store compute shaders by name
        public Dictionary<String, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();

        // Particle rendering shaders
        VertexShader vertexShader;
        VertexShader vertexShaderInstanced;
        GeometryShader geomShader;
        PixelShader pixelShader;

        // the compute shader constant buffer
        Buffer perComputeBuffer;
        Buffer perFrame;

        // the particle buffer
        Buffer indirectArgsBuffer;
        Buffer particleCountGSIABuffer;
        Buffer particleCountStaging;
        List<Buffer> particleBuffers = new List<Buffer>();
        List<ShaderResourceView> particleSRVs = new List<ShaderResourceView>();
        List<UnorderedAccessView> particleUAVs = new List<UnorderedAccessView>();

        int activeParticleTextureIndex = 0;
        List<ShaderResourceView> particleTextureSRVs = new List<ShaderResourceView>();

        SamplerState linearSampler;
        
        BlendState blendState;
        BlendState blendStateLight;
        public bool UseLightenBlend = false;
        DepthStencilState disableDepthWrite;

        public bool Instanced = true;

        public ParticleRenderer()
        {
            this.Constants.DomainBoundsMin = new Vector3(-15, -15, 15);
            this.Constants.DomainBoundsMax = new Vector3(0, 0, 0);
        }

        public int ThreadsX = 128;
        public int ThreadsY = 1;
        // Compile compute shader from file and add to computeShaders dictionary
        public void CompileComputeShader(string csFunction, string csFile = @"Shaders\ParticleCS.hlsl", string csProfile = "cs_5_0")
        {
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                new SharpDX.Direct3D.ShaderMacro("THREADSX", ThreadsX),
                new SharpDX.Direct3D.ShaderMacro("THREADSY", ThreadsY),
            };
            using (var bytecode = HLSLCompiler.CompileFromFile(csFile, csFunction, csProfile, defines))
            {
                computeShaders[csFunction] = ToDispose(new ComputeShader(this.DeviceManager.Direct3DDevice, bytecode));
            }
        }

        internal void UpdateConstants()
        {
            // Update the ParticleConstants buffer
            this.DeviceManager.Direct3DContext.UpdateSubresource(ref Constants, perComputeBuffer);
        }


        public int ParticlesPerBatch = 16;
        float limiter = 0f;
        float genTime = 0f;

        Random random = new Random();
        // Initialize the particles with maxParticles
        // They will be randomly dispersed within the 
        // Constants.DomainBoundsMin->DomainBoundsMax
        public void InitializeParticles(int maxParticles, float maxLifetime)
        {
            particleSRVs.ForEach(srv => RemoveAndDispose(ref srv));
            particleSRVs.Clear();
            particleUAVs.ForEach(uav => RemoveAndDispose(ref uav));
            particleUAVs.Clear();
            particleBuffers.ForEach(pb => RemoveAndDispose(ref pb));
            particleBuffers.Clear();
            RemoveAndDispose(ref indirectArgsBuffer);
            RemoveAndDispose(ref particleCountGSIABuffer);

            var device = this.DeviceManager.Direct3DDevice;
            var context = device.ImmediateContext;

            this.Constants.MaxParticles = maxParticles;
            this.Constants.MaxLifetime = maxLifetime;

            // Determine how often and how many particles to generate
            this.ParticlesPerBatch = (int)(maxParticles * 0.0128f);
            this.limiter = (float)(Math.Ceiling(this.ParticlesPerBatch / 16.0) * 16.0 * maxLifetime) / (float)maxParticles;

            #region Create Buffers and Views
            // Create 2 buffers, the first represents current simulation state
            // while the second represents the new simulation state 
            // (we will swap them after updating the simulation each frame)
            particleBuffers.Add(
                ToDispose(new Buffer(device,
                    Utilities.SizeOf<Particle>() * maxParticles,
                    ResourceUsage.Default,
                    BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                    CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, Utilities.SizeOf<Particle>())));
            particleBuffers.Add(
                ToDispose(new Buffer(device,
                    Utilities.SizeOf<Particle>() * maxParticles,
                    ResourceUsage.Default,
                    BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                    CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, Utilities.SizeOf<Particle>())));
           
            particleSRVs.Add(ToDispose(new ShaderResourceView(device, particleBuffers[0])));
            particleSRVs.Add(ToDispose(new ShaderResourceView(device, particleBuffers[1])));
            particleSRVs[0].DebugName = "ParticleSRV_0";
            particleSRVs[1].DebugName = "ParticleSRV_1";
            particleUAVs.Add(ToDispose(CreateBufferUAV(device, particleBuffers[0], UnorderedAccessViewBufferFlags.Append)));
            particleUAVs.Add(ToDispose(CreateBufferUAV(device, particleBuffers[1], UnorderedAccessViewBufferFlags.Append)));
            particleUAVs[0].DebugName = "ParticleUAV_0";
            particleUAVs[1].DebugName = "ParticleUAV_1";

            // Create particle count buffers:
            var bufDesc = new BufferDescription
            {
                BindFlags = SharpDX.Direct3D11.BindFlags.ConstantBuffer,
                SizeInBytes = 4 * SharpDX.Utilities.SizeOf<uint>(),
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
            };
            // 1. Buffer used as input to the context.DrawInstancedIndirect
            // The 4 elements represent the 4 parameters of DrawInstanced i.e.
            // DrawInstanced(vertixCountPerInstance, instanceCount, vertexOffset, instanceOffset)
            bufDesc.OptionFlags = ResourceOptionFlags.DrawIndirectArguments;
            bufDesc.BindFlags = BindFlags.None;
            indirectArgsBuffer = ToDispose(new Buffer(device, bufDesc));
            indirectArgsBuffer.DebugName = "ParticleCountIA";
            // 4 vertices per instance
            device.ImmediateContext.UpdateSubresource(new uint[4] { 4, 0, 0, 0 }, indirectArgsBuffer);

            // Geometry Shader version instance count
            particleCountGSIABuffer = ToDispose(new Buffer(device, bufDesc));
            particleCountGSIABuffer.DebugName = "ParticleCountGSIA";
            // 1 instance of ParticleCount vertices
            device.ImmediateContext.UpdateSubresource(new uint[4] { 0, 1, 0, 0 }, particleCountGSIABuffer);

            bufDesc.OptionFlags = ResourceOptionFlags.None;
            bufDesc.Usage = ResourceUsage.Staging;
            bufDesc.CpuAccessFlags = CpuAccessFlags.Read;
            particleCountStaging = ToDispose(new Buffer(device, bufDesc));
            
            #endregion

            //// Can initialize the initial particles on CPU as long as pass through the count for firstRun
            //var particles = new Particle[this.Constants.MaxParticles];
            //for (var i = 0; i < particles.Length; i++)
            //{
            //    particles[i].Radius = 0.05f;
            //    particles[i].Position = random.NextVector3(this.Constants.DomainBoundsMin, this.Constants.DomainBoundsMax);
            //    particles[i].OldPosition = particles[i].Position;
            //    particles[i].Energy = 0.2f;
            //}
            //// Load particles into buffer
            //context.UpdateSubresource(particles, particleBuffers[1]);
            //context.ComputeShader.SetUnorderedAccessView(0, particleUAVs[0], 0);
            //context.ComputeShader.SetUnorderedAccessView(1, particleUAVs[1], maxParticles);

            // Set the starting number of particles to 0
            context.ComputeShader.SetUnorderedAccessView(0, particleUAVs[0], 0);
            context.ComputeShader.SetUnorderedAccessView(1, particleUAVs[1], 0);

            // Update the ParticleConstants buffer
            context.UpdateSubresource(ref Constants, perComputeBuffer);
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref vertexShaderInstanced);
            RemoveAndDispose(ref geomShader);
            RemoveAndDispose(ref pixelShader);

            RemoveAndDispose(ref blendState);
            RemoveAndDispose(ref linearSampler);

            RemoveAndDispose(ref perComputeBuffer);
            RemoveAndDispose(ref perFrame);

            // Dispose of any loaded particle textures
            particleTextureSRVs.ForEach(srv => RemoveAndDispose(ref srv));
            particleTextureSRVs.Clear();

            // Dispose of any compute shaders
            computeShaders.Select(kv => kv.Value).ToList().ForEach(cs => RemoveAndDispose(ref cs));
            computeShaders.Clear();

            var device = this.DeviceManager.Direct3DDevice;

            #region Compile Vertex/Pixel/Geometry shaders

            // Compile and create the vertex shader
            using (var vsBytecode = HLSLCompiler.CompileFromFile(@"Shaders\ParticleVS.hlsl", "VSMain", "vs_5_0"))
            using (var vsInstance = HLSLCompiler.CompileFromFile(@"Shaders\ParticleVS.hlsl", "VSMainInstance", "vs_5_0"))
            // Compile and create the pixel shader
            using (var psBytecode = HLSLCompiler.CompileFromFile(@"Shaders\ParticlePS.hlsl", "PSMain", "ps_5_0"))
            // Compile and create the geometry shader
            using (var gsBytecode = HLSLCompiler.CompileFromFile(@"Shaders\ParticleGS.hlsl", "PointToQuadGS", "gs_5_0"))
            {
                vertexShader = ToDispose(new VertexShader(device, vsBytecode));
                vertexShaderInstanced = ToDispose(new VertexShader(device, vsInstance));
                pixelShader = ToDispose(new PixelShader(device, psBytecode));
                geomShader = ToDispose(new GeometryShader(device, gsBytecode));
            }
            #endregion

            #region Blend States
            var blendDesc = new BlendStateDescription() {
                IndependentBlendEnable = false,
                AlphaToCoverageEnable = false,
            };
            // Additive blend state that darkens
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                BlendOperation = BlendOperation.Add,
                AlphaBlendOperation = BlendOperation.Add,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.Zero,
                RenderTargetWriteMask = ColorWriteMaskFlags.All
            };
            blendState = ToDispose(new BlendState(device, blendDesc));

            // Additive blend state that lightens
            // (needs a dark background)
            blendDesc.RenderTarget[0].DestinationBlend = BlendOption.One;
            
            blendStateLight = ToDispose(new BlendState(device, blendDesc));
            #endregion

            // depth stencil state to disable Z-buffer
            disableDepthWrite = ToDispose(new DepthStencilState(device, new DepthStencilStateDescription {
                DepthComparison = Comparison.Less,
                DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.Zero,
                IsDepthEnabled = true,
                IsStencilEnabled = false
            }));

            // Create a linear sampler
            linearSampler = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear, // Bilinear
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
            }));

            // Create the per compute shader constant buffer
            perComputeBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ParticleConstants>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            // Create the particle frame buffer
            perFrame = ToDispose(new Buffer(device, Utilities.SizeOf<ParticleFrame>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            particleTextureSRVs.Add(ToDispose(ShaderResourceView.FromFile(device, "Particle.png")));
            particleTextureSRVs.Add(ToDispose(ShaderResourceView.FromFile(device, "Snowflake.png")));
            particleTextureSRVs.Add(ToDispose(ShaderResourceView.FromFile(device, "Square.png")));
            activeParticleTextureIndex = 0;

            // Reinitialize particles if > 0
            if (this.Constants.MaxParticles > 0)
            {
                InitializeParticles(this.Constants.MaxParticles, this.Constants.MaxLifetime);
            }
        }

        public static UnorderedAccessView CreateBufferUAV(SharpDX.Direct3D11.Device device, Buffer buffer, UnorderedAccessViewBufferFlags flags = UnorderedAccessViewBufferFlags.None)
        {
            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource { FirstElement = 0 }
            };
            if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferAllowRawViews) == ResourceOptionFlags.BufferAllowRawViews)
            {
                // A raw buffer requires R32_Typeless
                uavDesc.Format = Format.R32_Typeless;
                uavDesc.Buffer.Flags = UnorderedAccessViewBufferFlags.Raw | flags;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / 4;
            }
            else if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured)
            {
                uavDesc.Format = Format.Unknown;
                uavDesc.Buffer.Flags = flags;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride;
            }
            else
            {
                throw new ArgumentException("Buffer must be raw or structured", "buffer");
            }

            return new UnorderedAccessView(device, buffer, uavDesc);
        }

        public int CurrentParticleCount = 0;
        float elapsedSinceGenerator;
        private void DebugCount(string src, DeviceContext context, UnorderedAccessView uav)
        {
            if (elapsedSinceGenerator > 2)
            {
                elapsedSinceGenerator = 0;
                context.CopyStructureCount(particleCountStaging, 0, uav);
                DataStream ds;
                var db = context.MapSubresource(particleCountStaging, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out ds);
                CurrentParticleCount = ds.ReadInt();
                System.Diagnostics.Debug.WriteLine("{0}: {1},{2},{3},{4}", src, CurrentParticleCount, (uint)ds.ReadInt(), (uint)ds.ReadInt(), (uint)ds.ReadInt());
                context.UnmapSubresource(particleCountStaging, 0);
            }
        }

        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        public long LastDispatchTicks = 0;

        public void Update(string generatorCS, string updaterCS)
        {
            var context = DeviceManager.Direct3DContext;
            var append = particleUAVs[0];
            var consume = particleUAVs[1];
            // Assign particle append/consume UAVs
            context.ComputeShader.SetUnorderedAccessView(0, append);
            context.ComputeShader.SetUnorderedAccessView(1, consume);

            // Update the constant buffers
            // Generate the next random seed for particle generator
            Frame.RandomSeed = (uint)random.Next(int.MinValue, int.MaxValue);
            context.UpdateSubresource(ref Frame, perFrame);
            // Copy current consume buffer count into perFrame
            context.CopyStructureCount(perFrame, 4 * 3, consume);
            context.ComputeShader.SetConstantBuffer(0, perComputeBuffer);
            context.ComputeShader.SetConstantBuffer(1, perFrame);

            long ticks = 0;
            UpdateCS(updaterCS, particleUAVs[0], particleUAVs[1]);
            ticks = LastDispatchTicks;

            genTime += Frame.FrameTime;
            elapsedSinceGenerator += this.Frame.FrameTime;
            if (genTime > limiter)
            {
                genTime = 0;
                GenerateCS(generatorCS, particleUAVs[0]);
                ticks += LastDispatchTicks;
            }
            LastDispatchTicks = ticks;

            // Update the particle count for the render phase
            if (Instanced)
                context.CopyStructureCount(indirectArgsBuffer, 4, append);
            else
                context.CopyStructureCount(particleCountGSIABuffer, 0, append);

            // Clear the shader and resources from pipeline stage
            context.ComputeShader.SetUnorderedAccessViews(0, null, null, null);
            context.ComputeShader.SetUnorderedAccessViews(1, null, null, null);
            context.ComputeShader.Set(null);

            // Flip UAVs/SRVs
            var u0 = particleUAVs[0];
            particleUAVs[0] = particleUAVs[1];
            particleUAVs[1] = u0;
            var s0 = particleSRVs[0];
            particleSRVs[0] = particleSRVs[1];
            particleSRVs[1] = s0;
        }

        private void UpdateCS(string name, UnorderedAccessView append, UnorderedAccessView consume)
        {
            int width, height;
            height = 1;
            width = this.Constants.MaxParticles;
            var context = this.DeviceManager.Direct3DContext;
            // Compile the shader if it isn't already
            if (!computeShaders.ContainsKey(name))
            {
                CompileComputeShader(name);
            }

            // Set the shader to run
            context.ComputeShader.Set(computeShaders[name]);

            //DebugCount("Update-consume-1", context, consume);
            clock.Restart();
            // Dispatch the compute shader thread groups
            context.Dispatch((int)Math.Ceiling(width / (double)ThreadsX), (int)Math.Ceiling(height / (double)ThreadsY), 1);
            LastDispatchTicks = clock.ElapsedTicks;
            //DebugCount("Update-consume-2", context, consume);
        }

        public const int GeneratorThreadsX = 16;
        private void GenerateCS(string name, UnorderedAccessView append)
        {
            var context = this.DeviceManager.Direct3DContext;

            // Compile the shader if it isn't already
            if (!computeShaders.ContainsKey(name))
            {
                int oldThreadsX = ThreadsX;
                int oldThreadsY = ThreadsY;
                ThreadsX = GeneratorThreadsX;
                ThreadsY = 1;
                CompileComputeShader(name);
                ThreadsX = oldThreadsX;
                ThreadsY = oldThreadsY;
            }

            // Set the shader to run
            context.ComputeShader.Set(computeShaders[name]);

            clock.Restart();
            // Dispatch the compute shader thread groups
            context.Dispatch((int)Math.Ceiling(ParticlesPerBatch / 16.0), 1, 1);
            LastDispatchTicks = clock.ElapsedTicks;

            DebugCount("Gen-append", context, append);
        }


        public void SwitchTexture()
        {
            activeParticleTextureIndex = (activeParticleTextureIndex + 1) % particleTextureSRVs.Count;
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            // Retrieve existing pipeline states for backup
            Color4 oldBlendFactor;
            int oldSampleMask;
            int oldStencil;
            var oldPSBufs = context.PixelShader.GetConstantBuffers(0, 1);
            using (var oldVS = context.VertexShader.Get())
            using (var oldPS = context.PixelShader.Get())
            using (var oldGS = context.GeometryShader.Get())
            using (var oldSamp = context.PixelShader.GetSamplers(0, 1).FirstOrDefault())
            using (var oldBlendState = context.OutputMerger.GetBlendState(out oldBlendFactor, out oldSampleMask))
            using (var oldIA = context.InputAssembler.InputLayout)
            using (var oldDepth = context.OutputMerger.GetDepthStencilState(out oldStencil))
            {
                // There is no input layout for this renderer
                context.InputAssembler.InputLayout = null;

                // Disable depth test
                context.OutputMerger.SetDepthStencilState(disableDepthWrite);
                // Set the additive blend state
                if (!UseLightenBlend)
                    context.OutputMerger.SetBlendState(blendState, null, 0xFFFFFFFF);
                else
                    context.OutputMerger.SetBlendState(blendStateLight, Color.White, 0xFFFFFFFF);
                
                // Set vertex shader resources
                context.VertexShader.SetShaderResource(0, particleSRVs[1]);
                
                // Set pixel shader resources
                //context.PixelShader.SetConstantBuffer(0, perComputeBuffer);
                context.PixelShader.SetShaderResource(0, particleTextureSRVs[activeParticleTextureIndex]);
                context.PixelShader.SetSampler(0, linearSampler);
                context.PixelShader.Set(pixelShader);

                if (Instanced)
                {
                    // The input topology to the input assembler is a trianglestrip
                    context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
                    context.VertexShader.Set(vertexShaderInstanced);

                    // Draw the number of instances stored in the particleCountBuffer
                    // The vertex shader will rely upon the SV_VertexID and SV_InstanceID input semantic
                    context.DrawInstancedIndirect(indirectArgsBuffer, 0);
                }
                else
                {
                    // The input topology to the input assembler and geometry shader is a point list
                    context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PointList;
                    context.VertexShader.Set(vertexShader);

                    // Set geometry shader resources
                    context.GeometryShader.Set(geomShader);
                    // Draw the number of particles stored in the particleCountGSIABuffer
                    // The vertex shader will rely upon the SV_VertexID input semantic
                    context.DrawInstancedIndirect(particleCountGSIABuffer, 0);
                }

                context.VertexShader.SetShaderResource(0, null);

                // Restore previous pipeline state
                context.VertexShader.Set(oldVS);
                context.PixelShader.SetConstantBuffers(0, oldPSBufs);
                context.PixelShader.Set(oldPS);
                context.GeometryShader.Set(oldGS);
                context.PixelShader.SetSampler(0, oldSamp);
                context.InputAssembler.InputLayout = oldIA;
                
                // Restore previous blend and depth state
                context.OutputMerger.SetBlendState(oldBlendState, oldBlendFactor, oldSampleMask);
                context.OutputMerger.SetDepthStencilState(oldDepth, oldStencil);
            }
        }
    }
}
