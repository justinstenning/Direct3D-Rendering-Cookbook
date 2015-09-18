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
    public class ScreenAlignedQuadRenderer : Common.RendererBase
    {
        // The vertex shader
        ShaderBytecode vertexShaderBytecode;
        VertexShader vertexShader;

        // The vertex layout for the IA
        InputLayout vertexLayout;

        // The pixel shader
        ShaderBytecode pixelShaderBytecode;
        PixelShader pixelShader;
        PixelShader pixelShaderMS;

        SamplerState pointSamplerState;
        SamplerState linearSampleState;

        // The vertex buffer
        Buffer vertexBuffer;
        //// The index buffer
        //Buffer indexBuffer;
        // The vertex buffer binding
        VertexBufferBinding vertexBinding;

        public ShaderResourceView ShaderResource { get; set; }

        /// <summary>
        /// Default constructor (uses color of LightGray)
        /// </summary>
        public ScreenAlignedQuadRenderer()
        {
        }

        /// <summary>
        /// Create any device dependent resources here.
        /// This method will be called when the device is first
        /// initialized or recreated after being removed or reset.
        /// </summary>
        protected override void CreateDeviceDependentResources()
        {
            // Ensure that if already set the device resources
            // are correctly disposed of before recreating
            RemoveAndDispose(ref vertexBuffer);
            //RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            // Get a reference to the Device1 instance and immediate context
            var device = DeviceManager.Direct3DDevice;
            var context = DeviceManager.Direct3DContext;

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            // Use our HLSL file include handler to resolve #include directives in the HLSL source
            var includeHandler = new HLSLFileIncludeHandler(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Shaders"));

            // Compile and create the vertex shader
            vertexShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "VSMain", "vs_5_0", shaderFlags, EffectFlags.None, null, includeHandler));
            vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));

            // Compile and create the pixel shader
            pixelShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMain", "ps_5_0", shaderFlags, EffectFlags.None, null, includeHandler));
            pixelShader = ToDispose(new PixelShader(device, pixelShaderBytecode));

            using (var bytecode = ToDispose(ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMainMultisample", "ps_5_0", shaderFlags, EffectFlags.None, null, includeHandler)))
            {
                pixelShaderMS = ToDispose(new PixelShader(device, pixelShaderBytecode));
            }

            // Layout from VertexShader input signature
            vertexLayout = ToDispose(new InputLayout(device,
                vertexShaderBytecode.GetPart(ShaderBytecodePart.InputSignatureBlob),
                //ShaderSignature.GetInputSignature(vertexShaderBytecode),
            new[]
            {
                // "SV_Position" = vertex coordinate in object space
                new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0),
            }));

            linearSampleState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            }));

            pointSamplerState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            }));

            // Create vertex buffer
            vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new SimpleVertex[] {
                /*  Position: float x 3 */
                new SimpleVertex(-1.0f, -1.0f, 0.5f),
                new SimpleVertex(-1.0f, 1.0f, 0.5f),
                new SimpleVertex(1.0f, -1.0f, 0.5f),
                new SimpleVertex(1.0f, 1.0f, 0.5f),
            }));
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<SimpleVertex>(), 0);
            
            // Triangle strip:
            // v1   v3
            // |\   |
            // | \ B|
            // | A\ |
            // |   \|
            // v0   v2
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            // Retrieve the existing shader and IA settings
            using(var oldVertexLayout = context.InputAssembler.InputLayout)
            using(var oldSampler = context.PixelShader.GetSamplers(0, 1).FirstOrDefault())
            using(var oldPixelShader = context.PixelShader.Get())
            using(var oldVertexShader = context.VertexShader.Get())
            {

                // Set pixel shader
                context.PixelShader.SetSampler(0, pointSamplerState);
                bool isMultisampledSRV = false;
                if (ShaderResource != null && !ShaderResource.IsDisposed)
                {
                    context.PixelShader.SetShaderResource(0, ShaderResource);

                    if (ShaderResource.Description.Dimension == SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled)
                    {
                        isMultisampledSRV = true;
                    }
                }

                if (isMultisampledSRV)
                    context.PixelShader.Set(pixelShaderMS);
                else
                    context.PixelShader.Set(pixelShader);


                // Set vertex shader
                context.VertexShader.Set(vertexShader);

                // Update vertex layout to use
                context.InputAssembler.InputLayout = vertexLayout;

                // Tell the IA we are using a triangle strip
                context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
                // Pass in the vertices (note: only 4 vertices)
                context.InputAssembler.SetVertexBuffers(0, vertexBinding);

                // Draw the 4 vertices that make up the triangle strip
                context.Draw(4, 0);

                // Restore previous shader and IA settings
                context.PixelShader.SetSampler(0, oldSampler);
                context.PixelShader.Set(oldPixelShader);
                context.VertexShader.Set(oldVertexShader);
                context.InputAssembler.InputLayout = oldVertexLayout;
            }
        }
    }
}
