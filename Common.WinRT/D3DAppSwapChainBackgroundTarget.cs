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
    /// Implements support for swap chain creation from a System.Windows.Form
    /// </summary>
    public abstract class D3DAppSwapChainBackgroundTarget : D3DApplicationWinRT
    {
        private SwapChainBackgroundPanel backgroundPanel;
        private ISwapChainBackgroundPanelNative nativeBackgrounPanel;
        public SwapChainBackgroundPanel BackgroundPanel { get { return backgroundPanel; } }

        public D3DAppSwapChainBackgroundTarget(SwapChainBackgroundPanel panel)
        {
            this.backgroundPanel = panel;

            nativeBackgrounPanel = ToDispose(ComObject.As<SharpDX.DXGI.ISwapChainBackgroundPanelNative>(panel));
            this.backgroundPanel.SizeChanged += (sender, args) =>
            {
                SizeChanged();
            };
        }

        public override SharpDX.Rectangle CurrentBounds
        {
            get { return new SharpDX.Rectangle(0, 0, (int)backgroundPanel.RenderSize.Width, (int)backgroundPanel.RenderSize.Height); }
        }

        protected override SharpDX.DXGI.SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 factory, SharpDX.Direct3D11.Device1 device, SharpDX.DXGI.SwapChainDescription1 desc)
        {
            // Creates the swap chain for XAML composition
            var swapChain = new SwapChain1(factory, device, ref desc);

            // Associate the SwapChainBackgroundPanel with the swap chain
            nativeBackgrounPanel.SwapChain = swapChain;

            return swapChain;
        }
    }
}
