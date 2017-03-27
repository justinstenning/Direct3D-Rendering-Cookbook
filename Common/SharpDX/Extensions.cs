using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDX
{
    /// <summary>
    /// Extension methods that help make the SharpDX 2.5.1 code more compatible with 3.1.1
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Examples:
        /// var viewports = context.Rasterizer.GetViewports();
        /// context.Rasterizer.SetViewports(this.DeviceManager
        ///    .Direct3DContext.Rasterizer.GetViewports());
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        public static Mathematics.Interop.RawViewportF[] GetViewports(this RasterizerStage rs)
        {
            return rs.GetViewports<Mathematics.Interop.RawViewportF>();
        }

        public static int ReadInt(this DataStream ds)
        {
            return ds.Read<int>();
        }
    }
}
