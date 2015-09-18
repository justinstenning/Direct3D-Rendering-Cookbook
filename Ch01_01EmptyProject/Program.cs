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
using System.Threading.Tasks;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Device = SharpDX.Direct3D11.Device;

namespace Ch01_01EmptyProject
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Direct3D Initialization
            // Create the window to render to
            Form1 form = new Form1();
            form.Text = "D3DRendering - EmptyProject";
            form.Width = 640;
            form.Height = 480;

            // Create the device and swapchain
            Device device;
            SwapChain swapChain;

            Device.CreateWithSwapChain(
                SharpDX.Direct3D.DriverType.Hardware,
                DeviceCreationFlags.None,
                new [] {
                    SharpDX.Direct3D.FeatureLevel.Level_11_1,
                    SharpDX.Direct3D.FeatureLevel.Level_11_0,
                    SharpDX.Direct3D.FeatureLevel.Level_10_1,
                    SharpDX.Direct3D.FeatureLevel.Level_10_0,
                },
                new SwapChainDescription()
                {
                    ModeDescription =
                        new ModeDescription(
                            form.ClientSize.Width,
                            form.ClientSize.Height,
                            new Rational(60, 1),
                            Format.R8G8B8A8_UNorm
                        ),
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = SharpDX.DXGI.Usage.BackBuffer | Usage.RenderTargetOutput,
                    BufferCount = 1,
                    Flags = SwapChainFlags.None,
                    IsWindowed = true,
                    OutputHandle = form.Handle,
                    SwapEffect = SwapEffect.Discard,
                },
                out device, out swapChain
            );

            // Create references to backBuffer and renderTargetView
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderTargetView = new RenderTargetView(device, backBuffer);

            #endregion

            #region Render loop

            // Create Clock and FPS counters
            var clock = new System.Diagnostics.Stopwatch();
            var clockFrequency = (double)System.Diagnostics.Stopwatch.Frequency;
            clock.Start();
            var deltaTime = 0.0;
            var fpsTimer = new System.Diagnostics.Stopwatch();
            fpsTimer.Start();
            var fps = 0.0;
            int fpsFrames = 0;

            // Create and run the render loop
            RenderLoop.Run(form, () =>
            {
                // Time in seconds
                var totalSeconds = clock.ElapsedTicks / clockFrequency;

                #region FPS and title update
                fpsFrames++;
                if (fpsTimer.ElapsedMilliseconds > 1000)
                {
                    fps = 1000.0 * fpsFrames / fpsTimer.ElapsedMilliseconds;

                    // Update window title with FPS once every second
                    form.Text = string.Format("D3DRendering - FPS: {0:F2} ({1:F2}ms/frame)", fps, (float)fpsTimer.ElapsedMilliseconds / fpsFrames);

                    // Restart the FPS counter
                    fpsTimer.Reset();
                    fpsTimer.Start();
                    fpsFrames = 0;
                }
                #endregion

                // Execute rendering commands here...
                device.ImmediateContext.ClearRenderTargetView(
                    renderTargetView, 
                    Color.LightBlue);

                // Present the frame
                swapChain.Present(0, PresentFlags.None);

                // Determine the time it took to render the frame
                deltaTime = (clock.ElapsedTicks / clockFrequency) - totalSeconds;
            });
            #endregion

            #region Direct3D Cleanup

            // Release the device and any other resources created
            renderTargetView.Dispose();
            backBuffer.Dispose();
            device.Dispose();
            swapChain.Dispose();

            #endregion
        }
    }
}
