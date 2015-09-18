// Copyright (c) 2013 Justin Stenning
// This software contains source code provided by NVIDIA Corporation.
// Portions adapted from https://code.google.com/p/nvidia-mesh-tools/
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
//-----------------------------
// Copyright (c) 2009 NVIDIA Corporation
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

using System.Runtime.Serialization;
using System.IO;
using SharpDX;

namespace Common
{
    interface IBinarySerializable
    {
        void Serialize(BinaryWriter toStream);
        void Deserialize(BinaryReader fromStream);
    }

    /// <summary>
    /// Implementation of the Bicubic Bezier + Gregory Patch ACC mesh
    /// The bzrexport tool generates this mesh from regular .OBJ models.
    /// To get bzrexport to work with this class needs some minor changes, including changing the version in the generated file.
    /// </summary>
    public class BezierMesh: IBinarySerializable
    {
        public struct BezierHeader
        {
            public int Header;
            public int Version;
            
            /// <summary>
            /// The number of regular bicubic bezier patches
            /// </summary>
            public int RegularPatchCount;
            /// <summary>
            /// The number of Tensor-product Gregory patches
            /// </summary>
            public int QuadPatchCount;
            /// <summary>
            /// The number of triangular Gregory patches
            /// </summary>
            public int TriPatchCount;
        }

        public BezierHeader Header;

        // 16 per regular patch
        public IReadOnlyCollection<Vector3> RegularBezierControlPoints { get { return _RegularBezierControlPoints.AsReadOnly(); } }
        public IReadOnlyCollection<Vector2> RegularTextureCoordinates { get { return _RegularTextureCoordinates.AsReadOnly(); } }
        public IReadOnlyCollection<Vector3> RegularNormals { get { return _RegularNormals.AsReadOnly(); } }

        // 32 per quad patch
        public IReadOnlyCollection<Vector3> QuadBezierControlPoints { get { return _QuadBezierControlPoints.AsReadOnly(); } }
        // 20 per quad patch
        public IReadOnlyCollection<Vector3> QuadGregoryControlPoints { get { return _QuadGregoryControlPoints.AsReadOnly(); } }
        // 24 per quad patch
        public IReadOnlyCollection<Vector3> QuadPmControlPoints { get { return _QuadPmControlPoints.AsReadOnly(); } }
        // 16 per patch
        public IReadOnlyCollection<Vector2> QuadTextureCoordinates { get { return _QuadTextureCoordinates.AsReadOnly(); } }
        // 16 per patch
        public IReadOnlyCollection<Vector3> QuadNormals { get { return _QuadNormals.AsReadOnly(); } }

        public IReadOnlyCollection<Vector3> TriGregoryControlPoints { get { return _TriGregoryControlPoints.AsReadOnly(); } }
        public IReadOnlyCollection<Vector3> TriPmControlPoints { get { return _TriPmControlPoints.AsReadOnly(); } }
        public IReadOnlyCollection<Vector2> TriTextureCoordinates { get { return _TriTextureCoordinates.AsReadOnly(); } }

        // 16 per regular patch
        List<Vector3> _RegularBezierControlPoints;
        List<Vector2> _RegularTextureCoordinates;
        List<Vector3> _RegularNormals;

        // 32 per quad patch
        List<Vector3> _QuadBezierControlPoints;
        // 20 per quad patch
        List<Vector3> _QuadGregoryControlPoints;
        // 24 per quad patch
        List<Vector3> _QuadPmControlPoints;
        // 16 per patch
        List<Vector2> _QuadTextureCoordinates;
        // 16 per patch
        List<Vector3> _QuadNormals;

        List<Vector3> _TriGregoryControlPoints;
        List<Vector3> _TriPmControlPoints;
        List<Vector2> _TriTextureCoordinates;

        public int FaceTopologyCount;
        public int PrimitiveSize;

        // Length = FaceTopologyCount * 32 * PrimitiveSize
        public IReadOnlyCollection<float> BezierStencil { get { return _BezierStencil.AsReadOnly(); } }

        // Length = FaceTopologyCount * 20 * PrimitiveSize
        public IReadOnlyCollection<float> GregoryStencil { get { return _GregoryStencil.AsReadOnly(); } }

        public IReadOnlyCollection<int> RegularFaceTopologyIndex { get { return _RegularFaceTopologyIndex.AsReadOnly(); } }
        public IReadOnlyCollection<int> RegularVertexIndices { get { return _RegularVertexIndices.AsReadOnly(); } }
        public IReadOnlyCollection<int> RegularStencilIndices { get { return _RegularStencilIndices.AsReadOnly(); } }
        public IReadOnlyCollection<int> QuadFaceVertexCount { get { return _QuadFaceVertexCount.AsReadOnly(); } }
        public IReadOnlyCollection<int> QuadFaceTopologyIndex { get { return _QuadFaceTopologyIndex.AsReadOnly(); } }
        public IReadOnlyCollection<int> QuadVertexIndices { get { return _QuadVertexIndices.AsReadOnly(); } }
        public IReadOnlyCollection<int> QuadStencilIndices { get { return _QuadStencilIndices.AsReadOnly(); } }

        public IReadOnlyCollection<Vector3> Vertices { get { return _Vertices.AsReadOnly(); } }
        public IReadOnlyCollection<Vector3> Tangents { get { return _Tangents.AsReadOnly(); } }
        public IReadOnlyCollection<int> Valences { get { return _Valences.AsReadOnly(); } }
        public IReadOnlyCollection<Vector2> TextureCoordinates { get { return _TextureCoordinates.AsReadOnly(); } }
        public int MaxValence;

        public IReadOnlyCollection<int> RegularFaceIndices { get { return _RegularFaceIndices.AsReadOnly(); } }
        public IReadOnlyCollection<int> QuadFaceIndices { get { return _QuadFaceIndices.AsReadOnly(); } }
        public IReadOnlyCollection<int> TriFaceIndices { get { return _TriFaceIndices.AsReadOnly(); } }

        List<float> _BezierStencil;

        // Length = FaceTopologyCount * 20 * PrimitiveSize
        List<float> _GregoryStencil;

        List<int> _RegularFaceTopologyIndex;
        List<int> _RegularVertexIndices;
        List<int> _RegularStencilIndices;
        List<int> _QuadFaceVertexCount;
        List<int> _QuadFaceTopologyIndex;
        List<int> _QuadVertexIndices;
        List<int> _QuadStencilIndices;

        public int VertexCount;
        List<Vector3> _Vertices;
        List<Vector3> _Tangents;
        List<int> _Valences;
        List<Vector2> _TextureCoordinates;

        List<int> _RegularFaceIndices;
        List<int> _QuadFaceIndices;
        List<int> _TriFaceIndices;

        // Triangle list of all vertices that make up the base mesh (excluding control points)
        public IReadOnlyCollection<int> InputMeshIndices { get { return _InputMeshIndices.AsReadOnly(); } }
        List<int> _InputMeshIndices;

        public IReadOnlyCollection<int> Indices { get { return _Indices.AsReadOnly(); } }
        List<int> _Indices;

        public Vector3 Center { get; private set; }

        public void Serialize(BinaryWriter to)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(BinaryReader from)
        {
            // Bezier File Format :
            //   Header ('BZR ')            | sizeof(uint)
            //   Version (1.1)              | sizeof(uint)
            //   Regular patch count        | sizeof(uint)
            //   Quad patch count           | sizeof(uint)
            //   Triangle patch count       | sizeof(uint)

            this.Header = from.ReadStructure<BezierMesh.BezierHeader>();
            
            byte[] expectedHeader = {Convert.ToByte(' '), Convert.ToByte('R'), Convert.ToByte('Z'), Convert.ToByte('B')};

            if (BitConverter.IsLittleEndian)
                Array.Reverse(expectedHeader);

            if (Header.Header != BitConverter.ToInt32(expectedHeader,0) || (Header.Version != 0x0100 && Header.Version != 0x0101))
            {
                throw new ArgumentException("Incorrect input header or unsupported file format version");
            }


            //   Part 1.  Precomputed Control Points:
            //     Regular Patches:
            //       Bezier control points        | 16 * regularPatchCount * sizeof(float3)
            //       Texture coordinates          | 16 * regularPatchCount * sizeof(float2)
            //       Normal control points        | 16 * regularPatchCount * sizeof(float3)
            _RegularBezierControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.RegularPatchCount * 16));
            _RegularTextureCoordinates = new List<Vector2>(from.ReadStructure<Vector2>(Header.RegularPatchCount * 16));
            if (Header.Version == 0x0101)
            {
                // Load normals
                _RegularNormals = new List<Vector3>(from.ReadStructure<Vector3>(Header.RegularPatchCount * 16));
            } else 
            {
                _RegularNormals = new List<Vector3>();
            }

            //     Quad Patches:
            //       Bezier control points        | 32 * quadPatchCount * sizeof(float3)
            //       Gregory control points       | 20 * quadPatchCount * sizeof(float3)
            //       Pm control points            | 24 * quadPatchCount * sizeof(float3)
            //       Texture coordinates          | 16 * quadPatchCount * sizeof(float2)
            //       Normal control points        | 16 * quadPatchCount * sizeof(float3)
            _QuadBezierControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.QuadPatchCount * 32));
            _QuadGregoryControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.QuadPatchCount * 20));
            _QuadPmControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.QuadPatchCount * 24));
            _QuadTextureCoordinates = new List<Vector2>(from.ReadStructure<Vector2>(Header.QuadPatchCount * 16));
            if (Header.Version == 0x0101)
            {
                // Load normals
                _QuadNormals = new List<Vector3>(from.ReadStructure<Vector3>(Header.QuadPatchCount * 16));
            }
            else
            {
                _QuadNormals = new List<Vector3>();
            }

            //     Triangle Patches:
            //       Gregory control points       | 15 * trianglePatchCount * sizeof(float3)
            //       Pm control points            | 19 * trianglePatchCount * sizeof(float3)
            //       Texture coordinates          | 12 * trianglePatchCount * sizeof(float2)
            _TriGregoryControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.TriPatchCount * 15));
            _TriPmControlPoints = new List<Vector3>(from.ReadStructure<Vector3>(Header.TriPatchCount * 19));
            _TriTextureCoordinates = new List<Vector2>(from.ReadStructure<Vector2>(Header.TriPatchCount * 12));
            
            //   Part 2. Stencils:
            //     faceTopologyCount              | sizeof(int)
            //     primitiveSize                  | sizeof(int)
            //     Bezier Stencil                 | faceTopologyCount * 32 * m_primitiveSize * sizeof(float)
            //     Gregory Stencil                | faceTopologyCount * 20 * m_primitiveSize * sizeof(float)
            //     regularFaceTopologyIndex       | regularPatchCount * sizeof(uint)
            //     regularVertexIndices           | 16 * regularPatchCount * sizeof(uint)
            //     regularStencilIndices          | 16 * regularPatchCount * sizeof(uint)
            //     quadVertexCount                | quadPatchCount * sizeof(uint)
            //     quadFaceTopologyIndex          | quadPatchCount * sizeof(uint)
            //     quadVertexIndices              | primitiveSize * quadpatchCount * sizeof(uint)
            //     quadStecilIndices              | primitiveSize * quadPatchCount * sizeof(uint)

            FaceTopologyCount = from.ReadInt32();
            PrimitiveSize = from.ReadInt32();

            _BezierStencil = new List<float>(from.ReadStructure<float>(FaceTopologyCount * 32 * PrimitiveSize));
            _GregoryStencil = new List<float>(from.ReadStructure<float>(FaceTopologyCount * 20 * PrimitiveSize));

            _RegularFaceTopologyIndex = new List<int>(from.ReadStructure<int>(Header.RegularPatchCount));
            _RegularVertexIndices = new List<int>(from.ReadStructure<int>(Header.RegularPatchCount * 16));
            _RegularStencilIndices = new List<int>(from.ReadStructure<int>(Header.RegularPatchCount * 16));

            _QuadFaceVertexCount = new List<int>(from.ReadStructure<int>(Header.QuadPatchCount));
            _QuadFaceTopologyIndex = new List<int>(from.ReadStructure<int>(Header.QuadPatchCount));
            _QuadVertexIndices = new List<int>(from.ReadStructure<int>(Header.QuadPatchCount * PrimitiveSize));
            _QuadStencilIndices = new List<int>(from.ReadStructure<int>(Header.QuadPatchCount * PrimitiveSize));

            //   Part 3. Input Mesh Topology:
            //     Vertex count                   | sizeof(uint)
            //     Vertices                       | vertexCount * sizeof(float3)
            //     Tangents/Bitangents            | vertexCount * 2 * sizeof(float3)
            //     Valences                       | vertexCount * sizeof(int)
            //     Texture coordinates            | vertexCount * sizeof(float2)
            //     Max valence                    | sizeof(uint)
            //     Regular face indices           | 4 * regularPatchCount * sizeof(uint)
            //     Quad face indices              | 4 * irregularpatchCount * sizeof(uint)
            //     Triangle face indices          | 3 * trianglePatchCount * sizeof(uint)

            VertexCount = from.ReadInt32();
            _Vertices = new List<Vector3>(from.ReadStructure<Vector3>(VertexCount));
            _Tangents = new List<Vector3>(from.ReadStructure<Vector3>(VertexCount * 2));
            _Valences = new List<int>(from.ReadStructure<int>(VertexCount));
            _TextureCoordinates = new List<Vector2>(from.ReadStructure<Vector2>(VertexCount));

            MaxValence = from.ReadInt32();

            _RegularFaceIndices = new List<int>(from.ReadStructure<int>(Header.RegularPatchCount * 4));
            _QuadFaceIndices = new List<int>(from.ReadStructure<int>(Header.QuadPatchCount * 4));
            _TriFaceIndices = new List<int>(from.ReadStructure<int>(Header.TriPatchCount * 3));

            #region Calculate the patch corner point indices relative to the patch

            // For each regular patch (bicubic bezier) set the indices for the 
            // regular that define the face (i.e. the 4 corners)
            var regularIndices = new int[Header.RegularPatchCount * 4];
            for (int k = 0; k < Header.RegularPatchCount; k++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        if (_RegularVertexIndices[k * 16 + i] == _RegularFaceIndices[k * 4 + j])
                        {
                            regularIndices[k * 4 + j] = i;
                        }
                    }
                }
            }

            // For an irregular patch (Gregory patch) set the indices for the
            // patch that define the face (i.e. the 4 corners)
            var quadIndices = new int[Header.QuadPatchCount * 4];
            for (int k = 0; k < Header.QuadPatchCount; k++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < PrimitiveSize; i++)
                    {
                        if (_QuadVertexIndices[k * PrimitiveSize + i] == _QuadFaceIndices[k * 4 + j])
                        {
                            quadIndices[k * 4 + j] = i;
                        }
                    }
                }
            }

            #endregion

            #region Compute input mesh indices

            var indexCount = 6 * Header.RegularPatchCount + 6 * Header.QuadPatchCount + 3 * Header.TriPatchCount;
            //var indexCount2 = 8 * Header.RegularPatchCount + 8 * Header.QuadPatchCount;

            var indices = new int[indexCount];

            int idx = 0;
            for (int i = 0; i < Header.RegularPatchCount; i++)
            {
                indices[idx++] = _RegularFaceIndices[4 * i + 2];
                indices[idx++] = _RegularFaceIndices[4 * i + 0];
                indices[idx++] = _RegularFaceIndices[4 * i + 1];

                indices[idx++] = _RegularFaceIndices[4 * i + 0];
                indices[idx++] = _RegularFaceIndices[4 * i + 2];
                indices[idx++] = _RegularFaceIndices[4 * i + 3];
            }
            for (int i = 0; i < Header.QuadPatchCount; i++)
            {
                indices[idx++] = _QuadFaceIndices[4 * i + 2];
                indices[idx++] = _QuadFaceIndices[4 * i + 0];
                indices[idx++] = _QuadFaceIndices[4 * i + 1];
                                  
                indices[idx++] = _QuadFaceIndices[4 * i + 0];
                indices[idx++] = _QuadFaceIndices[4 * i + 2];
                indices[idx++] = _QuadFaceIndices[4 * i + 3];
            }

            for (int i = 0; i < Header.TriPatchCount; i++)
            {
                indices[idx++] = _TriFaceIndices[4 * i + 2];
                indices[idx++] = _TriFaceIndices[4 * i + 0];
                indices[idx++] = _TriFaceIndices[4 * i + 1];
            }

            _Indices = new List<int>(indices);
            #endregion

            // Calculate the bounding box
            Vector3 minCorner = _Vertices[0];
            Vector3 maxCorner = _Vertices[0];

            foreach (var vertex in _Vertices)
            {
                if (minCorner.X > vertex.X) minCorner.X = vertex.X;
                else if (maxCorner.X < vertex.X) maxCorner.X = vertex.X;

                if (minCorner.Y > vertex.Y) minCorner.Y = vertex.Y;
                else if (maxCorner.Y < vertex.Y) maxCorner.Y = vertex.Y;

                if (minCorner.Z > vertex.Z) minCorner.Z = vertex.Z;
                else if (maxCorner.Z < vertex.Z) maxCorner.Z = vertex.Z;
            }

            Center = (minCorner + maxCorner) * 0.5f;
        }
    }

    // Bezier File Format :
    //   Header ('BZR ')            | sizeof(uint)
    //   Version (1.1)              | sizeof(uint)
    //   Regular patch count        | sizeof(uint)
    //   Quad patch count           | sizeof(uint)
    //   Triangle patch count       | sizeof(uint)

    //   Part 1.  Precomputed Control Points:
    //     Regular Patches:
    //       Bezier control points        | 16 * regularPatchCount * sizeof(float3)
    //       Texture coordinates          | 16 * regularPatchCount * sizeof(float2)
    //       Normal control points        | 16 * regularPatchCount * sizeof(float3)
    //     Quad Patches:
    //       Bezier control points        | 32 * quadPatchCount * sizeof(float3)
    //       Gregory control points       | 20 * quadPatchCount * sizeof(float3)
    //       Pm control points            | 24 * quadPatchCount * sizeof(float3)
    //       Texture coordinates          | 16 * quadPatchCount * sizeof(float2)
    //       Normal control points        | 16 * quadPatchCount * sizeof(float3)
    //     Triangle Patches:
    //       Gregory control points       | 15 * trianglePatchCount * sizeof(float3)
    //       Pm control points            | 19 * trianglePatchCount * sizeof(float3)
    //       Texture coordinates          | 12 * trianglePatchCount * sizeof(float2)

    //   Part 2. Stencils:
    //     faceTopologyCount              | sizeof(int)
    //     primitiveSize                  | sizeof(int)
    //     Bezier Stencil                 | faceTopologyCount * 32 * m_primitiveSize * sizeof(float)
    //     Gregory Stencil                | faceTopologyCount * 20 * m_primitiveSize * sizeof(float)
    //     regularFaceTopologyIndex       | regularPatchCount * sizeof(uint)
    //     regularVertexIndices           | 16 * regularPatchCount * sizeof(uint)
    //     regularStencilIndices          | 16 * regularPatchCount * sizeof(uint)
    //     quadVertexCount                | quadPatchCount * sizeof(uint)
    //     quadFaceTopologyIndex          | quadPatchCount * sizeof(uint)
    //     quadVertexIndices              | primitiveSize * quadpatchCount * sizeof(uint)
    //     quadStecilIndices              | primitiveSize * quadPatchCount * sizeof(uint)

    //   Part 3. Input Mesh Topology:
    //     Vertex count                   | sizeof(uint)
    //     Vertices                       | vertexCount * sizeof(float3)
    //     Tangents/Bitangents            | vertexCount * 2 * sizeof(float3)
    //     Valences                       | vertexCount * sizeof(int)
    //     Texture coordinates            | vertexCount * sizeof(float2)
    //     Max valence                    | sizeof(uint)
    //     Regular face indices           | 4 * regularPatchCount * sizeof(uint)
    //     Quad face indices              | 4 * irregularpatchCount * sizeof(uint)
    //     Triangle face indices          | 3 * trianglePatchCount * sizeof(uint)
}
