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

        public override void GetWorldTransform(out BulletSharp.Math.Matrix worldTrans)
        {

            worldTrans = (Mesh.World * Matrix.Translation(Mesh.Mesh.Extent.Center)).ToBulletMatrix();
        }

        public override void SetWorldTransform(ref BulletSharp.Math.Matrix worldTrans)
        {
            Mesh.World = (Matrix.Translation(-Mesh.Mesh.Extent.Center).ToBulletMatrix() * worldTrans).ToSharpDXMatrix();
        }
    }
}
