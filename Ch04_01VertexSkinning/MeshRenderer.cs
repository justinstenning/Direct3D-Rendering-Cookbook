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

namespace Ch04_01VertexSkinning
{
    public class MeshRenderer: RendererBase
    {
        // The vertex buffer
        List<Buffer> vertexBuffers = new List<Buffer>();
        // The index buffer
        List<Buffer> indexBuffers = new List<Buffer>();

        // texture resources
        List<ShaderResourceView> textureViews = new List<ShaderResourceView>();
        // Control sampling behavior with this state
        SamplerState samplerState;

        Common.Mesh mesh;
        // Provide access to the underlying mesh object
        public Common.Mesh Mesh { get { return this.mesh; } }

        // The per material buffer to use so that the mesh parameters can be used
        public Buffer PerMaterialBuffer { get; set; }
        // The per armature constant buffer to use
        public Buffer PerArmatureBuffer { get; set; }

        public MeshRenderer(Common.Mesh mesh)
        {
            this.mesh = mesh;
        }

        protected override void CreateDeviceDependentResources()
        {
            // Dispose of each vertex and index buffer
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
                    // Retrieve skinning information for vertex
                    Common.Mesh.SkinningVertex skin = new Common.Mesh.SkinningVertex();
                    if (mesh.SkinningVertexBuffers.Count > 0)
                        skin = mesh.SkinningVertexBuffers[indx][i];

                    // Create vertex
                    vertices[i] = new Vertex(vb[i].Position, vb[i].Normal, vb[i].Color, vb[i].UV, skin);
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
            foreach (var m in mesh.Materials)
            {
                for (var i = 0; i < m.Textures.Length; i++)
                {
                    if (System.IO.File.Exists(m.Textures[i]))
                        textureViews.Add(ToDispose(ShaderResourceView.FromFile(device, m.Textures[i])));
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

            // Calculate skin matrices for each bone
            ConstantBuffers.PerArmature skinMatrices = new ConstantBuffers.PerArmature();
            if (mesh.Bones != null)
            {
                // Retrieve each bone's local transform
                for (var i = 0; i < mesh.Bones.Count; i++)
                {
                    skinMatrices.Bones[i] = mesh.Bones[i].BoneLocalTransform;
                }

                // TODO: Load bone transforms from animation frames here

                // Apply parent bone transforms
                // We assume here that the first bone has no parent
                // and that each parent bone appears before children
                for (var i = 1; i < mesh.Bones.Count; i++)
                {
                    var bone = mesh.Bones[i];
                    // ParentIndex == -1 means root bone
                    if (bone.ParentIndex > -1)
                    {
                        var parentTransform = skinMatrices.Bones[bone.ParentIndex];
                        skinMatrices.Bones[i] = (skinMatrices.Bones[i] * parentTransform);
                    }
                }

                // Change the bone transform from rest pose space into bone space (using the inverse of the bind/rest pose)
                for (var i = 0; i < mesh.Bones.Count; i++)
                {
                   skinMatrices.Bones[i] = Matrix.Transpose(mesh.Bones[i].InvBindPose * skinMatrices.Bones[i]);
                }

                // TODO: Check need to loop animation here
            }

            // Update the constant buffer with the skin matrices for each bone
            context.UpdateSubresource(skinMatrices.Bones, PerArmatureBuffer);

            // Draw sub-meshes grouped by material
            for (var mIndx = 0; mIndx < mesh.Materials.Count; mIndx++)
            {
                // Retrieve sub meshes for this material
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
