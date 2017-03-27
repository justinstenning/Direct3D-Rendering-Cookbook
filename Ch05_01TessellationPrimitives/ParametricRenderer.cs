using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch05_01TessellationPrimitives
{
    public class ParametricRenderer : Common.RendererBase
    {
        // The vertex buffer
        Buffer vertices;

        // The vertex buffer binding structure
        VertexBufferBinding vertexBinding;

        // Shader texture resource
        ShaderResourceView textureView;
        // Control sampling behavior with this state
        SamplerState samplerState;

        /// <summary>
        /// Create any device dependent resources here.
        /// This method will be called when the device is first
        /// initialized or recreated after being removed or reset.
        /// </summary>
        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            // Ensure that if already set the device resources
            // are correctly disposed of before recreating
            RemoveAndDispose(ref vertices);
            RemoveAndDispose(ref textureView);
            RemoveAndDispose(ref samplerState);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Create a vertex to begin the parametric surface
            vertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new[] {
            /*  Vertex Position */
                new Vertex(new Vector3(0f, 0f, 0f)), // Base-right
            }));
            vertexBinding = new VertexBufferBinding(vertices, Utilities.SizeOf<Vertex>(), 0);

            // Load texture
            textureView = ToDispose(Common.TextureLoader.ShaderResourceViewFromFile(device, "Texture2.png"));

            // Create our sampler state
            samplerState = ToDispose(new SamplerState(device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f
            }));
        }

        protected override void DoRender()
        {
            // Get the context reference
            var context = this.DeviceManager.Direct3DContext;

            // Render the parametric surface

            // Set the shader resource
            context.PixelShader.SetShaderResource(0, textureView);
            // Set the sampler state
            context.PixelShader.SetSampler(0, samplerState);

            // Tell the IA we are now using a patch list with 1 control points
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PatchListWith1ControlPoints;
            // Pass in the control points
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 1 vertices of our parametric surface
            context.Draw(1, 0);
        }
    }
}