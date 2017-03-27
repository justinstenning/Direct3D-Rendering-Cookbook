using SharpDX;
using System.Linq;

namespace Ch08_01Physics
{
    public static class BulletExtensions
    {
        public static Matrix ToSharpDXMatrix(this BulletSharp.Math.Matrix m)
        {
            return new Matrix(m.ToArray());
        }

        public static BulletSharp.Math.Matrix ToBulletMatrix(this Matrix m)
        {
            return new BulletSharp.Math.Matrix(m.ToArray());
        }

        public static Vector3 ToSharpDXVector3(this BulletSharp.Math.Vector3 v)
        {
            return new Vector3(v.ToArray());
        }

        public static BulletSharp.Math.Vector3 ToBulletVector3(this Vector3 v)
        {
            return new BulletSharp.Math.Vector3(v.ToArray());
        }

        public static BulletSharp.Math.Vector3[] ToBulletVector3Array(this Vector3[] varray)
        {
            return varray.Select(v => v.ToBulletVector3()).ToArray();
        }
    }
}
