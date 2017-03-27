using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch07_01ImageProcessing
{
    public class ImageProcessingCS : RendererBase
    {
        // Input variables for compute shaders
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct ComputeConstants
        {
            public float LerpT;
            public Vector3 Params0;
            public Vector4 Params1;
            public Vector4 Params2;
        }

        public struct ComputeConfig
        {
            public int ThreadsX;
            public int ThreadsY;
            public ComputeConstants Constants;
        }

        public ComputeConstants Constants;

        // Store compute shaders by name
        public Dictionary<String, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();

        // the compute shader constant buffer
        Buffer perComputeBuffer;

        string sourceImageFile;
        Texture2D sourceTextureTypeless;
        ShaderResourceView sourceTextureSRV;
        UnorderedAccessView sourceTextureUAV;

        Buffer histogramResult;
        Buffer histogramCPU;
        UnorderedAccessView histogramUAV;

        SamplerState linearSampler;

        public ImageProcessingCS()
        {
            // Default to full application of filter
            Constants.LerpT = 1.0f;
        }

        // Load an image
        public void LoadSourceImage(string sourceImage)
        {
            sourceImageFile = sourceImage;
            if (this.DeviceManager != null)
            {
                var device = this.DeviceManager.Direct3DDevice;
                sourceTextureSRV = TextureLoader.ShaderResourceViewFromFile(device, sourceImageFile);
                //using (var tmpTex = sourceTextureSRV.ResourceAs<Texture2D>())
                //{
                //    if (tmpTex.Description.Format != Format.R8G8B8A8_UNorm)
                //        throw new Exception("Expected an RGBA texture with 8 bits per channel");
                //}
            }
        }

        public int ThreadsX = 256;
        public int ThreadsY = 4;
        
        // Compile compute shader from file and add to computeShaders dictionary
        public void CompileComputeShader(string csFunction, string csFile = @"Shaders\ImageProcessingCS.hlsl", string csVersion = "cs_5_0")
        {
            var shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            SharpDX.Direct3D.ShaderMacro[] defines = new[] {
                new SharpDX.Direct3D.ShaderMacro("THREADSX", ThreadsX),
                new SharpDX.Direct3D.ShaderMacro("THREADSY", ThreadsY),
            };

            // Use our HLSL file include handler to resolve #include directives in the HLSL source
            var includeHandler = new HLSLFileIncludeHandler(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Shaders"));
            
            using (var bytecode = ShaderBytecode.CompileFromFile(csFile, csFunction, csVersion, shaderFlags, EffectFlags.None, defines, includeHandler))
            {
                computeShaders[csFunction] = ToDispose(new ComputeShader(this.DeviceManager.Direct3DDevice, bytecode));
            }
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref sourceTextureSRV);
            RemoveAndDispose(ref sourceTextureTypeless);
            RemoveAndDispose(ref sourceTextureUAV);

            RemoveAndDispose(ref linearSampler);

            RemoveAndDispose(ref histogramResult);
            RemoveAndDispose(ref histogramCPU);

            RemoveAndDispose(ref perComputeBuffer);

            // Dispose of any compute shaders
            computeShaders.Select(kv => kv.Value).ToList().ForEach(cs => RemoveAndDispose(ref cs));
            computeShaders.Clear();

            var device = this.DeviceManager.Direct3DDevice;

            linearSampler = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                //ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                //MaximumLod = float.MaxValue,
                //MinimumLod = 0,
            }));

            #region Pre-compile compute shaders

            //CompileComputeShader("DesaturateCS");
            //CompileComputeShader("InplaceDesaturateCS");
            //CompileComputeShader("HistogramCS");
            //CompileComputeShader("NegativeCS");

            #endregion

            // Create the per compute shader constant buffer
            perComputeBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ComputeConstants>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // If a source image file has been provided, load it
            if (!String.IsNullOrEmpty(sourceImageFile))
            {
                LoadSourceImage(sourceImageFile);
            }

            #region Histogram objects
            
            histogramResult = ToDispose(new Buffer(device, new BufferDescription
            {
                BindFlags = BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                Usage = ResourceUsage.Default,
                SizeInBytes = 256 * 4,
                StructureByteStride = 4
            }));
            histogramResult.DebugName = "Histogram Result";
            
            histogramUAV = ToDispose(CreateBufferUAV(device, histogramResult));

            // Create resource that can be read from the CPU for retrieving
            // the histogram results
            var cpuReadDesc = histogramResult.Description;
            cpuReadDesc.OptionFlags = ResourceOptionFlags.None;
            cpuReadDesc.BindFlags = BindFlags.None;
            cpuReadDesc.CpuAccessFlags = CpuAccessFlags.Read;
            cpuReadDesc.Usage = ResourceUsage.Staging;
            histogramCPU = ToDispose(new Buffer(device, cpuReadDesc));
            histogramCPU.DebugName = "Histogram Result (CPU)";

            #endregion
        }

        public static UnorderedAccessView CreateBufferUAV(SharpDX.Direct3D11.Device device, Buffer buffer)
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
                uavDesc.Buffer.Flags = UnorderedAccessViewBufferFlags.Raw;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / 4;
            }
            else if ((buffer.Description.OptionFlags & ResourceOptionFlags.BufferStructured) == ResourceOptionFlags.BufferStructured)
            {
                uavDesc.Format = Format.Unknown;
                uavDesc.Buffer.ElementCount = buffer.Description.SizeInBytes / buffer.Description.StructureByteStride;
            }
            else
            {
                throw new ArgumentException("Buffer must be raw or structured", "buffer");
            }

            return new UnorderedAccessView(device, buffer, uavDesc);
        }

        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
        public long LastDispatchTicks = 0;

        private void CheckSRVWidthHeight(ShaderResourceView srv, out int width, out int height)
        {
            using (var t = srv.ResourceAs<Texture2D>())
            {
                width = t.Description.Width;
                height = t.Description.Height;
            }
        }

        public void RunChainedCS(TexturePingPong resources, string[] names, ComputeConfig[] configuration = null)
        {
            RunChainedCS(sourceTextureSRV, resources, names, configuration);
        }

        public void RunChainedCS(ShaderResourceView source, TexturePingPong resources, string[] names, ComputeConfig[] configuration = null)
        {
            long dispatchTicks = 0;
            ShaderResourceView input = source;

            if (configuration != null && names.Length != configuration.Length)
                throw new ArgumentOutOfRangeException("configuration", "If not null, configuration must have the same number of elements as names");

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (configuration != null)
                {
                    this.Constants = configuration[i].Constants;
                    this.ThreadsX = configuration[i].ThreadsX;
                    this.ThreadsY = configuration[i].ThreadsY;
                }
                RunCS(name, input, resources.GetNextAsUAV());
                dispatchTicks += LastDispatchTicks;
                input = resources.GetCurrentAsSRV();
            }
            LastDispatchTicks = dispatchTicks;
        }

        public void RunCS(string name, UnorderedAccessView destination)
        {
            RunCS(name, this.sourceTextureSRV, destination);
        }

        public void RunCS(string name, ShaderResourceView source, UnorderedAccessView destination)
        {
            int width, height;
            CheckSRVWidthHeight(source, out width, out height);

            var context = this.DeviceManager.Direct3DContext;

            // Update the constant buffer
            context.UpdateSubresource(ref Constants, perComputeBuffer);

            //context.ComputeShader.SetSampler(0, linearSampler);
            context.ComputeShader.SetShaderResource(0, source);
            context.ComputeShader.SetUnorderedAccessView(0, destination);
            context.ComputeShader.SetConstantBuffer(0, perComputeBuffer);

            // Compile the shader if it isn't already
            if (!computeShaders.ContainsKey(name))
            {
                CompileComputeShader(name);
            }

            // Set the shader to run
            context.ComputeShader.Set(computeShaders[name]);

            clock.Restart();
            context.Dispatch((int)Math.Ceiling(width / (double)ThreadsX), (int)Math.Ceiling(height / (double)ThreadsY), 1);
            LastDispatchTicks = clock.ElapsedTicks;

            // Clear the shader and resources
            context.ComputeShader.SetSampler(0, null);
            context.ComputeShader.SetShaderResources(0, null, null, null);
            context.ComputeShader.SetUnorderedAccessViews(0, null, null, null);
            context.ComputeShader.Set(null);
        }

        //public void Desaturate(UnorderedAccessView destination)
        //{
        //    this.RunCS("DesaturateCS", this.sourceTextureSRV, destination);
        //}

        public int[] Histogram()
        {
            return Histogram(sourceTextureSRV);
        }

        public int[] Histogram(ShaderResourceView source)
        {
            var context = this.DeviceManager.Direct3DContext;

            // Firstly clear the target UAV otherwise the value will accumulate
            // between calls
            context.ClearUnorderedAccessView(histogramUAV, Int4.Zero);

            RunCS("HistogramCS", source, histogramUAV);
            
            // Copy the result into a CPU accessible resource
            context.CopyResource(histogramResult, histogramCPU);

            // Retrieve the luminance histogram from GPU
            try
            {
                var databox = context.MapSubresource(histogramCPU, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                int[] intArray = new int[databox.RowPitch / sizeof(int)];

                System.Runtime.InteropServices.Marshal.Copy(databox.DataPointer, intArray, 0, intArray.Length);

#if DEBUG
                // Debug output of histogram
                var intArrayString = "";
                foreach (var item in intArray)
                {
                    intArrayString += item.ToString() + ",";
                }
                System.Diagnostics.Debug.WriteLine(intArrayString);
#endif

                return intArray;
            }
            finally
            {
                context.UnmapSubresource(histogramCPU, 0);
            }
        }

        private UnorderedAccessView CopySRVToNewR32_UInt_UAV(ShaderResourceView srv)
        {
            UnorderedAccessView uav;
            var device = this.DeviceManager.Direct3DDevice;

            using (var t = srv.ResourceAs<Texture2D>())
            {
                // Resize the sourceTexture resource so that it is the correct size
                var desc = t.Description;
                desc.BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess;
                desc.Format = Format.R8G8B8A8_Typeless;
                using (var t2 = ToDispose(new Texture2D(device, desc)))
                {

                    uav = ToDispose(new UnorderedAccessView(device, t2, new UnorderedAccessViewDescription
                    {
                        Format = Format.R32_UInt,
                        Dimension = UnorderedAccessViewDimension.Texture2D
                    }));

                    // Copy the texture for the resource to the typeless texture
                    device.ImmediateContext.CopyResource(t, t2);
                }
            }

            return uav;
        }

        private void CopyR32_UInt_UAVToExistingSRV(UnorderedAccessView uav, ShaderResourceView srv)
        {
            var device = this.DeviceManager.Direct3DDevice;

            int width, height;

            using (var t = uav.ResourceAs<Texture2D>())
            {
                width = t.Description.Width;
                height = t.Description.Height;

                if (t.Description.Format != Format.R32_UInt)
                    throw new ArgumentException("The provided UAV does not use the format R32_Uint", "uav");

                using (var t2 = srv.ResourceAs<Texture2D>())
                {
                    if (t2.Description.Format != Format.R8G8B8A8_Typeless)
                        throw new ArgumentException("Currently only supporting R8G8B8A8_Typeless SRVs", "srv");

                    this.DeviceManager.Direct3DDevice.ImmediateContext.CopyResource(t, t2);
                }
            }
        }

        private void CopyUAVToSRV(ShaderResourceView srv, UnorderedAccessView uav)
        {
            var device = this.DeviceManager.Direct3DDevice;

            using (var t = srv.ResourceAs<Texture2D>())
            {
                using (var t2 = uav.ResourceAs<Texture2D>())
                {
                    // Copy the texture for the resource to the typeless texture
                    device.ImmediateContext.CopyResource(t, t2);
                }
            }
        }

        protected override void DoRender()
        {
            throw new NotImplementedException("Use one of the image manipulation methods instead (e.g. Desaturate())");
        }
    }
}
