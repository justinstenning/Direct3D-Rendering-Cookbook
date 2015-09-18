using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch08_02Particles
{
    public class AxisGridRenderer : Common.RendererBase
    {
        // The vertex buffer
        Buffer vertexBuffer;
        int vertexCount = 0;

        protected override void CreateDeviceDependentResources()
        {
            base.CreateDeviceDependentResources();

            RemoveAndDispose(ref vertexBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            List<Vertex> vertices = new List<Vertex>();

            vertices.AddRange(new[] {
                new Vertex(0, 0, -4, Vector3.UnitY, Color.Blue),
                new Vertex(0, 0, 4,Vector3.UnitY,  Color.Blue),
                new Vertex(-4, 0, 0, Vector3.UnitY, Color.Red),
                new Vertex(4, 0, 0,Vector3.UnitY,  Color.Red),
            });
            for (var i = -4f; i < -0.09f; i += 0.2f)
            {
                vertices.Add(new Vertex(i, 0, -4, Color.Gray));
                vertices.Add(new Vertex(i, 0, 4, Color.Gray));
                vertices.Add(new Vertex(-i, 0, -4, Color.Gray));
                vertices.Add(new Vertex(-i, 0, 4, Color.Gray));

                vertices.Add(new Vertex(-4, 0, i, Color.Gray));
                vertices.Add(new Vertex(4, 0, i, Color.Gray));
                vertices.Add(new Vertex(-4, 0, -i, Color.Gray));
                vertices.Add(new Vertex(4, 0, -i, Color.Gray));
            }
            vertexCount = vertices.Count;
            vertexBuffer = ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices.ToArray()));
        }

        protected override void DoRender()
        {
            var context = this.DeviceManager.Direct3DContext;

            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            context.Draw(vertexCount, 0);
        }
    }
}
