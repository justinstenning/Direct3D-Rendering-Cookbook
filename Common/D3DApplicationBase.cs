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
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Device1 = SharpDX.Direct3D11.Device1;
using Buffer = SharpDX.Direct3D11.Buffer;


namespace Common
{
    /// <summary>
    /// </summary>
    public abstract class D3DApplicationBase: Component
    {
        DeviceManager _deviceManager;
        SharpDX.DXGI.SwapChain1 _swapChain;

        SharpDX.Direct3D11.RenderTargetView _renderTargetView;
        SharpDX.Direct3D11.DepthStencilView _depthStencilView;
        SharpDX.Direct3D11.Texture2D _backBuffer;
        SharpDX.Direct3D11.Texture2D _depthBuffer;

        SharpDX.Direct2D1.Bitmap1 _bitmapTarget;

//#if NETFX_CORE
//        /// <summary>
//        /// Gets the bounds of the control linked to this render target
//        /// </summary>
//        public Windows.Foundation.Rect Bounds { get; protected set; }

//        /// <summary>
//        /// Gets the bounds of the control linked to this render target
//        /// </summary>
//        public abstract Windows.Foundation.Rect CurrentBounds { get; }
//#else
        /// <summary>
        /// Gets the configured bounds of the control used to render to
        /// </summary>
        public SharpDX.Rectangle Bounds { get; protected set; }

        /// <summary>
        /// Gets the current bounds of the control used to render to
        /// </summary>
        public abstract SharpDX.Rectangle CurrentBounds { get; }
//#endif
        /// <summary>
        /// Gets the <see cref="DeviceManager"/> attached to this instance.
        /// </summary>
        public DeviceManager DeviceManager { get { return _deviceManager; } }

        /// <summary>
        /// Gets the <see cref="SharpDX.DXGI.SwapChain1"/> created by this
        /// instance.
        /// </summary>
        public SwapChain1 SwapChain
        {
            get { return _swapChain; }
        }

        /// <summary>
        /// Provides access to the list of available display modes.
        /// </summary>
        public ModeDescription[] DisplayModeList { get; private set; }

        /// <summary>
        /// Gets or sets whether the swap chain will wait for the 
        /// next vertical sync before presenting.
        /// </summary>
        /// <remarks>
        /// Changes the behavior of the <see cref="D3DApplicationBase.Present"/> method.
        /// </remarks>
        public bool VSync { get; set; }

        /// <summary>
        /// Width of the swap chain buffers.
        /// </summary>
        public virtual int Width
        {
            get
            {
                return (int)(Bounds.Width * DeviceManager.Dpi / 96.0);
            }
        }

        /// <summary>
        /// Height of the swap chain buffers.
        /// </summary>
        public virtual int Height
        {
            get
            {
                return (int)(Bounds.Height * DeviceManager.Dpi / 96.0);
            }
        }


        protected ViewportF Viewport { get; set; }
        protected SharpDX.Rectangle RenderTargetBounds { get; set; }
        protected SharpDX.Size2 RenderTargetSize { get { return new SharpDX.Size2(RenderTargetBounds.Width, RenderTargetBounds.Height); } }

        /// <summary>
        /// Gets the Direct3D RenderTargetView used by this app.
        /// </summary>
        public SharpDX.Direct3D11.RenderTargetView RenderTargetView
        {
            get { return _renderTargetView; }
            protected set
            {
                if (_renderTargetView != value)
                {
                    RemoveAndDispose(ref _renderTargetView);
                    _renderTargetView = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the BackBuffer used by this app.
        /// </summary>
        public SharpDX.Direct3D11.Texture2D BackBuffer
        {
            get { return _backBuffer; }
            protected set
            {
                if (_backBuffer != value)
                {
                    RemoveAndDispose(ref _backBuffer);
                    _backBuffer = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the depthBuffer used by this app.
        /// </summary>
        public SharpDX.Direct3D11.Texture2D DepthBuffer
        {
            get { return _depthBuffer; }
            protected set
            {
                if (_depthBuffer != value)
                {
                    RemoveAndDispose(ref _depthBuffer);
                    _depthBuffer = value;
                }
            }
        }

        /// <summary>
        /// Gets the Direct3D DepthStencilView used by this app.
        /// </summary>
        public SharpDX.Direct3D11.DepthStencilView DepthStencilView
        {
            get { return _depthStencilView; }
            protected set
            {
                if (_depthStencilView != value)
                {
                    RemoveAndDispose(ref _depthStencilView);
                    _depthStencilView = value;
                }
            }
        }

        /// <summary>
        /// Gets the Direct2D RenderTarget used by this app.
        /// </summary>
        public SharpDX.Direct2D1.Bitmap1 BitmapTarget2D
        {
            get { return _bitmapTarget; }
            protected set
            {
                if (_bitmapTarget != value)
                {
                    RemoveAndDispose(ref _bitmapTarget);
                    _bitmapTarget = value;
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public D3DApplicationBase()
        {
            // Create our device manager instance.
            // This encapsulates the creation of Direct3D and Direct2D devices
            _deviceManager = ToDispose(new DeviceManager());

            // If the device needs to be reinitialized, make sure we
            // are able to recreate our device dependent resources.
            DeviceManager.OnInitialize += CreateDeviceDependentResources;

            // If the size changes, make sure we reinitialize
            // any size dependent resources.
            this.OnSizeChanged += CreateSizeDependentResources;
        }
        
        /// <summary>
        /// Initialize the attached DeviceManager and trigger an initial
        /// OnSizeChanged event.
        /// </summary>
        public virtual void Initialize()
        {
            // Initialize the device manager
            DeviceManager.Initialize();

            // We trigger an initial size change to ensure all
            // render buffers and size dependent resources have the
            // correct dimensions.
            SizeChanged();
        }

        /// <summary>
        /// Trigger the OnSizeChanged event if the width and height
        /// of the <see cref="CurrentBounds"/> differs to the
        /// last call to SizeChanged.
        /// </summary>
        protected void SizeChanged(bool force = false)
        {
            var newBounds = CurrentBounds;

            // Ignore invalid sizes, either both are zero, or both are not zero
            if ((newBounds.Width == 0 && newBounds.Height != 0) ||
                (newBounds.Width != 0 && newBounds.Height == 0))
                return;

            if (newBounds.Width != Bounds.Width ||
                newBounds.Height != Bounds.Height || force)
            {
                // Store the bounds so the next time we get a SizeChanged event we can
                // avoid rebuilding everything if the size is identical.
                Bounds = newBounds;

                if (OnSizeChanged != null)
                    OnSizeChanged(this);
            }
        }

        /// <summary>
        /// Create device dependent resources
        /// </summary>
        /// <param name="deviceManager"></param>
        protected virtual void CreateDeviceDependentResources(DeviceManager deviceManager)
        {
            if (_swapChain != null)
            {
                // Release the swap chain
                RemoveAndDispose(ref _swapChain);

                // Force reinitialize size dependent resources
                SizeChanged(true);
            }
        }

        /// <summary>
        /// Create size dependent resources, in this case the swap chain and render targets
        /// </summary>
        /// <param name="app"></param>
        protected virtual void CreateSizeDependentResources(D3DApplicationBase app)
        {
            // Retrieve references to device and context
            var device = DeviceManager.Direct3DDevice;
            var context = DeviceManager.Direct3DContext;
            // Retrieve Direct2D context (for use with text rendering etc)
            var d2dContext = DeviceManager.Direct2DContext;

            // Before the swapchain can resize all the buffers must be released
            RemoveAndDispose(ref _backBuffer);
            RemoveAndDispose(ref _renderTargetView);
            RemoveAndDispose(ref _depthStencilView);
            RemoveAndDispose(ref _depthBuffer);
            RemoveAndDispose(ref _bitmapTarget);
            d2dContext.Target = null;

            #region Initialize Direct3D swap chain and render target

            // If the swap chain already exists, resize it.
            if (_swapChain != null)
            {
                _swapChain.ResizeBuffers(
                    _swapChain.Description1.BufferCount, 
                    Width, 
                    Height, 
                    _swapChain.Description.ModeDescription.Format,
                    _swapChain.Description.Flags);
            }
            // Otherwise, create a new one.
            else
            {
                // SwapChain description
                var desc = CreateSwapChainDescription();

                // Rather than create a new DXGI Factory we should reuse
                // the one that has been used internally to create the device

                // First, retrieve the underlying DXGI Device from the D3D Device.
                // access the adapter used for that device and then create the swap chain 
                using (var dxgiDevice2 = device.QueryInterface<SharpDX.DXGI.Device2>())
                using (var dxgiAdapter = dxgiDevice2.Adapter)
                using (var dxgiFactory2 = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>())
                using (var output = dxgiAdapter.Outputs.First())
                {
                    // The CreateSwapChain method is used so we can descend
                    // from this class and implement a swapchain for a desktop
                    // or a Windows 8 AppStore app
                    _swapChain = ToDispose(CreateSwapChain(dxgiFactory2, device, desc));

#if !NETFX_CORE
                    // Retrieve the list of supported display modes
                    DisplayModeList = output.GetDisplayModeList(desc.Format, DisplayModeEnumerationFlags.Scaling);
#endif
                }
            }

            // Obtain the backbuffer for this window which will be the final 3D rendertarget.
            BackBuffer = ToDispose(Texture2D.FromSwapChain<Texture2D>(_swapChain, 0));
            // Create a view interface on the rendertarget to use on bind.
            RenderTargetView = ToDispose(new RenderTargetView(device, BackBuffer));

            // Cache the rendertarget dimensions in our helper class for convenient use.
            var backBufferDesc = BackBuffer.Description;
            RenderTargetBounds = new SharpDX.Rectangle(0, 0, backBufferDesc.Width, backBufferDesc.Height);

            // Create a viewport descriptor of the render size.
            this.Viewport = new SharpDX.ViewportF(
                (float)RenderTargetBounds.X,
                (float)RenderTargetBounds.Y,
                (float)RenderTargetBounds.Width,
                (float)RenderTargetBounds.Height,
                0.0f,   // min depth
                1.0f);  // max depth

            // Set the current viewport for the rasterizer.
            context.Rasterizer.SetViewport(Viewport);

            // Create a descriptor for the depth/stencil buffer.
            // Allocate a 2-D texture as the depth/stencil buffer.
            // Create a DSV to use on bind.
            this.DepthBuffer = ToDispose(new Texture2D(device, new Texture2DDescription()
                {
                    Format = SharpDX.DXGI.Format.D32_Float_S8X24_UInt,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = RenderTargetSize.Width,
                    Height = RenderTargetSize.Height,
                    SampleDescription = SwapChain.Description.SampleDescription,
                    BindFlags = BindFlags.DepthStencil,
                }));
            this.DepthStencilView = ToDispose(
                new DepthStencilView(
                    device, 
                    DepthBuffer, 
                    new DepthStencilViewDescription() 
                    { 
                        Dimension = (SwapChain.Description.SampleDescription.Count > 1 || SwapChain.Description.SampleDescription.Quality > 0) ? DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D
                    }));
            
            // Set the OutputMerger targets
            context.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);

            #endregion

            #region Initialize Direct2D render target

            // Now we set up the Direct2D render target bitmap linked to the swapchain. 
            // Whenever we render to this bitmap, it will be directly rendered to the 
            // swapchain associated with the window.
            var bitmapProperties = new SharpDX.Direct2D1.BitmapProperties1(
                new SharpDX.Direct2D1.PixelFormat(_swapChain.Description.ModeDescription.Format, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                DeviceManager.Dpi,
                DeviceManager.Dpi,
                SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw);

            // Direct2D needs the dxgi version of the backbuffer surface pointer.
            // Get a D2D surface from the DXGI back buffer to use as the D2D render target.
            using (var dxgiBackBuffer = _swapChain.GetBackBuffer<SharpDX.DXGI.Surface>(0))
                BitmapTarget2D = ToDispose(new SharpDX.Direct2D1.Bitmap1(d2dContext, dxgiBackBuffer, bitmapProperties));

            // So now we can set the Direct2D render target.
            d2dContext.Target = BitmapTarget2D;

            // Set D2D text anti-alias mode to Grayscale to ensure proper rendering of text on intermediate surfaces.
            d2dContext.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;

            #endregion
        }

        /// <summary>
        /// Abstract method to implement the main application/rendering loop
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Present the back buffer of the swap chain.
        /// </summary>
        public virtual void Present()
        {
            // The application may optionally specify "dirty" or "scroll" rects to improve efficiency
            // in certain scenarios. In this sample we do not utilize those features.
            var parameters = new SharpDX.DXGI.PresentParameters();

            try
            {
                // If enabled the first argument instructs DXGI to block until VSync, 
                // putting the application to sleep until the next VSync. 
                // This ensures we don't waste any CPU/GPU cycles rendering frames that will never 
                // be displayed to the screen.
                _swapChain.Present((VSync ? 1 : 0), SharpDX.DXGI.PresentFlags.None, parameters);
            }
            catch (SharpDX.SharpDXException ex)
            {
                // If the device was removed either by a disconnect or a driver upgrade, we 
                // must completely reinitialize the renderer.
                if (ex.ResultCode == SharpDX.DXGI.ResultCode.DeviceRemoved
                    || ex.ResultCode == SharpDX.DXGI.ResultCode.DeviceReset)
                    DeviceManager.Initialize(DeviceManager.Dpi);
                else
                    throw;
            }
        }

        /// <summary>
        /// Creates the swap chain description.
        /// </summary>
        /// <returns>A swap chain description</returns>
        /// <remarks>
        /// This method can be overloaded in order to modify default parameters.
        /// </remarks>
        protected virtual SharpDX.DXGI.SwapChainDescription1 CreateSwapChainDescription()
        {
            // SwapChain description
            var desc = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = Width,
                Height = Height,
                // B8G8R8A8_UNorm gives us better performance 
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = SharpDX.DXGI.Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 1,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.Discard,
                Flags = SwapChainFlags.AllowModeSwitch
            };
            return desc;
        }

        /// <summary>
        /// Creates the swap chain.
        /// </summary>
        /// <param name="factory">The DXGI factory</param>
        /// <param name="device">The D3D11 device</param>
        /// <param name="desc">The swap chain description</param>
        /// <returns>An instance of swap chain</returns>
        protected abstract SharpDX.DXGI.SwapChain1 CreateSwapChain(SharpDX.DXGI.Factory2 factory, SharpDX.Direct3D11.Device1 device, SharpDX.DXGI.SwapChainDescription1 desc);

        protected override void Dispose(bool disposeManagedResources)
        {
            if (disposeManagedResources)
            {
                if (SwapChain != null)
                {
                    // Make sure we are no longer in fullscreen or the disposing of
                    // Direct3D device will generate an exception.
                    SwapChain.IsFullScreen = false;
                }
            }
            base.Dispose(disposeManagedResources);
        }

        /// <summary>
        /// Event fired when size of the underlying render control is changed
        /// </summary>
        public event Action<D3DApplicationBase> OnSizeChanged;
    }
}
