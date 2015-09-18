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

namespace Ch05_02TessellatedMesh
{
    public class QuadRenderer : Common.RendererBase
    {
        // The quad vertex buffer
        Buffer quadVertices;
        // The quad index buffer
        Buffer quadIndices;
        // The vertex buffer binding for the quad
        VertexBufferBinding quadBinding;

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
            RemoveAndDispose(ref quadVertices);
            RemoveAndDispose(ref quadIndices);
            RemoveAndDispose(ref textureView);
            RemoveAndDispose(ref samplerState);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;
            // Create a quad (two triangles)
            quadVertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new[] {
            /*  Vertex Position                       Vertex Color */
                //new Vertex(0.25f, 0.5f, -0.5f, Color.Lime), // Top-left
                //new Vertex(0.75f, 0.5f, -0.5f, Color.Yellow), // Top-right
                //new Vertex(0.75f, 0.0f, -0.5f, Color.Red), // Base-right
                //new Vertex(0.25f, 0.0f, -0.5f, Color.Blue), // Base-left

                new Vertex(0f, 0f, -0.001f, 0, 0, 1, 0.0f, 2.0f, Color.Black), // Base-left
                new Vertex(1.5f, 0f, -0.001f, 0, 0, 1, 2.0f, 2.0f, Color.Black), // Base-right
                new Vertex(1.5f, 1.5f, -0.001f, 0, 0, 1, 2.0f, 0.0f, Color.Black), // Top-right
                new Vertex(0f, 1.5f, -0.001f, 0, 0, 1, 0.0f, 0.0f, Color.Black), // Top-left
            }));
            quadBinding = new VertexBufferBinding(quadVertices, Utilities.SizeOf<Vertex>(), 0);

            // Load texture
            textureView = ToDispose(ShaderResourceView.FromFile(device, "Texture2.png"));

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
            var context = this.DeviceManager.Direct3DContext;

            // Render a quad

            // Set the shader resource
            context.PixelShader.SetShaderResource(0, textureView);
            // Set the sampler state
            context.PixelShader.SetSampler(0, samplerState);

            // Tell the IA we are using a patch list with 4 control points
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.PatchListWith4ControlPoints;
            context.InputAssembler.SetVertexBuffers(0, quadBinding);
            context.Draw(4, 0);
        }
    }
}