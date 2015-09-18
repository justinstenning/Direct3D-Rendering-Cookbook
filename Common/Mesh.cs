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
// -----------------------------------------------------------------------------
// Portions of code are ported from DirectXTk http://directxtk.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

#if NETFX_CORE
using Windows.Storage;
#endif

namespace Common
{
    /// <summary>
    /// Represents a single mesh within a Compiled Mesh Object (.CMO)
    /// </summary>
    public class Mesh
    {
        public const int MaxTextures = 8;  // 8 unique textures are supported.
        public const int MaxBoneInfluences = 4; // 4 bone influences are supported

        #region Mesh Structures (as per CMO file structure)

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SubMesh
        {
            public uint MaterialIndex;
            public uint IndexBufferIndex;
            public uint VertexBufferIndex;
            public uint StartIndex;
            public uint PrimCount;
        };
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Material
        {
            public string Name;

            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Specular;
            public float SpecularPower;
            public Vector4 Emissive;
            public Matrix UVTransform;

            public string[] Textures;
            public string VertexShaderName;
            public string PixelShaderName;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector4 Tangent;
            public Color Color;
            public Vector2 UV;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SkinningVertex
        {
            public uint BoneIndex0;
            public uint BoneIndex1;
            public uint BoneIndex2;
            public uint BoneIndex3;
            public float BoneWeight0;
            public float BoneWeight1;
            public float BoneWeight2;
            public float BoneWeight3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MeshExtent
        {
            public Vector3 Center;
            public float Radius;

            public Vector3 Min;
            public Vector3 Max;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Bone
        {
            // Bone name is stored in Mesh with BoneNames (indexes match between Bones and BoneNames)
            public int ParentIndex;
            public Matrix InvBindPose;
            public Matrix BindPose;
            public Matrix BoneLocalTransform;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Keyframe
        {
            public uint BoneIndex;
            public float Time;
            public Matrix Transform;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Animation
        {
            public float StartTime;
            public float EndTime;
            public List<Keyframe> Keyframes;
        };

        #endregion

        #region Additional helper structures

        public struct Triangle
        {
            public Vector3[] Points;
            public Vector3 FaceNormal;
            public int[] Indices;

            public Triangle(Vertex[] vertices, int i0, int i1, int i2)
            {
                Indices = new[] { i0, i1, i2 };
                this.Points = (from vertex in vertices select vertex.Position).ToArray();

                #region Calculate face normal

                // Calculate face normal
                Vector3 edge1 = this.Points[1] - this.Points[0];
                Vector3 edge2 = this.Points[2] - this.Points[0];
                Vector3 faceNormal = Vector3.Cross(edge1, edge2);

                // Calculate face normal direction using the vertex normals, rather than relying on vertex winding order
                Vector3 avgVertexNormal = Vector3.Normalize((vertices[0].Normal + vertices[1].Normal + vertices[2].Normal) / 3.0f);

                this.FaceNormal = (Vector3.Dot(faceNormal, avgVertexNormal) < 0.0f) ? -faceNormal : faceNormal;

                #endregion
            }

            public Vector3 DominantAxis()
            {
                Vector3 n = this.FaceNormal;
                float max = Math.Max(Math.Abs(n.X), Math.Max(Math.Abs(n.Y), Math.Abs(n.Y)));
                float x,y,z;
                x = Math.Abs(n.X) < max ? 0.0f : 1.0f;
                y = Math.Abs(n.Y) < max ? 0.0f : 1.0f;
                z = Math.Abs(n.Z) < max ? 0.0f : 1.0f;

                if (x > 0)
                    return Vector3.UnitX;
                else if (y > 0)
                    return Vector3.UnitY;
                else
                    return Vector3.UnitZ;
            }

        }

        #endregion

        #region Public properties

        public List<SubMesh> SubMeshes { get; private set; }
        public List<Material> Materials { get; private set; }
        public List<Vertex[]> VertexBuffers { get; private set; }
        public List<SkinningVertex[]> SkinningVertexBuffers { get; private set; }
        public List<ushort[]> IndexBuffers { get; private set; }
        public MeshExtent Extent { get; private set; }
        public Dictionary<string, Animation> Animations { get; private set; }
        public List<Bone> Bones { get; private set; }
        public List<string> BoneNames { get; private set; }
        public string Name { get; private set; }

        public List<Triangle> Triangles { get; private set; }

        public object Tag;

        #endregion

        #region Public methods

        public struct Ray
        {
            public Vector3 Direction;
            public Vector3 Origin;
        }

        public bool RayIntersects(Ray ray)
        {
            const float maxDistance = 10000;
            float min = maxDistance;
            bool isValid = false;



            //for (int i = 0; i < this.Triangles.Count; i++)
            //{
            //    Triangle tri = this.Triangles[i];

            //    float NdotD = Vector3.Dot(tri.FaceNormal, ray.Direction);
            //    if (Math.Abs(NdotD) < 0.001f)
            //    {
            //        // ray is too close to parallel to the polygon plane
            //        continue;
            //    }

            //    float t = (
            //}

            return isValid;
        }

        #endregion

        /// <summary>
        /// Loads a scene from the specified file, returning a list of mesh objects
        /// </summary>
        /// <param name="meshFilename"></param>
        /// <param name="shaderPathLocation"></param>
        /// <param name="texturePathLocation"></param>
        /// <returns></returns>
#if NETFX_CORE
        // Windows Store app - async loading of a mesh
        public async static Task<List<Mesh>> LoadFromFileAsync(string meshFilename)
#else
        public static List<Mesh> LoadFromFile(string meshFilename)
#endif
        {
            List<Mesh> loadedMeshes = new List<Mesh>();

            // open the mesh file
            Stream fs = null;
            try 
            {
#if NETFX_CORE
                if (!Path.IsPathRooted(meshFilename))
                    meshFilename = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, meshFilename);
                StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(meshFilename);
                using (fs = await file.OpenStreamForReadAsync()) 
#else
                using (fs = File.OpenRead(meshFilename))
#endif
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Read how many meshes are in the scene
                    var meshCount = br.ReadUInt32();

                    for (var i = 0; i < meshCount; i++)
                    {
                        // Load the mesh
                        Mesh mesh = Mesh.Load(br);
                        if (mesh != null)
                        {
                            loadedMeshes.Add(mesh);
                        }
                    }
                }
            }
            catch (Exception e) 
            {
                Debug.WriteLine("Unable to load mesh file '" + meshFilename + "': " + e.ToString());
            }

            return loadedMeshes;
        }

        #region CMO file structure
        /*
         .CMO files

         UINT - Mesh count
         { [Mesh count]
              UINT - Length of name
              wchar_t[] - Name of mesh (if length > 0)
              UINT - Material count
              { [Material count]
                  UINT - Length of material name
                  wchar_t[] - Name of material (if length > 0)
                  public Vector4 Ambient;
                  public Vector4 Diffuse;
                  public Vector4 Specular;
                  public float SpecularPower;
                  public Vector4 Emissive;
                  public Matrix UVTransform;
                  UINT - Length of pixel shader name
                  wchar_t[] - Name of pixel shader (if length > 0)
                  { [8]
                      UINT - Length of texture name
                      wchar_t[] - Name of texture (if length > 0)
                  }
              }
              BYTE - 1 if there is skeletal animation data present
              UINT - SubMesh count
              { [SubMesh count]
                  SubMesh structure
              }
              UINT - IB Count
              { [IB Count]
                  UINT - Number of USHORTs in IB
                  USHORT[] - Array of indices
              }
              UINT - VB Count
              { [VB Count]
                  UINT - Number of verts in VB
                  Vertex[] - Array of vertices
              }
              UINT - Skinning VB Count
              { [Skinning VB Count]
                  UINT - Number of verts in Skinning VB
                  SkinningVertex[] - Array of skinning verts
              }
              MeshExtents structure
              [If skeleton animation data is not present, file ends here]
              UINT - Bone count
              { [Bone count]
                  UINT - Length of bone name
                  wchar_t[] - Bone name (if length > 0)
                  Bone structure
              }
              UINT - Animation clip count
              { [Animation clip count]
                  UINT - Length of clip name
                  wchar_t[] - Clip name (if length > 0)
                  float - Start time
                  float - End time
                  UINT - Keyframe count
                  { [Keyframe count]
                      Keyframe structure
                  }
              }
         }
        */
        #endregion

        /// <summary>
        /// Load a mesh
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        static Mesh Load(BinaryReader br)
        {
            // Create new mesh object
            Mesh mesh = new Mesh();
                
            //      UINT - Length of name
            //      wchar_t[] - Name of mesh (if length > 0)
            //      UINT - Material count
            //      { [Material count]
            //          UINT - Length of material name
            //          wchar_t[] - Name of material (if length > 0)
            //          Vector4 Ambient;
            //          Vector4 Diffuse;
            //          Vector4 Specular;
            //          float SpecularPower;
            //          Vector4 Emissive;
            //          Matrix UVTransform;
            //          UINT - Length of pixel shader name
            //          wchar_t[] - Name of pixel shader (if length > 0)
            //          { [8]
            //              UINT - Length of texture name
            //              wchar_t[] - Name of texture (if length > 0)
            //          }
            //      }

            // read name of mesh
            mesh.Name = br.ReadCMO_wchar(); 
                
            // read material count
            int numMaterials = (int)br.ReadUInt32();
            mesh.Materials = new List<Material>(numMaterials);

            // load each material
            for (int i = 0; i < numMaterials; i++)
            {
                Material material = new Material();
                material.Name = br.ReadCMO_wchar();
                material.Ambient = br.ReadStructure<Vector4>();
                material.Diffuse = br.ReadStructure<Vector4>();
                material.Specular = br.ReadStructure<Vector4>();
                material.SpecularPower = br.ReadSingle();
                material.Emissive = br.ReadStructure<Vector4>();
                material.UVTransform = br.ReadStructure<Matrix>();
                    
                // read pixel shader name
                material.PixelShaderName = br.ReadCMO_wchar();

                //material.Textures = new string[Mesh.MaxTextures];
                List<String> textures = new List<string>();
                for (int t = 0; t < MaxTextures; t++)
                {
                    // read name of texture
                    var textureName = br.ReadCMO_wchar();
                    textures.Add(textureName);
                }
                material.Textures = textures.ToArray();

                mesh.Materials.Add(material);
            }

            //      BYTE - 1 if there is skeletal animation data present

            // is there skeletal animation data present?
            bool isAnimationData = br.ReadByte() == 1;

            //      UINT - SubMesh count
            //      { [SubMesh count]
            //          SubMesh structure
            //      }
            
            // load sub meshes if any
            int subMeshCount = (int)br.ReadUInt32();
            mesh.SubMeshes = new List<SubMesh>(subMeshCount);
            for (int i = 0; i < subMeshCount; i++) 
            {
                mesh.SubMeshes.Add(br.ReadStructure<SubMesh>());
            }

            //      UINT - IB Count
            //      { [IB Count]
            //          UINT - Number of USHORTs in IB
            //          USHORT[] - Array of indices
            //      }
            
            // load index buffers
            int indexBufferCount = (int)br.ReadUInt32();
            mesh.IndexBuffers = new List<ushort[]>(indexBufferCount);
            for (var i = 0; i < indexBufferCount; i++)
            {
                mesh.IndexBuffers.Add(br.ReadUInt16((int)br.ReadUInt32()));
            }
                
            //      UINT - VB Count
            //      { [VB Count]
            //          UINT - Number of verts in VB
            //          Vertex[] - Array of vertices
            //      }

            // load vertex buffers
            int vertexBufferCount = (int)br.ReadUInt32();
            mesh.VertexBuffers = new List<Vertex[]>(vertexBufferCount);
            for (var i = 0; i < vertexBufferCount; i++)
            {
                mesh.VertexBuffers.Add(br.ReadStructure<Vertex>((int)br.ReadUInt32()));
            }

            //      UINT - Skinning VB Count
            //      { [Skinning VB Count]
            //          UINT - Number of verts in Skinning VB
            //          SkinningVertex[] - Array of skinning verts
            //      }
            
            // load vertex skinning buffers
            int skinningVertexBufferCount = (int)br.ReadUInt32();
            mesh.SkinningVertexBuffers = new List<SkinningVertex[]>(skinningVertexBufferCount);
            for (var i = 0; i < skinningVertexBufferCount; i++)
            {
                mesh.SkinningVertexBuffers.Add(br.ReadStructure<SkinningVertex>((int)br.ReadUInt32()));
            }

            // load mesh extent
            mesh.Extent = br.ReadStructure<MeshExtent>();

            // load bone animation data
            if (isAnimationData)
            {
                //      UINT - Bone count
                //      { [Bone count]
                //          UINT - Length of bone name
                //          wchar_t[] - Bone name (if length > 0)
                //          Bone structure
                //      }
                int boneCount = (int)br.ReadUInt32();
                mesh.BoneNames = new List<string>(boneCount);
                mesh.Bones = new List<Bone>(boneCount);
                for (var i = 0; i < boneCount; i++)
                {
                    mesh.BoneNames.Add(br.ReadCMO_wchar());
                    mesh.Bones.Add(br.ReadStructure<Bone>());
                }

                //      UINT - Animation clip count
                //      { [Animation clip count]
                //          UINT - Length of clip name
                //          wchar_t[] - Clip name (if length > 0)
                //          float - Start time
                //          float - End time
                //          UINT - Keyframe count
                //          { [Keyframe count]
                //              Keyframe structure
                //          }
                //      }
                int animationCount = (int)br.ReadUInt32();
                mesh.Animations = new Dictionary<string,Animation>(animationCount);
                for (var i = 0; i < animationCount; i++)
                {
                    Animation animation;
                    string animationName = br.ReadCMO_wchar();
                    animation.StartTime = br.ReadSingle();
                    animation.EndTime = br.ReadSingle();
                    int keyframeCount = (int)br.ReadUInt32();
                    animation.Keyframes = new List<Keyframe>(keyframeCount);
                    for (var j = 0; j < keyframeCount; j++)
                        animation.Keyframes.Add(br.ReadStructure<Keyframe>());

                    mesh.Animations.Add(animationName, animation);
                }
            }

            mesh.Triangles = new List<Triangle>();

            // Load triangles and precalculate face normal
            foreach (var subMesh in mesh.SubMeshes)
            {
                var indexBuffer = mesh.IndexBuffers[(int)subMesh.IndexBufferIndex];
                var vertexBuffer = mesh.VertexBuffers[(int)subMesh.VertexBufferIndex];

                for (int i = 0; i < indexBuffer.Length; i += 3)
                {
                    var v0 = vertexBuffer[(int)indexBuffer[i]];
                    var v1 = vertexBuffer[(int)indexBuffer[i + 1]];
                    var v2 = vertexBuffer[(int)indexBuffer[i + 2]];
                    
                    Vertex[] vertices = new Vertex[3] {
                        v0, v1, v2
                    };

                    mesh.Triangles.Add(new Triangle(vertices, i, i + 1, i + 2));
                }
            }



            // return the final result
            return mesh;
        }
    }

    /// <summary>
    /// Extensions to BinaryReader to simplify loading .CMO scenes
    /// </summary>
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Loads a string from the CMO file (WCHAR prefixed with uint length)
        /// </summary>
        /// <param name="br"></param>
        /// <returns></returns>
        public static string ReadCMO_wchar(this BinaryReader br)
        {
            // uint - Length of string (in WCHAR's i.e. 2-bytes)
            // wchar[] - string (if length > 0)
            int length = (int)br.ReadUInt32();
            if (length > 0)
            {
                var result = System.Text.Encoding.Unicode.GetString(br.ReadBytes(length * 2), 0, length * 2);
                // Remove the trailing \0
                return result.Substring(0, result.Length - 1);
            }
            else
                return null;
        }

        static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
#if NETFX_CORE
            T stuff = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
#else
            T stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
#endif
            handle.Free();
            return stuff;
        }

        /// <summary>
        /// Read a structure from binary reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="br"></param>
        /// <returns></returns>
        public static T ReadStructure<T>(this BinaryReader br) where T : struct
        {
            return ByteArrayToStructure<T>(br.ReadBytes(Utilities.SizeOf<T>()));
        }

        /// <summary>
        /// Read <paramref name="count"/> instances of the structure from the binary reader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="br"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static T[] ReadStructure<T>(this BinaryReader br, int count) where T : struct
        {
            T[] result = new T[count];

            for (var i = 0; i < count; i++)
                result[i] = ByteArrayToStructure<T>(br.ReadBytes(Utilities.SizeOf<T>()));

            return result;
        }

        /// <summary>
        /// Read <paramref name="count"/> UInt16s.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ushort[] ReadUInt16(this BinaryReader br, int count)
        {
            ushort[] result = new ushort[count];
            for (var i = 0; i < count; i++)
                result[i] = br.ReadUInt16();
            return result;
        }
    }
}
