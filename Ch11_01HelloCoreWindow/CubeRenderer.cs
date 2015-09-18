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

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch11_01HelloCoreWindow
{
    public class CubeRenderer : Common.RendererBase
    {
        // The vertex buffer
        Buffer vertexBuffer;
        // The index buffer
        Buffer indexBuffer;
        // The vertex buffer binding
        VertexBufferBinding vertexBinding;

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref vertexBuffer);
            RemoveAndDispose(ref indexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Create vertex buffer for cube
            vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new Vertex[] {
                    /*  Vertex Position    Color */
            new Vertex(-0.5f, 0.5f, -0.5f, Color.Red),  // 0-Top-left
            new Vertex(0.5f, 0.5f, -0.5f,  Color.Green),  // 1-Top-right
            new Vertex(0.5f, -0.5f, -0.5f,  Color.Blue), // 2-Base-right
            new Vertex(-0.5f, -0.5f, -0.5f, Color.Cyan), // 3-Base-left

            new Vertex(-0.5f, 0.5f, 0.5f,  Color.Yellow),  // 4-Top-left
            new Vertex(0.5f, 0.5f, 0.5f,   Color.Red),  // 5-Top-right
            new Vertex(0.5f, -0.5f, 0.5f,  Color.Green),  // 6-Base-right
            new Vertex(-0.5f, -0.5f, 0.5f, Color.Blue),  // 7-Base-left
            }));
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            // Front    Right    Top      Back     Left     Bottom  
            // v0    v1 v1    v5 v1    v0 v5    v4 v4    v0 v3    v2
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // | \ A |  | \ A |  | \ A |  | \ A |  | \ A |  | \ A |
            // | B \ |  | B \ |  | B \ |  | B \ |  | B \ |  | B \ |
            // |-----|  |-----|  |-----|  |-----|  |-----|  |-----|
            // v3    v2 v2    v6 v5    v4 v6    v7 v7    v3 v7    v6
            indexBuffer = ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, new ushort[] {
                // using Right-handed coordinates, therefore counter-clockwise
                0, 2, 1, // Front A
                0, 3, 2, // Front B
                1, 6, 5, // Right A
                1, 2, 6, // Right B
                1, 4, 0, // Top A
                1, 5, 4, // Top B
                5, 7, 4, // Back A
                5, 6, 7, // Back B
                4, 3, 0, // Left A
                4, 7, 3, // Left B
                3, 6, 2, // Bottom A
                3, 7, 6, // Bottom B
            }));
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            // Tell the IA we are using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Set the index buffer
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
            // Pass in the vertices (note: only 8 vertices)
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            // Draw the 36 vertices using the vertex indices
            context.DrawIndexed(36, 0, 0);
            // Note: we have called DrawIndexed so that the index buffer will be used
        }
    }
}