using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Ch03_01CubeAndSphere
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
        /// Create vertex with position, normal and color
        /// </summary>
        /// <param name="position"></param>
        /// <param name="normal"></param>
        /// <param name="color"></param>
        public Vertex(Vector3 position, Vector3 normal, Color color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }
}