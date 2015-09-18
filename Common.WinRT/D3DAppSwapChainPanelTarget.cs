// Copyright (c) 2013 Justin Stenning
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Common
{
    /// <summary>
    /// Implements support for swap chain creation from a SwapChainPanel
    /// </summary>
    public abstract class D3DAppSwapChainPanelTarget : D3DApplicationWinRT
    {
        private SwapChainPanel panel;
        private ISwapChainPanelNative nativePanel;
        public SwapChainPanel SwapChainPanel { get { return panel; } }

        public D3DAppSwapChainPanelTarget(SwapChainPanel panel)
        {
            this.panel = panel;
            nativePanel = ToDispose(ComObject.As<SharpDX.DXGI.ISwapChainPanelNative>(panel));

            this.panel.CompositionScaleChanged += (sender, args) =>
            {
                //SizeChanged();
                ScaleChanged();
            };
            this.panel.SizeChanged += (sender, args) =>
            {
                SizeChanged();
            };
        }

        /// <summary>
        /// Event fired when size of the underlying render control is changed
        /// </summary>
        public event Action<D3DAppSwapChainPanelTarget> OnScaleChanged;

        protected void ScaleChanged()
        {
            // Update the DPI
            DeviceManager.Dpi = 96.0f * panel.CompositionScaleX;
            
            base.CreateSizeDependentResources(this);
            
            // Retrieve SwapChain2 reference and apply appropriate 2D scaling
            using (var swapChain2 = this.SwapChain.QueryInterface<SwapChain2>())
            {
                // 2D affine transform matrix
                Matrix3x2 inverseScale = new Matrix3x2();
                inverseScale.M11 = 1.0f / panel.CompositionScaleX;
                inverseScale.M22 = 1.0f / panel.CompositionScaleY;
                swapChain2.MatrixTransform = inverseScale;
            }

            if (OnScaleChanged != null)
                OnScaleChanged(this);
        }

        public override SharpDX.Rectangle CurrentBounds
        {
            get { return new SharpDX.Rectangle(0, 0, (int)(panel.RenderSize.Width), (int)(panel.RenderSize.Height)); }
        }

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            base.CreateSizeDependentResources(app);
        }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            var desc = base.CreateSwapChainDescription();

            // SwapChainPanel requires Stretch scaling
            // http://msdn.microsoft.com/en-us/library/windows/desktop/hh825871.aspx
            desc.Scaling = Scaling.Stretch;
            return desc;
        }

        protected override SharpDX.DXGI.SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 factory, SharpDX.Direct3D11.Device1 device, SharpDX.DXGI.SwapChainDescription1 desc)
        {
            // Create the swap chain for XAML composition
            var swapChain = new SwapChain1(factory, device, ref desc);
            // Attach swap chain to SwapChainPanel
            nativePanel.SwapChain = swapChain;
            return swapChain;
        }
    }
}
