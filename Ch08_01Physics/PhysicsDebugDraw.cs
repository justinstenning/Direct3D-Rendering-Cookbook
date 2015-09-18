using Common;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BulletSharp;

using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using DataStream = SharpDX.DataStream;
using SharpDX;

namespace Ch08_01Physics
{
    public class PhysicsDebugDraw : BufferedDebugDraw
    {
        Device device;
        InputAssemblerStage inputAssembler;
        InputLayout inputLayout;
        BufferDescription vertexBufferDesc;
        PositionColored[] lineArray;
        Buffer vertexBuffer;
        VertexBufferBinding vertexBufferBinding;

        VertexShader vertexShader;
        PixelShader pixelShader;

        public PhysicsDebugDraw(DeviceManager manager)
        {
            device = manager.Direct3DDevice;
            inputAssembler = device.ImmediateContext.InputAssembler;
            lineArray = new PositionColored[0];

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\PhysicsDebug.hlsl", "VSMain", "vs_5_0"))
            {
                vertexShader = new VertexShader(device, bc);
                
                InputElement[] elements = new InputElement[]
                {
                    new InputElement("SV_POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                    new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 12, 0, InputClassification.PerVertexData, 0)
                };
                inputLayout = new InputLayout(device, bc, elements);
            }

            vertexBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write
            };

            vertexBufferBinding = new VertexBufferBinding(null, PositionColored.Stride, 0);

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\PhysicsDebug.hlsl", "PSMain", "ps_5_0"))
                pixelShader = new PixelShader(device, bc);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (vertexBuffer != null)
                {
                    vertexBuffer.Dispose();
                    vertexBuffer = null;
                }

                if (vertexShader != null)
                {
                    vertexShader.Dispose();
                    vertexShader = null;
                }

                if (pixelShader != null)
                {
                    pixelShader.Dispose();
                    pixelShader = null;
                }

                if (inputLayout != null)
                {
                    inputLayout.Dispose();
                    inputLayout = null;
                }
            }

            base.Dispose(disposing);
        }

        public void DrawDebugWorld(DynamicsWorld world)
        {
            world.DebugDrawWorld();

            if (lines.Count == 0)
                return;

            inputAssembler.InputLayout = inputLayout;

            if (lineArray.Length != lines.Count)
            {
                lineArray = new PositionColored[lines.Count];
                lines.CopyTo(lineArray);

                if (vertexBuffer != null)
                {
                    vertexBuffer.Dispose();
                }
                vertexBufferDesc.SizeInBytes = PositionColored.Stride * lines.Count;
                using (var data = new DataStream(vertexBufferDesc.SizeInBytes, false, true))
                {
                    data.WriteRange(lineArray);
                    data.Position = 0;
                    vertexBuffer = new Buffer(device, data, vertexBufferDesc);
                }
                vertexBufferBinding.Buffer = vertexBuffer;
            }
            else
            {
                lines.CopyTo(lineArray);

                DataStream ds;
                var map = device.ImmediateContext.MapSubresource(vertexBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out ds);
                ds.WriteRange(lineArray);
                device.ImmediateContext.UnmapSubresource(vertexBuffer, 0);
            }

            inputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            inputAssembler.PrimitiveTopology = global::SharpDX.Direct3D.PrimitiveTopology.LineList;

            device.ImmediateContext.VertexShader.Set(vertexShader);
            device.ImmediateContext.PixelShader.Set(pixelShader);
            device.ImmediateContext.Draw(lines.Count, 0);

            lines.Clear();
        }
    }
}
