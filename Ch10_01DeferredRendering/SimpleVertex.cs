using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using SharpDX;

namespace Ch10_01DeferredRendering
{
    /// <summary>
    /// Vertex input structure: Position, Normal and Color
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SimpleVertex
    {
        public Vector3 Position;

        public SimpleVertex(float pX, float pY, float pZ)
            : this(new Vector3(pX, pY, pZ))
        { }

        public SimpleVertex(Vector3 position)
        {
            Position = position;
        }
    }
}
