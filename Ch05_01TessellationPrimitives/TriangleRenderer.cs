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
    public class TriangleRenderer : Common.RendererBase
    {
        // The triangle vertex buffer
        Buffer triangleVertices;

        // The vertex buffer binding structure for the triangle
        VertexBufferBinding triangleBinding;

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
            RemoveAndDispose(ref triangleVertices);
            RemoveAndDispose(ref textureView);
            RemoveAndDispose(ref samplerState);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Create a triangle
            triangleVertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new[] {
            /*  Vertex Position, normal, Color, UV */
                new Vertex(new Vector3(0f, 0f, -0.001f), Vector3.UnitZ, Color.Black, new Vector2(1.0f, 1.0f)), // Base-right
                new Vertex(new Vector3(-0.75f, 1.5f, -0.001f), Vector3.UnitZ, Color.Black, new Vector2(0.5f, 0.0f)), // Apex
                new Vertex(new Vector3(-1.5f, 0f, -0.001f),Vector3.UnitZ, Color.Black, new Vector2(0.0f, 1.0f)), // Base-left
            }));
            triangleBinding = new VertexBufferBinding(triangleVertices, Utilities.SizeOf<Vertex>(), 0);

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

            // Render the triangle

            // Set the shader resource
            context.PixelShader.SetShaderResource(0, textureView);
            // Set the sampler state
            context.PixelShader.SetSampler(0, samplerState);

            // Tell the IA we are now using a patch list with 3 control points
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PatchListWith3ControlPoints;
            // Pass in the control points
            context.InputAssembler.SetVertexBuffers(0, triangleBinding);
            // Draw the 3 vertices of our triangle
            context.Draw(3, 0);
        }
    }
}