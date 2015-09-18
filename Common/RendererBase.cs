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
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class RendererBase: Component
    {
        public DeviceManager DeviceManager { get; protected set; }
        public D3DApplicationBase Target { get; protected set; }
        public virtual bool Show { get; set; }
        public Matrix World;

        // Allow the context used for rendering to be specified
        SharpDX.Direct3D11.DeviceContext _renderContext = null;
        public SharpDX.Direct3D11.DeviceContext RenderContext
        {
            get { return _renderContext ?? this.DeviceManager.Direct3DContext; }
            set { _renderContext = value; }
        }

        public RendererBase()
        {
            World = Matrix.Identity;
            Show = true;
        }

        /// <summary>
        /// Initialize with the provided deviceManager
        /// </summary>
        /// <param name="deviceManager"></param>
        public virtual void Initialize(D3DApplicationBase app)
        {
            // If there is a previous device manager, remove event handler
            if (this.DeviceManager != null)
                this.DeviceManager.OnInitialize -= DeviceManager_OnInitialize;
            
            this.DeviceManager = app.DeviceManager;
            // Handle OnInitialize event so that device dependent
            // resources can be reinitialized.
            this.DeviceManager.OnInitialize += DeviceManager_OnInitialize;

            // If there is a previous target, remove event handler
            if (this.Target != null)
                this.Target.OnSizeChanged -= Target_OnSizeChanged;
            
            this.Target = app;
            // Handle OnSizeChanged event so that size dependent
            // resources can be reinitialized.
            this.Target.OnSizeChanged += Target_OnSizeChanged;

            // If the device is already initialized, then create
            // any device resources immediately.
            if (this.DeviceManager.Direct3DDevice != null)
            {
                CreateDeviceDependentResources();
            }
        }

        void DeviceManager_OnInitialize(DeviceManager deviceManager)
        {
            CreateDeviceDependentResources();
        }

        void Target_OnSizeChanged(D3DApplicationBase target)
        {
            CreateSizeDependentResources();
        }

        /// <summary>
        /// Create any resources that depend on the device or device context
        /// </summary>
        protected virtual void CreateDeviceDependentResources()
        {
        }

        /// <summary>
        /// Create any resources that depend upon the size of the render target
        /// </summary>
        protected virtual void CreateSizeDependentResources()
        {
        }

        /// <summary>
        /// Render a frame
        /// </summary>
        public void Render()
        {
            if (Show)
                DoRender();
        }

        /// <summary>
        /// Each descendant of RendererBase performs a frame
        /// render within the implementation of DoRender
        /// </summary>
        protected abstract void DoRender();

        public void Render(SharpDX.Direct3D11.DeviceContext context)
        {
            if (Show)
                DoRender(context);
        }

        protected virtual void DoRender(SharpDX.Direct3D11.DeviceContext context)
        {

        }
    }
}
