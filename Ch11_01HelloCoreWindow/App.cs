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
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Core;

namespace Ch11_01HelloCoreWindow
{
    internal static class App
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        [MTAThread]
        private static void Main()
        {
            var viewFactory = new D3DAppViewProviderFactory();
            CoreApplication.Run(viewFactory);
        }

        class D3DAppViewProviderFactory : IFrameworkViewSource
        {
            public IFrameworkView CreateView()
            {
                return new D3DAppViewProvider();
            }
        }

        /// <summary>
        /// D3D App IFrameworkView
        /// http://msdn.microsoft.com/en-us/windows/apps/windows.applicationmodel.core.iframeworkview
        /// </summary>
        class D3DAppViewProvider : SharpDX.Component, IFrameworkView
        {
            bool windowClosed = false;
            Windows.UI.Core.CoreWindow window;
            D3DApp d3dApp;

            #region IFrameworkView members
            public void Initialize(CoreApplicationView applicationView)
            {
            }

            public void Load(string entryPoint)
            {
            }

            public void SetWindow(Windows.UI.Core.CoreWindow window)
            {
                RemoveAndDispose(ref d3dApp);
                this.window = window;
                d3dApp = ToDispose(new D3DApp(window));
                d3dApp.Initialize();
            }

            public void Uninitialize()
            {
            }

            public void Run()
            {
                // Specify the cursor type as the standard arrow cursor.
                window.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

                // Activate the application window, making it visible and enabling it to receive events.
                window.Activate();

                // Set the DPI and handle changes
                d3dApp.DeviceManager.Dpi = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
                Windows.Graphics.Display.DisplayInformation.GetForCurrentView().DpiChanged += (sender, args) =>
                {
                    d3dApp.DeviceManager.Dpi = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
                };

                // Starting camera position
                d3dApp.Camera.Position = new SharpDX.Vector3(1, 1, 2);
                d3dApp.Camera.LookAtDir = -d3dApp.Camera.Position;

                // Enter the render loop. Note that Windows Store apps should never exit.
                while (true)
                {
                    if (window.Visible)
                    {
                        // Process events incoming to the window.
                        window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

                        // Render frame
                        d3dApp.Render();
                    }
                    else
                    {
                        window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessOneAndAllPending);
                    }
                }
            }

            #endregion
        }
    }
}
