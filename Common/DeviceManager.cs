// Copyright (c) 2013 Justin Stenning
// Adapted from original code by Alexandre Mutel
// 
//----------------------------------------------------------------------------
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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

using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Common
{
    public class DeviceManager: SharpDX.Component
    {
        // Direct3D Objects
        protected SharpDX.Direct3D11.Device1 d3dDevice;
        protected SharpDX.Direct3D11.DeviceContext1 d3dContext;
        protected float dpi;

        // Declare Direct2D Objects
        protected SharpDX.Direct2D1.Factory1 d2dFactory;
        protected SharpDX.Direct2D1.Device d2dDevice;
        protected SharpDX.Direct2D1.DeviceContext d2dContext;

        // Declare DirectWrite & Windows Imaging Component Objects
        protected SharpDX.DirectWrite.Factory dwriteFactory;
        protected SharpDX.WIC.ImagingFactory2 wicFactory;

        /// <summary>
        /// The list of feature levels to accept
        /// </summary>
        public FeatureLevel[] Direct3DFeatureLevels = new FeatureLevel[] {
            FeatureLevel.Level_11_1, 
            FeatureLevel.Level_11_0,
            //FeatureLevel.Level_10_1,
            //FeatureLevel.Level_10_0
        };

        /// <summary>
        /// Gets the Direct3D11 device.
        /// </summary>
        public SharpDX.Direct3D11.Device1 Direct3DDevice { get { return d3dDevice; } }

        /// <summary>
        /// Gets the Direct3D11 immediate context.
        /// </summary>
        public SharpDX.Direct3D11.DeviceContext1 Direct3DContext { get { return d3dContext; } }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public SharpDX.Direct2D1.Factory1 Direct2DFactory { get { return d2dFactory; } }

        /// <summary>
        /// Gets the Direct2D device.
        /// </summary>
        public SharpDX.Direct2D1.Device Direct2DDevice { get { return d2dDevice; } }

        /// <summary>
        /// Gets the Direct2D context.
        /// </summary>
        public SharpDX.Direct2D1.DeviceContext Direct2DContext { get { return d2dContext; } }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public SharpDX.DirectWrite.Factory DirectWriteFactory { get { return dwriteFactory; } }

        /// <summary>
        /// Gets the WIC factory.
        /// </summary>
        public SharpDX.WIC.ImagingFactory2 WICFactory { get { return wicFactory; } }

        /// <summary>
        /// Gets or sets the DPI.
        /// </summary>
        /// <remarks>
        /// This method will fire the event <see cref="OnDpiChanged"/>
        /// if the dpi is modified.
        /// </remarks>
        public virtual float Dpi
        {
            get { return dpi; }
            set
            {
                if (dpi != value)
                {
                    dpi = value;
                    d2dContext.DotsPerInch = new SharpDX.Size2F(dpi, dpi);

                    if (OnDpiChanged != null)
                        OnDpiChanged(this);
                }
            }
        }

        /// <summary>
        /// This event is fired when the DeviceManager is initialized by the <see cref="Initialize"/> method.
        /// </summary>
        public event Action<DeviceManager> OnInitialize;
        
        /// <summary>
        /// This event is fired when the <see cref="Dpi"/> is called,
        /// </summary>
        public event Action<DeviceManager> OnDpiChanged;

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="window">Window to receive the rendering</param>
        public virtual void Initialize(float dpi = 96.0f)
        {
            CreateInstances();

            if (OnInitialize != null)
                OnInitialize(this);

            Dpi = dpi;
        }

        /// <summary>
        /// Creates device manager objects
        /// </summary>
        /// <remarks>
        /// This method is called at the initialization of this instance.
        /// </remarks>
        protected virtual void CreateInstances()
        {
            // Dispose previous references and set to null
            RemoveAndDispose(ref d3dDevice);
            RemoveAndDispose(ref d3dContext);
            RemoveAndDispose(ref d2dDevice);
            RemoveAndDispose(ref d2dContext);
            RemoveAndDispose(ref d2dFactory);
            RemoveAndDispose(ref dwriteFactory);
            RemoveAndDispose(ref wicFactory);

            #region Create Direct3D 11.1 device and retrieve device context

            // Bgra performs better especially with Direct2D software
            // render targets
            var creationFlags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            // Enable D3D device debug layer
            creationFlags |= DeviceCreationFlags.Debug;
#endif

            // Retrieve the Direct3D 11.1 device and device context
            using (var device = new Device(DriverType.Hardware, creationFlags, Direct3DFeatureLevels))
            {
                d3dDevice = ToDispose(device.QueryInterface<Device1>());
            }

            // Get Direct3D 11.1 context
            d3dContext = ToDispose(d3dDevice.ImmediateContext.QueryInterface<DeviceContext1>());
            #endregion

            #region Create Direct2D device and context

#if DEBUG
            var debugLevel = SharpDX.Direct2D1.DebugLevel.Information;
#else
            var debugLevel = SharpDX.Direct2D1.DebugLevel.None;
#endif

            // Allocate new references
            d2dFactory = ToDispose(new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debugLevel));
            dwriteFactory = ToDispose(new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared));
            wicFactory = ToDispose(new SharpDX.WIC.ImagingFactory2());

            // Create Direct2D device
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
            {
                d2dDevice = ToDispose(new SharpDX.Direct2D1.Device(d2dFactory, dxgiDevice));
            }

            // Create Direct2D context
            d2dContext = ToDispose(new SharpDX.Direct2D1.DeviceContext(d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None));
            #endregion
        }
    }
}
