using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Ch08_02Particles
{
    /// <summary>
    /// Vertex input structure: Position, Normal and Color
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color Color;
        public Vector2 UV;
        public Common.Mesh.SkinningVertex Skin;
        public Vector4 Tangent;

        /// <summary>
        /// Create vertex with position (normal will be based on position and color will be white)
        /// </summary>
        /// <param name="position">Vertex position</param>
        public Vertex(Vector3 position)
            : this(position, Color.White)
        { }

        /// <summary>
        /// Create vertex with position and color (normal will be based on position)
        /// </summary>
        /// <param name="position">Vertex position</param>
        /// <param name="color">Vertex color</param>
        public Vertex(Vector3 position, Color color)
            : this(position, Vector3.Normalize(position), color)
        { }

        /// <summary>
        /// Create vertex with position from individual components (normal will be calculated and color will be white)
        /// </summary>
        /// <param name="pX">X</param>
        /// <param name="pY">Y</param>
        /// <param name="pZ">Z</param>
        public Vertex(float pX, float pY, float pZ)
            : this(new Vector3(pX, pY, pZ))
        { }

        public Vertex(float pX, float pY, float pZ, float u, float v)
            : this(new Vector3(pX, pY, pZ), new Vector2(u, v))
        {
        }

        public Vertex(float pX, float pY, float pZ, float nX, float nY, float nZ, float u, float v)
            : this(new Vector3(pX, pY, pZ), new Vector3(nX, nY, nZ), Color.White, new Vector2(u, v))
        {
        }

        public Vertex(float pX, float pY, float pZ, float nX, float nY, float nZ, float u, float v, Color color)
            : this(new Vector3(pX, pY, pZ), new Vector3(nX, nY, nZ), color, new Vector2(u, v))
        {
        }


        public Vertex(Vector3 position, Vector2 uv)
            : this(position, Vector3.Normalize(position), Color.White, uv)
        {
        }



        /// <summary>
        /// Create vertex with position and color from individual components (normal will be calculated)
        /// </summary>
        /// <param name="pX">X</param>
        /// <param name="pY">Y</param>
        /// <param name="pZ">Z</param>
        /// <param name="color">color</param>
        public Vertex(float pX, float pY, float pZ, Color color)
            : this(new Vector3(pX, pY, pZ), color)
        { }

        /// <summary>
        /// Create vertex with position, normal and color from individual components
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <param name="pZ"></param>
        /// <param name="nX"></param>
        /// <param name="nY"></param>
        /// <param name="nZ"></param>
        /// <param name="color"></param>
        public Vertex(float pX, float pY, float pZ, float nX, float nY, float nZ, Color color)
            : this(new Vector3(pX, pY, pZ), new Vector3(nX, nY, nZ), color)
        { }

        /// <summary>
        /// Create vertex with position from individual components and normal and color
        /// </summary>
        /// <param name="pX"></param>
        /// <param name="pY"></param>
        /// <param name="pZ"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(float pX, float pY, float pZ, Vector3 normal, SharpDX.Color color)
            : this(new Vector3(pX, pY, pZ), normal, color)
        { }

        /// <summary>
        /// Create vertex with position, normal and color - UV and Skin will be 0
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(Vector3 position, Vector3 normal, Color color)
            : this(position, normal, color, Vector2.Zero, new Common.Mesh.SkinningVertex(), Vector4.Zero)
        { }

        /// <summary>
        /// Create vertex with position, normal, color and uv coordinates
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        /// <param name="uv"></param>
        public Vertex(Vector3 position, Vector3 normal, Color color, Vector2 uv)
            : this(position, normal, color, uv, new Common.Mesh.SkinningVertex(), Vector4.Zero)
        { }

        /// <summary>
        /// Create vertex with position, normal, color, uv coordinates, and skinning
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(Vector3 position, Vector3 normal, Color color, Vector2 uv, Common.Mesh.SkinningVertex skin, Vector4 tangent)
        {
            Position = position;
            Normal = normal;
            Color = color;
            UV = uv;
            Skin = skin;
            Tangent = tangent;
        }
    }
}
