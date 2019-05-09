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
using System.Windows.Forms;

using SharpDX;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch02_02AddingTexture
{
    public class D3DApp : D3DApplicationDesktop
    {
        // The vertex shader
        ShaderBytecode vertexShaderBytecode;
        VertexShader vertexShader;

        // The pixel shader
        ShaderBytecode pixelShaderBytecode;
        PixelShader pixelShader;

        // The vertex layout for the IA
        InputLayout vertexLayout;

        // A buffer that will be used to update the worldViewProjection 
        // constant buffer of the vertex shader
        Buffer worldViewProjectionBuffer;

        // Our configured depth stencil state
        DepthStencilState depthStencilState;

        public D3DApp(System.Windows.Forms.Form window)
            : base(window)
        {
        }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            var description = base.CreateSwapChainDescription();
            //description.SampleDescription = new SampleDescription(4, 0);
            return description;
        }

        protected override void CreateDeviceDependentResources(DeviceManager deviceManager)
        {
            base.CreateDeviceDependentResources(deviceManager);

            // Release all resources
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref vertexShaderBytecode);
            RemoveAndDispose(ref pixelShader);
            RemoveAndDispose(ref pixelShaderBytecode);

            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref worldViewProjectionBuffer);
            RemoveAndDispose(ref depthStencilState);

            // Get a reference to the Device1 instance and immediate context
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;

            ShaderFlags shaderFlags = ShaderFlags.None;
#if DEBUG
            shaderFlags = ShaderFlags.Debug;
#endif

            // Compile and create the vertex shader
            vertexShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile("Simple.hlsl", "VSMain", "vs_5_0", shaderFlags));
            vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));

            // Compile and create the pixel shader
            pixelShaderBytecode = ToDispose(ShaderBytecode.CompileFromFile("Simple.hlsl", "PSMain", "ps_5_0", shaderFlags));
            pixelShader = ToDispose(new PixelShader(device, pixelShaderBytecode));

            // Layout from VertexShader input signature
            vertexLayout = ToDispose(new InputLayout(device,
                vertexShaderBytecode.GetPart(ShaderBytecodePart.InputSignatureBlob),
                //ShaderSignature.GetInputSignature(vertexShaderBytecode),
                new[]
                {
                    // input semantic SV_Position = vertex coordinate in object space
                    new InputElement("SV_Position", 0, Format.R32G32B32A32_Float, 0, 0),
                    // input semantic TEXTCOORD = vertex texture coordinate
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0)
                }));

            // Create the constant buffer that will
            // store our worldViewProjection matrix
            worldViewProjectionBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Configure the depth buffer to discard pixels that are
            // further than the current pixel.
            depthStencilState = ToDispose(new DepthStencilState(device,
                new DepthStencilStateDescription()
                {
                    IsDepthEnabled = true, // enable depth?
                    DepthComparison = Comparison.Less,
                    DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,
                    IsStencilEnabled = false,// enable stencil?
                    StencilReadMask = 0xff, // 0xff (no mask)
                    StencilWriteMask = 0xff,// 0xff (no mask)
                    // Configure FrontFace depth/stencil operations
                    FrontFace = new DepthStencilOperationDescription()
                    {
                        Comparison = Comparison.Always,
                        PassOperation = StencilOperation.Keep,
                        FailOperation = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Increment
                    },
                    // Configure BackFace depth/stencil operations
                    BackFace = new DepthStencilOperationDescription()
                    {
                        Comparison = Comparison.Always,
                        PassOperation = StencilOperation.Keep,
                        FailOperation = StencilOperation.Keep,
                        DepthFailOperation = StencilOperation.Decrement
                    },
                }));

            // Tell the IA what the vertices will look like
            // in this case two 4-component 32bit floats
            // (32 bytes in total)
            context.InputAssembler.InputLayout = vertexLayout;

            // Set our constant buffer (to store worldViewProjection)
            context.VertexShader.SetConstantBuffer(0, worldViewProjectionBuffer);

            // Set the vertex shader to run
            context.VertexShader.Set(vertexShader);

            // Set the pixel shader to run
            context.PixelShader.Set(pixelShader);

            // Set our depth stencil state
            context.OutputMerger.DepthStencilState = depthStencilState;
        }

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            base.CreateSizeDependentResources(app);

            //// Create a viewport descriptor of the render size.
            //this.Viewport = new SharpDX.ViewportF(
            //    (float)RenderTargetBounds.X + 100,
            //    (float)RenderTargetBounds.Y + 100,
            //    (float)RenderTargetBounds.Width - 200,
            //    (float)RenderTargetBounds.Height - 200,
            //    0.0f,   // min depth
            //    1.0f);  // max depth

            //// Set the current viewport for the rasterizer.
            //this.DeviceManager.Direct3DContext.Rasterizer.SetViewport(Viewport);
        }

        public override void Run()
        {
            #region Create renderers

            // Note: the renderers take care of creating their own 
            // device resources and listen for DeviceManager.OnInitialize

            // Create and initialize the axis lines renderer
            var axisLines = ToDispose(new AxisLinesRenderer());
            axisLines.Initialize(this);

            // Create and initialize the triangle renderer
            var triangle = ToDispose(new TriangleRenderer());
            triangle.Initialize(this);

            // Create and initialize the quad renderer
            var quad = ToDispose(new QuadRenderer());
            quad.Initialize(this);

            // Create and initialize a Direct2D FPS text renderer
            var fps = ToDispose(new Common.FpsRenderer("Calibri", Color.CornflowerBlue, new Point(8, 8), 16));
            fps.Initialize(this);

            // Create and initialize a general purpose Direct2D text renderer
            // This will display some instructions and the current view and rotation offsets
            var textRenderer = ToDispose(new Common.TextRenderer("Calibri", Color.CornflowerBlue, new Point(8, 30), 12));
            textRenderer.Initialize(this);

            #endregion

            // Initialize the world matrix
            var worldMatrix = Matrix.Identity;

            // Set the camera position slightly to the right (x), above (y) and behind (-z)
            var cameraPosition = new Vector3(0.6f, 1f, -1.6f);
            var cameraTarget = Vector3.Zero; // Looking at the origin 0,0,0
            var cameraUp = Vector3.UnitY; // Y+ is Up

            // Prepare matrices
            // Create the view matrix from our camera position, look target and up direction
            var viewMatrix = Matrix.LookAtLH(cameraPosition, cameraTarget, cameraUp);

            // Create the projection matrix
            /* FoV 60degrees = Pi/3 radians */
            // Aspect ratio (based on window size), Near clip, Far clip
            var projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);
            };

            #region Rotation and window event handlers

            // Create a rotation vector to keep track of the rotation
            // around each of the axes
            var rotation = new Vector3(0.0f, 0.0f, 0.0f);

            // We will call this action to update text
            // for the text renderer
            Action updateText = () =>
            {
                textRenderer.Text =
                    String.Format("World rotation ({0}) (Up/Down Left/Right Wheel+-)\nView ({1}) (A/D, W/S, Shift+Wheel+-)"
                    + "\nPress X to reinitialize the device and resources (device ptr: {2})"
                    ,
                        rotation,
                        viewMatrix.TranslationVector,
                        DeviceManager.Direct3DDevice.NativePointer);
            };

            bool useDepthShaders = false;

            // Support keyboard/mouse input to rotate or move camera view
            var moveFactor = 0.02f; // how much to change on each keypress
            var shiftKey = false;
            var ctrlKey = false;
            Window.KeyDown += (s, e) =>
            {
                shiftKey = e.Shift;
                ctrlKey = e.Control;

                switch (e.KeyCode)
                {
                    // WASD -> pans view
                    case Keys.A:
                        viewMatrix.TranslationVector += new Vector3(moveFactor * 2, 0f, 0f);
                        break;
                    case Keys.D:
                        viewMatrix.TranslationVector -= new Vector3(moveFactor * 2, 0f, 0f);
                        break;
                    case Keys.S:
                        if (shiftKey)
                            viewMatrix.TranslationVector += new Vector3(0f, moveFactor * 2, 0f);
                        else
                            viewMatrix.TranslationVector += new Vector3(0f, 0f, 1) * moveFactor * 2;
                        break;
                    case Keys.W:
                        if (shiftKey)
                            viewMatrix.TranslationVector -= new Vector3(0f, moveFactor * 2, 0f);
                        else
                            viewMatrix.TranslationVector -= new Vector3(0f, 0f, 1) * moveFactor * 2;
                        break;
                    // Up/Down and Left/Right - rotates around X / Y respectively
                    // (Mouse wheel rotates around Z)
                    case Keys.Down:
                        worldMatrix *= Matrix.RotationX(-moveFactor);
                        rotation -= new Vector3(moveFactor, 0f, 0f);
                        break;
                    case Keys.Up:
                        worldMatrix *= Matrix.RotationX(moveFactor);
                        rotation += new Vector3(moveFactor, 0f, 0f);
                        break;
                    case Keys.Left:
                        worldMatrix *= Matrix.RotationY(-moveFactor);
                        rotation -= new Vector3(0f, moveFactor, 0f);
                        break;
                    case Keys.Right:
                        worldMatrix *= Matrix.RotationY(moveFactor);
                        rotation += new Vector3(0f, moveFactor, 0f);
                        break;

                    case Keys.X:
                        // To test for correct resource recreation
                        // Simulate device reset or lost.
                        System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                        DeviceManager.Initialize(DeviceManager.Dpi);
                        System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                        break;
                }

                updateText();
            };
            Window.KeyUp += (s, e) =>
            {
                // Clear the shift/ctrl keys so they aren't sticky
                if (e.KeyCode == Keys.ShiftKey)
                    shiftKey = false;
                if (e.KeyCode == Keys.ControlKey)
                    ctrlKey = false;
            };
            Window.MouseWheel += (s, e) =>
            {
                if (shiftKey)
                {
                    // Zoom in/out
                    viewMatrix.TranslationVector -= new Vector3(0f, 0f, (e.Delta / 120f) * moveFactor * 2);
                }
                else
                {
                    // rotate around Z-axis
                    viewMatrix *= Matrix.RotationZ((e.Delta / 120f) * moveFactor);
                    rotation += new Vector3(0f, 0f, (e.Delta / 120f) * moveFactor);
                }
                updateText();
            };

            var lastX = 0;
            var lastY = 0;

            Window.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    lastX = e.X;
                    lastY = e.Y;
                }
            };

            Window.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    var yRotate = lastX - e.X;
                    var xRotate = lastY - e.Y;
                    lastY = e.Y;
                    lastX = e.X;

                    // Mouse move changes 

                    // Rotate view (i.e. camera)
                    //viewMatrix *= Matrix.RotationX(xRotate * moveFactor);
                    //viewMatrix *= Matrix.RotationY(yRotate * moveFactor);

                    // Rotate around origin
                    var backup = viewMatrix.TranslationVector;
                    viewMatrix.TranslationVector = Vector3.Zero;
                    viewMatrix *= Matrix.RotationX(xRotate * moveFactor);
                    viewMatrix.TranslationVector = backup;
                    worldMatrix *= Matrix.RotationY(yRotate * moveFactor);

                    updateText();
                }
            };

            // Display instructions with initial values
            updateText();

            #endregion

            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            #region Render loop

            // Create and run the render loop
            RenderLoop.Run(Window, () =>
            {
                // Start of frame:

                // Retrieve immediate context
                var context = DeviceManager.Direct3DContext;

                // Clear depth stencil view
                context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
                // Clear render target view
                context.ClearRenderTargetView(RenderTargetView, Color.White);

                // Create viewProjection matrix
                var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);

                // If Keys.CtrlKey is down, auto rotate viewProjection based on time
                if (ctrlKey)
                {
                    var time = clock.ElapsedMilliseconds / 1000.0f;
                    viewProjection = Matrix.RotationY(time * 1.8f) * Matrix.RotationZ(time * 1f) * Matrix.RotationZ(time * 0.6f) * viewProjection;
                }

                // Create WorldViewProjection Matrix
                var worldViewProjection = worldMatrix * viewProjection;
                // HLSL uses "column major" matrices so transpose from "row major" first
                worldViewProjection.Transpose();
                // Write the worldViewProjection to the constant buffer
                context.UpdateSubresource(ref worldViewProjection, worldViewProjectionBuffer);

                // Render the primitives
                axisLines.Render();
                triangle.Render();
                quad.Render();

                // Render FPS
                fps.Render();

                // Render instructions + position changes
                textRenderer.Render();

                // Present the frame
                Present();
            });
            #endregion
        }
    }
}