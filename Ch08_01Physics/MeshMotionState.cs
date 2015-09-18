using BulletSharp;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ch08_01Physics
{
    public class MeshMotionState: MotionState
    {
        public MeshRenderer Mesh { get; private set; }
        public MeshMotionState(MeshRenderer mesh)
        {
            Mesh = mesh;
        }

        public override SharpDX.Matrix WorldTransform
        {
            get
            {
                return Mesh.World *  Matrix.Translation(Mesh.Mesh.Extent.Center);
            }
            set
            {
                Mesh.World = Matrix.Translation(-Mesh.Mesh.Extent.Center) * value;
            }
        }
    }
}
