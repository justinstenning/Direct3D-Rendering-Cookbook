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

namespace Ch10_01DeferredRendering
{
    public class ScreenAlignedQuadRenderer : Common.RendererBase
    {
        // The vertex shader
        VertexShader vertexShader;

        // The vertex layout for the IA
        InputLayout vertexLayout;

        // The pixel shader
        PixelShader pixelShader;
        PixelShader pixelShaderMS;

        SamplerState samplerState;

        // The vertex buffer
        Buffer vertexBuffer;
        //// The index buffer
        //Buffer indexBuffer;
        // The vertex buffer binding
        VertexBufferBinding vertexBinding;

        public PixelShader Shader { get; set; }
        public ShaderResourceView[] ShaderResources { get; set; }

        /// <summary>
        /// Default constructor
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
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref vertexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = DeviceManager.Direct3DDevice;

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#endif
            // Use our HLSL file include handler to resolve #include directives in the HLSL source
            var includeHandler = new HLSLFileIncludeHandler(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Shaders"));

            // Compile and create the vertex shader
            using (ShaderBytecode vertexShaderBytecode = ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "VSMain", "vs_5_0", shaderFlags, EffectFlags.None, null, includeHandler))
            {
                vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));
                // Layout from VertexShader input signature
                vertexLayout = ToDispose(new InputLayout(device,
                    vertexShaderBytecode.GetPart(ShaderBytecodePart.InputSignatureBlob),
                    //ShaderSignature.GetInputSignature(vertexShaderBytecode),
                new[]
                {
                    // "SV_Position" = vertex coordinate
                    new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0),
                }));

                // Create vertex buffer
                vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new Vector3[] {
                    /*  Position: float x 3 */
                    new Vector3(-1.0f, -1.0f, -1.0f),
                    new Vector3(-1.0f, 1.0f, -1.0f),
                    new Vector3(1.0f, -1.0f, -1.0f),
                    new Vector3(1.0f, 1.0f, -1.0f),
                }));
                vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0);
                // Triangle strip:
                // v1   v3
                // |\   |
                // | \ B|
                // | A\ |
                // |   \|
                // v0   v2
            }

            // Compile and create the pixel shader
            using (var bytecode = ToDispose(ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMain", "ps_5_0", shaderFlags, EffectFlags.None, null, includeHandler)))
                pixelShader = ToDispose(new PixelShader(device, bytecode));

            using (var bytecode = ToDispose(ShaderBytecode.CompileFromFile(@"Shaders\SAQuad.hlsl", "PSMainMultisample", "ps_5_0", shaderFlags, EffectFlags.None, null, includeHandler)))
                pixelShaderMS = ToDispose(new PixelShader(device, bytecode));

            samplerState = ToDispose(new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            }));

            //pointSamplerState = ToDispose(new SamplerState(device, new SamplerStateDescription
            //{
            //    Filter = Filter.MinMagMipPoint,
            //    AddressU = TextureAddressMode.Wrap,
            //    AddressV = TextureAddressMode.Wrap,
            //    AddressW = TextureAddressMode.Wrap,
            //    ComparisonFunction = Comparison.Never,
            //    MinimumLod = 0,
            //    MaximumLod = float.MaxValue
            //}));

        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            // Retrieve the existing shader and IA settings
            using(var oldVertexLayout = context.InputAssembler.InputLayout)
            //using(var oldSampler = context.PixelShader.GetSamplers(0, 1).FirstOrDefault())
            using(var oldPixelShader = context.PixelShader.Get())
            using(var oldVertexShader = context.VertexShader.Get())
            {

                // Set pixel shader
                //context.PixelShader.SetSampler(0, linearSamplerState); 
                bool isMultisampledSRV = false;
                if (ShaderResources != null && ShaderResources.Length > 0 && !ShaderResources[0].IsDisposed)
                {
                    context.PixelShader.SetShaderResources(0, ShaderResources);

                    if (ShaderResources[0].Description.Dimension == SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled)
                    {
                        isMultisampledSRV = true;
                    }
                }

                // Set a default pixel shader
                if (Shader == null)
                {
                    if (isMultisampledSRV)
                        context.PixelShader.Set(pixelShaderMS);
                    else
                        context.PixelShader.Set(pixelShader);
                }
                else
                {
                    context.PixelShader.Set(Shader);
                }

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

                // Reset pixel shader resources
                if (ShaderResources != null && ShaderResources.Length > 0)
                {
                    context.PixelShader.SetShaderResources(0, new ShaderResourceView[ShaderResources.Length]);
                }

                // Restore previous shader and IA settings
                //context.PixelShader.SetSampler(0, oldSampler);
                context.PixelShader.Set(oldPixelShader);
                context.VertexShader.Set(oldVertexShader);
                context.InputAssembler.InputLayout = oldVertexLayout;
            }
        }
    }
}
