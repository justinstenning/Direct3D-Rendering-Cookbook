using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Ch03_01CubeAndSphere
{
    public static class ConstantBuffers
    {
        /// <summary>
        /// Per Object constant buffer (matrices)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PerObject
        {
            // WorldViewProjection matrix
            public Matrix WorldViewProjection;

            // We need the world matrix so that we can
            // calculate the lighting in world space
            public Matrix World;

            // Inverse transpose of World
            public Matrix WorldInverseTranspose;

            /// <summary>
            /// Transpose the matrices so that they are in row major order for HLSL
            /// </summary>
            internal void Transpose()
            {
                this.World.Transpose();
                this.WorldInverseTranspose.Transpose();
                this.WorldViewProjection.Transpose();
            }
        }

        /// <summary>
        /// Per frame constant buffer (camera position)
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerFrame
        {
            public SharpDX.Vector3 CameraPosition;
            float _padding0;
        }
    }
}
