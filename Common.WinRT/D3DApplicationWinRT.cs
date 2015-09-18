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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common
{
    /// <summary>
    /// Implements support for swap chain creation for a Windows Store app
    /// </summary>
    public abstract class D3DApplicationWinRT: D3DApplicationBase
    {
        public D3DApplicationWinRT()
            : base()
        {
            Windows.ApplicationModel.Core.CoreApplication.Suspending += OnSuspending;
        }

        private void OnSuspending(Object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // When suspending hint that resources can be temporarily reclaimed
            using (SharpDX.DXGI.Device3 dxgiDevice = DeviceManager.Direct3DDevice.QueryInterface<SharpDX.DXGI.Device3>())
            {
                dxgiDevice.Trim();
            }
        }

        protected override SharpDX.DXGI.SwapChainDescription1 CreateSwapChainDescription()
        {
            // Using SwapChain for Composition requires FlipSequential in SwapEffect
            // which in turn requires between 2 adn 16 in BufferCount
            // http://msdn.microsoft.com/en-us/library/windows/desktop/hh404528(v=vs.85).aspx
            // About FlipSequential:
            // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706346(v=vs.85).aspx

            var desc = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = Width,
                Height = Height,
                // B8G8R8A8_UNorm gives us better performance 
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = SharpDX.DXGI.Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                Scaling = SharpDX.DXGI.Scaling.None,
                BufferCount = 2,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            return desc;
        }

        public abstract void Render();

        #pragma warning disable 809
        [Obsolete("Use the Render method for WinRT", true)] // CS0809
        public override void Run()
        { }
    }
}
