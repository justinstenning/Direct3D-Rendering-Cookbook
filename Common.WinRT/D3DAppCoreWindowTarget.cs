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
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Media.Imaging;

namespace Common
{
    /// <summary>
    /// Implements support for swap chain creation from a System.Windows.Form
    /// </summary>
    public abstract class D3DAppCoreWindowTarget : D3DApplicationWinRT
    {
        private CoreWindow _window;
        public CoreWindow Window { get { return _window; } }

        public D3DAppCoreWindowTarget(CoreWindow window)
        {
            _window = window;
            
            Window.SizeChanged += (sender, args) =>
            {
                SizeChanged();
            };
        }
        
        public override SharpDX.Rectangle CurrentBounds
        {
            get
            {
                return new SharpDX.Rectangle((int)_window.Bounds.X, (int)_window.Bounds.Y, (int)_window.Bounds.Width, (int)_window.Bounds.Height);
            }
        }

        //public override int Width
        //{
        //    get
        //    {
        //        return 0; // use size of CoreWindow 
        //    }
        //}

        //public override int Height
        //{
        //    get
        //    {
        //        return 0; // use size of CoreWindow 
        //    }
        //}

        protected override SharpDX.DXGI.SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 factory, SharpDX.Direct3D11.Device1 device, SharpDX.DXGI.SwapChainDescription1 desc)
        {
            // Creates the swap chain for the CoreWindow
            using (var coreWindow = new ComObject(_window))
                return new SwapChain1(factory, device, coreWindow, ref desc);
        }
    }
}
