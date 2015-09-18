

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch03_02WithCubeMapping
{

    public class SphereRenderer : Common.RendererBase
    {
        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;

        ShaderResourceView textureView;
        SamplerState samplerState;

        int totalVertexCount = 0;

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref vertexBuffer);
            RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Load texture (a DDS cube map)
            textureView = ShaderResourceView.FromFile(device, "CubeMap.dds");

            // Create our sampler state
            samplerState = new SamplerState(device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                MaximumLod = 9, // Our cube map has 10 mip map levels (0-9)
                MinimumLod = 0,
                MipLodBias = 0.0f
            });

            Vertex[] vertices;
            int[] indices;
            GeometricPrimitives.GenerateSphere(out vertices, out indices, Color.Gray);

            vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices));
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, indices));
            totalVertexCount = indices.Length;
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            context.PixelShader.SetShaderResource(0, textureView);
            context.PixelShader.SetSampler(0, samplerState);

            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            // Pass in the quad vertices (note: only 4 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 36 vertices that make up the two triangles in the quad
            // using the vertex indices
            context.DrawIndexed(totalVertexCount, 0, 0);
            // Note: we have called DrawIndexed so that the index buffer will be used
        }
    }

}