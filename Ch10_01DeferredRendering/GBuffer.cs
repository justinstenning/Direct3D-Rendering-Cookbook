using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Common;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Ch10_01DeferredRendering
{
    public class GBuffer: RendererBase
    {
        public List<Texture2D> RTs = new List<Texture2D>();
        public List<ShaderResourceView> SRVs = new List<ShaderResourceView>();
        public List<RenderTargetView> RTVs = new List<RenderTargetView>();

        public Texture2D DS0;
        public ShaderResourceView DSSRV; // Depth stencil
        public DepthStencilView DSV;

        int width;
        int height;

        SampleDescription sampleDescription;
        SharpDX.DXGI.Format[] RTFormats;

        public GBuffer(int width, int height, SampleDescription sampleDesc, params SharpDX.DXGI.Format[] targetFormats)
        {
            System.Diagnostics.Debug.Assert(targetFormats != null && targetFormats.Length > 0 && targetFormats.Length < 9, "Between 1 and 8 target formats must be provided");
            this.width = width;
            this.height = height;
            this.sampleDescription = sampleDesc;

            RTFormats = targetFormats;
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref DSSRV);
            RemoveAndDispose(ref DSV);
            RemoveAndDispose(ref DS0);

            RTs.ForEach(rt => RemoveAndDispose(ref rt));
            SRVs.ForEach(srv => RemoveAndDispose(ref srv));
            RTVs.ForEach(rtv => RemoveAndDispose(ref rtv));
            RTs.Clear();
            SRVs.Clear();
            RTVs.Clear();

            var device = DeviceManager.Direct3DDevice;

            bool isMSAA = sampleDescription.Count > 1;

            // Render Target texture description
            var texDesc = new Texture2DDescription();
            texDesc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
            texDesc.ArraySize = 1;
            texDesc.CpuAccessFlags = CpuAccessFlags.None;
            texDesc.Usage = ResourceUsage.Default;
            texDesc.Width = width;
            texDesc.Height = height;
            texDesc.MipLevels = 1; // No mip levels
            texDesc.SampleDescription = sampleDescription;

            // Render Target View description
            var rtvDesc = new RenderTargetViewDescription();
            rtvDesc.Dimension = isMSAA ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
            rtvDesc.Texture2D.MipSlice = 0;

            // SRV description for render targets
            var srvDesc = new ShaderResourceViewDescription();
            srvDesc.Dimension = isMSAA ? SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled : SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D;
            srvDesc.Texture2D.MipLevels = -1;
            srvDesc.Texture2D.MostDetailedMip = 0;

            // Create Render Targets (with SRV and RTV)
            foreach (var format in RTFormats)
            {
                texDesc.Format = format;
                srvDesc.Format = format;
                rtvDesc.Format = format;
                
                RTs.Add(ToDispose(new Texture2D(device, texDesc)));
                SRVs.Add(ToDispose(new ShaderResourceView(device, RTs.Last(), srvDesc)));
                RTVs.Add(ToDispose(new RenderTargetView(device, RTs.Last(), rtvDesc)));

                RTs.Last().DebugName = String.Format("RT{0}_{1}", RTs.Count, format);
                SRVs.Last().DebugName = String.Format("SRV{0}_{1}", RTs.Count, format);
                RTVs.Last().DebugName = String.Format("RTV{0}_{1}", RTs.Count, format);
            }

            // Create Depth/Stencil
            texDesc.BindFlags = BindFlags.ShaderResource | BindFlags.DepthStencil;
            texDesc.Format = SharpDX.DXGI.Format.R32G8X24_Typeless; // typeless so we can use as shader resource
            DS0 = ToDispose(new Texture2D(device, texDesc));
            DS0.DebugName = "DS0";

            srvDesc.Format = SharpDX.DXGI.Format.R32_Float_X8X24_Typeless;
            DSSRV = ToDispose(new ShaderResourceView(device, DS0, srvDesc));
            DSSRV.DebugName = "DSSRV-DepthStencil";

            // Depth Stencil View
            var dsvDesc = new DepthStencilViewDescription();
            dsvDesc.Flags = DepthStencilViewFlags.None;
            dsvDesc.Dimension = isMSAA ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D;
            dsvDesc.Format = SharpDX.DXGI.Format.D32_Float_S8X24_UInt;
            dsvDesc.Texture2D.MipSlice = 0;

            DSV = ToDispose(new DepthStencilView(device, DS0, dsvDesc));
            DSV.DebugName = "DSV0";
        }

        /// <summary>
        /// Bind the render targets to the OutputMerger
        /// </summary>
        /// <param name="context"></param>
        public void Bind(DeviceContext1 context)
        {
            // The empty UnorderedAccessView array is necessary, passing null results in an error
            context.OutputMerger.SetTargets(DSV, 0, new UnorderedAccessView [0], RTVs.ToArray());
        }

        /// <summary>
        /// Unbind the render targets
        /// </summary>
        /// <param name="context"></param>
        public void Unbind(DeviceContext1 context)
        {
            context.OutputMerger.ResetTargets();
        }

        /// <summary>
        /// Clear the render targets and depth stencil
        /// </summary>
        /// <param name="context"></param>
        /// <param name="background"></param>
        public void Clear(DeviceContext1 context, Color background)
        {
            context.ClearDepthStencilView(DSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            foreach (var rtv in RTVs)
                context.ClearRenderTargetView(rtv, background);
        }

        /// <summary>
        /// Save all render targets to .dds files (uses the render target DebugName for filename)
        /// </summary>
        public void SaveToFiles()
        {
            foreach (var rt in RTs)
            {
                CopyTexture.SaveToFile(DeviceManager, rt, rt.DebugName + ".dds");
            }
        }

        protected override void DoRender()
        {
            throw new NotImplementedException();
        }
    }
}
