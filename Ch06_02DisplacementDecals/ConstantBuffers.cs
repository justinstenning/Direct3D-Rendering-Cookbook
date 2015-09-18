using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Ch06_02DisplacementDecals
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

            // ViewProjection matrix
            public Matrix ViewProjection;

            /// <summary>
            /// Transpose the matrices so that they are in row major order for HLSL
            /// </summary>
            internal void Transpose()
            {
                this.World.Transpose();
                this.WorldInverseTranspose.Transpose();
                this.WorldViewProjection.Transpose();
                this.ViewProjection.Transpose();
            }
        }

        /// <summary>
        /// Directional light
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DirectionalLight
        {
            public SharpDX.Color4 Color;
            public SharpDX.Vector3 Direction;
            float _padding0;
        }

        /// <summary>
        /// Per frame constant buffer (camera position)
        /// </summary>        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerFrame
        {
            public DirectionalLight Light;
            public SharpDX.Vector3 CameraPosition;
            public float TessellationFactor;
        }

        /// <summary>
        /// Per material constant buffer
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerMaterial
        {
            public Color4 Ambient;
            public Color4 Diffuse;
            public Color4 Specular;
            public float SpecularPower;
            public uint HasTexture;    // Does the current material have a texture (0 false, 1 true)
            public uint HasNormalMap;  // Does the current material have a normal map  (0 false, 1 true)
            public float DisplaceScale;// Scale to apply to displacement map (if there is one)
            public Color4 Emissive;
            public Matrix UVTransform; // Support UV coordinate transformations
            public float DisplaceMidLevel; // Mid-level of displacement/height map
            Vector3 _padding0;
        }

        /// <summary>
        /// Per armature/skeleton constant buffer
        /// </summary>
        public class PerArmature
        {
            // The maximum number of bones supported
            public const int MaxBones = 1024;
            public Matrix[] Bones;

            public PerArmature()
            {
                Bones = new Matrix[MaxBones];
            }

            public static int Size()
            {
                return Utilities.SizeOf<Matrix>() * MaxBones;
            }
        }

        /// <summary>
        /// The decal constant buffer
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DecalBuffer
        {
            public float DecalDisplaceScale;
            public Vector3 DecalNormal;    // If normal is 0 then decal not applied
            public Vector3 DecalTangent;
            public float _padding0;
            public Vector3 DecalBitangent;
            public float _padding1;
            public Vector3 DecalPosition;
            public float DecalRadius;
        }
    }
}
