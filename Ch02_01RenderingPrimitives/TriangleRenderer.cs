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

namespace Ch02_01RenderingPrimitives
{
    public class TriangleRenderer : Common.RendererBase
    {
        // The triangle vertex buffer
        Buffer triangleVertices;

        // The vertex buffer binding structure for the triangle
        VertexBufferBinding triangleBinding;

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

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Create a triangle
            triangleVertices = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, new[] {
            /*  Vertex Position                       Vertex Color */
                new Vector4(0.0f, 0.0f, 0.5f, 1.0f),  new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Base-right
                new Vector4(-0.5f, 0.0f, 0.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Base-left
                new Vector4(-0.25f, 1f, 0.25f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // Apex
            }));
            triangleBinding = new VertexBufferBinding(triangleVertices, Utilities.SizeOf<Vector4>() * 2, 0);
        }

        protected override void DoRender()
        {
            // Get the context reference
            var context = this.DeviceManager.Direct3DContext;

            // Render the triangle

            // Tell the IA we are now using triangles
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            // Pass in the triangle vertices
            context.InputAssembler.SetVertexBuffers(0, triangleBinding);
            // Draw the 3 vertices of our triangle
            context.Draw(3, 0);
        }
    }
}