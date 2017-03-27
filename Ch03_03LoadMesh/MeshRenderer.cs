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

namespace Ch03_03LoadMesh
{
    public class MeshRenderer: RendererBase
    {
        // The vertex buffer
        List<Buffer> vertexBuffers = new List<Buffer>();
        // The index buffer
        List<Buffer> indexBuffers = new List<Buffer>();
        // Texture resources
        List<ShaderResourceView> textureViews = new List<ShaderResourceView>();
        public List<ShaderResourceView> TextureViews { get { return textureViews; } }
        // Control sampling behavior with this state
        SamplerState samplerState;

        Common.Mesh mesh;
        // Provide access to the underlying mesh object
        public Common.Mesh Mesh { get { return this.mesh; } }

        // The per material buffer to use so that the mesh parameters can be used
        public Buffer PerMaterialBuffer { get; set; }

        public MeshRenderer(Common.Mesh mesh)
        {
            this.mesh = mesh;
        }

        protected override void CreateDeviceDependentResources()
        {
            // Dispose of each vertex, index buffer and texture
            vertexBuffers.ForEach(vb => RemoveAndDispose(ref vb));
            vertexBuffers.Clear();
            indexBuffers.ForEach(ib => RemoveAndDispose(ref ib));
            indexBuffers.Clear();
            textureViews.ForEach(tv => RemoveAndDispose(ref tv));
            textureViews.Clear();
            RemoveAndDispose(ref samplerState);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            // Initialize vertex buffers
            for (int indx = 0; indx < mesh.VertexBuffers.Count; indx++)
            {
                var vb = mesh.VertexBuffers[indx];
                Vertex[] vertices = new Vertex[vb.Length];
                for (var i = 0; i < vb.Length; i++)
                {
                    // Create vertex
                    vertices[i] = new Vertex(vb[i].Position, vb[i].Normal, vb[i].Color, vb[i].UV);
                }

                vertexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.VertexBuffer, vertices.ToArray())));
                vertexBuffers[vertexBuffers.Count - 1].DebugName = "VertexBuffer_" + indx.ToString();
            }

            // Initialize index buffers
            foreach (var ib in mesh.IndexBuffers)
            {
                indexBuffers.Add(ToDispose(Buffer.Create(device, BindFlags.IndexBuffer, ib)));
                indexBuffers[indexBuffers.Count - 1].DebugName = "IndexBuffer_" + (indexBuffers.Count - 1).ToString();
            }

            // Load textures if a material has any.
            // The CMO file format supports up to 8 per material
            foreach (var m in mesh.Materials)
            {
                // Diffuse Color
                for (var i = 0; i < m.Textures.Length; i++)
                {
                    if (SharpDX.IO.NativeFile.Exists(m.Textures[i]))
                        textureViews.Add(ToDispose(TextureLoader.ShaderResourceViewFromFile(device, m.Textures[i])));
                    else
                        textureViews.Add(null);
                }
            }

            // Create our sampler state
            samplerState = ToDispose(new SamplerState(device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
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
            // Retrieve device context
            var context = this.DeviceManager.Direct3DContext;

            // Draw sub-meshes grouped by material
            for (var mIndx = 0; mIndx < mesh.Materials.Count; mIndx++)
            {
                var subMeshesForMaterial =
                    (from sm in mesh.SubMeshes
                     where sm.MaterialIndex == mIndx
                     select sm).ToArray();

                // If the material buffer is available and there are submeshes
                // using the material update the PerMaterialBuffer
                if (PerMaterialBuffer != null && subMeshesForMaterial.Length > 0)
                {
                    // update the PerMaterialBuffer constant buffer
                    var material = new ConstantBuffers.PerMaterial()
                    {
                        Ambient = new Color4(mesh.Materials[mIndx].Ambient),
                        Diffuse = new Color4(mesh.Materials[mIndx].Diffuse),
                        Emissive = new Color4(mesh.Materials[mIndx].Emissive),
                        Specular = new Color4(mesh.Materials[mIndx].Specular),
                        SpecularPower = mesh.Materials[mIndx].SpecularPower,
                        UVTransform = mesh.Materials[mIndx].UVTransform,
                    };

                    // Bind textures to the pixel shader
                    int texIndxOffset = mIndx * Common.Mesh.MaxTextures;
                    material.HasTexture = (uint)(textureViews[texIndxOffset] != null ? 1 : 0); // 0=false
                    context.PixelShader.SetShaderResources(0, textureViews.GetRange(texIndxOffset, Common.Mesh.MaxTextures).ToArray());

                    // Set texture sampler state
                    context.PixelShader.SetSampler(0, samplerState);

                    // Update material buffer
                    context.UpdateSubresource(ref material, PerMaterialBuffer);
                }

                // For each sub-mesh
                foreach (var subMesh in subMeshesForMaterial)
                {
                    // Ensure the vertex buffer and index buffers are in range
                    if (subMesh.VertexBufferIndex < vertexBuffers.Count && subMesh.IndexBufferIndex < indexBuffers.Count)
                    {
                        // Retrieve and set the vertex and index buffers
                        var vertexBuffer = vertexBuffers[(int)subMesh.VertexBufferIndex];
                        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
                        context.InputAssembler.SetIndexBuffer(indexBuffers[(int)subMesh.IndexBufferIndex], Format.R16_UInt, 0);
                        // Set topology
                        context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                    }

                    // Draw the sub-mesh (includes Primitive count which we multiply by 3)
                    // The submesh also includes a start index into the vertex buffer
                    context.DrawIndexed((int)subMesh.PrimCount * 3, (int)subMesh.StartIndex, 0);
                }
            }

            // If there are no materials
            if (mesh.Materials.Count == 0)
            {
                foreach (var subMesh in mesh.SubMeshes)
                {
                    // Ensure the vertex buffer and index buffers are in range
                    if (subMesh.VertexBufferIndex < vertexBuffers.Count && subMesh.IndexBufferIndex < indexBuffers.Count)
                    {
                        // Retrieve and set the vertex and index buffers
                        var vertexBuffer = vertexBuffers[(int)subMesh.VertexBufferIndex];
                        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
                        context.InputAssembler.SetIndexBuffer(indexBuffers[(int)subMesh.IndexBufferIndex], Format.R16_UInt, 0);
                        // Set topology
                        context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                    }

                    // Draw the sub-mesh (includes Primitive count which we multiply by 3)
                    // The submesh also includes a start index into the vertex buffer
                    context.DrawIndexed((int)subMesh.PrimCount * 3, (int)subMesh.StartIndex, 0);
                }
            }
        }
    }
}
