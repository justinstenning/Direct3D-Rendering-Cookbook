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
    /*
    /// <summary>
    /// Implements support for swap chain creation from a System.Windows.Form
    /// </summary>
    public abstract class D3DAppSurfaceImageTarget: D3DApplicationBase
    {
        private int width;
        private int height;
        private SurfaceImageSource surfaceImageSource;
        private ISurfaceImageSourceNative surfaceImageSourceNative;

        public D3DAppSurfaceImageTarget(int width, int height, bool isOpaque = false)
        {
            this.width = width;
            this.height = height;
            surfaceImageSource = new SurfaceImageSource(width, height, isOpaque);
        }

        protected override SharpDX.Rectangle CurrentBounds
        {
            get
            {
                return new SharpDX.Rectangle(0, 0, width, height);
            }
        }

        protected virtual SharpDX.DXGI.SwapChainFullScreenDescription CreateFullScreenDescription()
        {
            return new SharpDX.DXGI.SwapChainFullScreenDescription()
            {
                RefreshRate = new SharpDX.DXGI.Rational(60, 1),
                Scaling = SharpDX.DXGI.DisplayModeScaling.Centered,
                Windowed = true
            };
        }

        protected override SharpDX.DXGI.SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 factory, SharpDX.Direct3D11.Device1 device, SharpDX.DXGI.SwapChainDescription1 desc)
        {
            if (_window != null)
            {
                // Creates the swap chain for the CoreWindow
                using (var coreWindow = new ComObject(_window))
                    return new SwapChain1(factory, device, coreWindow, ref desc);
            }
            else if (backgroundPanel != null)
            {
                // Creates the swap chain for XAML composition
                var swapChain = new SwapChain1(factory, device, ref desc);

                // Associate the SwapChainBackgroundPanel with the swap chain
                //panel.Swa
                //panel.SwapChain = swapChain;

            }
        }

        //public bool TrySetDisplayMode(int width, int height, bool fullScreen)
        //{
        //    // Fail attempts when the DisplayModeList has not be initialized
        //    if (DisplayModeList == null)
        //        return false;

        //    // Try to find the first mode that matches the provided dimensions
        //    ModeDescription firstMatch = (from mode in DisplayModeList
        //                                   where mode.Width == width && mode.Height == height
        //                                   select mode).FirstOrDefault();

        //    // If the width > 0 then a matching mode was found
        //    if (firstMatch.Width > 0)
        //    {
        //        if (fullScreen)
        //            SetFullScreen(firstMatch);
        //        else
        //            SetWindowed(firstMatch);
        //        return true;
        //    }

        //    return false;
        //}

        //public void SetWindowed(ModeDescription mode)
        //{
        //    Window.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
        //    Window.ClientSize = new System.Drawing.Size(mode.Width, mode.Height);
        //    SwapChain.IsFullScreen = false;
        //    SwapChain.ResizeTarget(ref mode);
        //}

        //public void SetFullScreen(ModeDescription mode)
        //{
        //    Window.SizeChanged -= Window_SizeChanged;
        //    Window.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        //    Window.ClientSize = new System.Drawing.Size(mode.Width, mode.Height);
        //    Window.SizeChanged += Window_SizeChanged;
            
        //    SizeChanged();

        //    SwapChain.SetFullscreenState(true, null);
        //    //SwapChain.IsFullScreen = true;
        //    SwapChain.ResizeTarget(ref mode);
        //}
    }
     */
}
