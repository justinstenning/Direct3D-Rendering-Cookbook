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


namespace Ch02_02AddingTexture
{
    public class AxisLinesRenderer : Common.RendererBase
    {
        // The vertex buffer for axis lines
        Buffer axisLinesVertices;

        // The binding structure of the axis lines vertex buffer
        VertexBufferBinding axisLinesBinding;

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
            // Ensure that if already set the device resources
            // are correctly disposed of before recreating
            RemoveAndDispose(ref axisLinesVertices);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Load texture
            textureView = ToDispose(Common.TextureLoader.ShaderResourceViewFromFile(device, "Texture.png"));

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

            // Create xyz-axis arrows
            // X is Red, Y is Green, Z is Blue
            // The arrows point along the + for each axis
            axisLinesVertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new[]
            {
            /*  Vertex Position         Texture UV */
                                        // ~45x10
                -1f, 0f, 0f, 1f,        0.1757f, 0.039f, // - x-axis 
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,  // + x-axis
                0.9f, -0.05f, 0f, 1f,   0.1757f, 0.039f,// arrow head start
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,
                0.9f, 0.05f, 0f, 1f,    0.1757f, 0.039f,
                1f, 0f, 0f, 1f,         0.1757f, 0.039f,  // arrow head end
                                        // ~135x35
                0f, -1f, 0f, 1f,        0.5273f, 0.136f, // - y-axis
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,  // + y-axis
                -0.05f, 0.9f, 0f, 1f,   0.5273f, 0.136f,// arrow head start
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,
                0.05f, 0.9f, 0f, 1f,    0.5273f, 0.136f,
                0f, 1f, 0f, 1f,         0.5273f, 0.136f,  // arrow head end
                                        // ~220x250
                0f, 0f, -1f, 1f,        0.859f, 0.976f, // - z-axis
                0f, 0f, 1f, 1f,         0.859f, 0.976f,  // + z-axis
                0f, -0.05f, 0.9f, 1f,   0.859f, 0.976f,// arrow head start
                0f, 0f, 1f, 1f,         0.859f, 0.976f,
                0f, 0.05f, 0.9f, 1f,    0.859f, 0.976f,
                0f, 0f, 1f, 1f,         0.859f, 0.976f,  // arrow head end
            }));
            axisLinesBinding = new VertexBufferBinding(axisLinesVertices, Utilities.SizeOf<float>() * 6, 0);
        }

        protected override void DoRender()
        {
            // Get the context reference
            var context = this.DeviceManager.Direct3DContext;

            // Set the shader resource
            context.PixelShader.SetShaderResource(0, textureView);
            // Set the sampler state
            context.PixelShader.SetSampler(0, samplerState);

            // Tell the IA we are using lines
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;
            // Pass in the line vertices
            context.InputAssembler.SetVertexBuffers(0, axisLinesBinding);
            // Draw the 18 vertices or our xyz-axis arrows
            context.Draw(18, 0);
        }
    }
}