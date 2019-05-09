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

namespace Ch09_01Benchmark
{
    public class D3DApp : Common.D3DApplicationDesktop
    {
        // The vertex shader
        VertexShader vertexShader;

        // The pixel shader
        PixelShader pixelShader;

        // A pixel shader that renders the depth (black closer, white further away)
        PixelShader depthPixelShader;

        // The lambert shader
        PixelShader lambertShader;

        // The Blinn-Phong shader
        PixelShader blinnPhongShader;

        // The Phong shader
        PixelShader phongShader;

        // The vertex layout for the IA
        InputLayout vertexLayout;

        // Our configured depth stencil state
        DepthStencilState depthStencilState;

        // A buffer that will be used to update the worldViewProjection 
        // constant buffer of the vertex shader
        Buffer perObjectBuffer;

        // A buffer that will be used to update the lights
        Buffer perFrameBuffer;

        // A buffer that will be used to update object materials
        Buffer perMaterialBuffer;

        // A buffer that will be used to update object armature/skeleton
        Buffer perArmatureBuffer;

        DeviceContext[] contextList;
        int threadCount = 2;
        int additionalCPULoad = 0;

        public D3DApp(System.Windows.Forms.Form window) : base(window) { }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            var description = base.CreateSwapChainDescription();
            description.SampleDescription = new SampleDescription(4, 0);
            return description;
        }

        protected override void CreateDeviceDependentResources(DeviceManager deviceManager)
        {
            base.CreateDeviceDependentResources(deviceManager);

            // Release all resources
            RemoveAndDispose(ref vertexShader);

            RemoveAndDispose(ref pixelShader);
            RemoveAndDispose(ref depthPixelShader);
            RemoveAndDispose(ref lambertShader);
            RemoveAndDispose(ref blinnPhongShader);
            RemoveAndDispose(ref phongShader);

            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref perObjectBuffer);
            RemoveAndDispose(ref perFrameBuffer);
            RemoveAndDispose(ref perMaterialBuffer);
            RemoveAndDispose(ref perArmatureBuffer);

            RemoveAndDispose(ref depthStencilState);

            // Get a reference to the Device1 instance and immediate context
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;

            // Compile and create the vertex shader and input layout
            using (var vertexShaderBytecode = HLSLCompiler.CompileFromFile(@"Shaders\VS.hlsl", "VSMain", "vs_5_0"))
            {
                vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));
                // Layout from VertexShader input signature
                vertexLayout = ToDispose(new InputLayout(device,
                    vertexShaderBytecode.GetPart(ShaderBytecodePart.InputSignatureBlob),
                new[]
                {
                    // "SV_Position" = vertex coordinate in object space
                    new InputElement("SV_Position", 0, Format.R32G32B32_Float, 0, 0),
                    // "NORMAL" = the vertex normal
                    new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                    // "COLOR"
                    new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 24, 0),
                    // "UV"
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 28, 0),
                    // "SkinIndices"
                    new InputElement("BLENDINDICES", 0, Format.R32G32B32A32_UInt, 36, 0),
                    // "SkinWeights"
                    new InputElement("BLENDWEIGHT", 0, Format.R32G32B32A32_Float, 52, 0),
                }));
            }

            // Compile and create the pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\SimplePS.hlsl", "PSMain", "ps_5_0"))
                pixelShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the depth vertex and pixel shaders
            // This shader is for checking what the depth buffer would look like
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\DepthPS.hlsl", "PSMain", "ps_5_0"))
                depthPixelShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the Lambert pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\DiffusePS.hlsl", "PSMain", "ps_5_0"))
                lambertShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the blinn phong pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\BlinnPhongPS.hlsl", "PSMain", "ps_5_0"))
                blinnPhongShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the Lambert pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\PhongPS.hlsl", "PSMain", "ps_5_0"))
                phongShader = ToDispose(new PixelShader(device, bytecode));

            // IMPORTANT: A constant buffer's size must be a multiple of 16-bytes
            // use LayoutKind.Explicit and an explicit Size= to force this for structures
            // or alternatively add padding fields and use a LayoutKind.Sequential and Pack=1

            // Create the constant buffer that will
            // store our worldViewProjection matrix
            perObjectBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<ConstantBuffers.PerObject>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Create the per frame constant buffer
            // lighting / camera position
            perFrameBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffers.PerFrame>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Create the per material constant buffer
            perMaterialBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffers.PerMaterial>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Create the per armature/skeletong constant buffer
            perArmatureBuffer = ToDispose(new Buffer(device, ConstantBuffers.PerArmature.Size(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

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

            // Initialize the ImmediateContext pipeline stages
            InitializeContext(context);
        }

        //protected void InitializeContext(DeviceContext context)
        //{
        //    // Tell the IA what the vertices will look like
        //    context.InputAssembler.InputLayout = vertexLayout;

        //    // Set our constant buffer (to store worldViewProjection)
        //    context.VertexShader.SetConstantBuffer(0, perObjectBuffer);
        //    context.VertexShader.SetConstantBuffer(1, perFrameBuffer);
        //    context.VertexShader.SetConstantBuffer(2, perMaterialBuffer);
        //    context.VertexShader.SetConstantBuffer(3, perArmatureBuffer);

        //    // Set the vertex shader to run
        //    context.VertexShader.Set(vertexShader);

        //    // Set our pixel constant buffers
        //    context.PixelShader.SetConstantBuffer(1, perFrameBuffer);
        //    context.PixelShader.SetConstantBuffer(2, perMaterialBuffer);

        //    // Set the pixel shader to run
        //    context.PixelShader.Set(blinnPhongUVShader);

        //    // Set our depth stencil state
        //    context.OutputMerger.DepthStencilState = depthStencilState;

        //    // Set viewport
        //    context.Rasterizer.SetViewports(this.DeviceManager.Direct3DContext.Rasterizer.GetViewports());

        //    // No culling
        //    if (context.Rasterizer.State == null)
        //    {
        //        context.Rasterizer.State = ToDispose(new RasterizerState(this.DeviceManager.Direct3DDevice, new RasterizerStateDescription()
        //        {
        //            FillMode = FillMode.Solid,
        //            CullMode = CullMode.None,
        //        }));
        //    }

        //    // Set render targets
        //    context.OutputMerger.SetTargets(this.DepthStencilView, this.RenderTargetView);
        //}

        protected void InitializeContext(DeviceContext context)
        {
            // Tell the IA what the vertices will look like
            context.InputAssembler.InputLayout = vertexLayout;

            // Set the constant buffers for vertex shader stage
            context.VertexShader.SetConstantBuffer(0, perObjectBuffer);
            context.VertexShader.SetConstantBuffer(1, perFrameBuffer);
            context.VertexShader.SetConstantBuffer(2, perMaterialBuffer);
            context.VertexShader.SetConstantBuffer(3, perArmatureBuffer);

            // Set the default vertex shader to run
            context.VertexShader.Set(vertexShader);

            // Set our pixel shader constant buffers
            context.PixelShader.SetConstantBuffer(1, perFrameBuffer);
            context.PixelShader.SetConstantBuffer(2, perMaterialBuffer);

            // Set the pixel shader to run
            context.PixelShader.Set(blinnPhongShader);

            // Set our depth stencil state
            context.OutputMerger.DepthStencilState = depthStencilState;

            // Set viewport
            context.Rasterizer.SetViewports(this.DeviceManager
                .Direct3DContext.Rasterizer.GetViewports());

            // Back-face culling
            if (context.Rasterizer.State == null)
            {
                context.Rasterizer.State = ToDispose(new RasterizerState(this.DeviceManager.Direct3DDevice, new RasterizerStateDescription()
                {
                    FillMode = FillMode.Solid,
                    CullMode = CullMode.Back,
                }));
            }

            // Set render targets
            context.OutputMerger.SetTargets(this.DepthStencilView, this.RenderTargetView);
        }


        // Action used to create and initialize contexts
        private void SetupContextList()
        {
            // Remove existing deferred contexts
            if (contextList != null)
            {
                foreach (var context in contextList)
                    if (!context.IsDisposed && context.TypeInfo != DeviceContextType.Immediate)
                        context.Dispose();
            }
            contextList = new DeviceContext[threadCount];

            // If only one context we will use the ImmediateContext directly
            if (threadCount == 1)
            {
                contextList[0] = this.DeviceManager.Direct3DContext;
                return;
            }

            for (var i = 0; i < threadCount; i++)
            {
                contextList[i] = ToDispose(new DeviceContext(this.DeviceManager.Direct3DDevice));
                InitializeContext(contextList[i]);
            }
        }

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            // Clear the render targets of any deferred contexts before
            // processing a resize.
            if (contextList != null)
            {
                foreach (var context in contextList)
                {
                    if (context.TypeInfo == DeviceContextType.Deferred)
                    {
                        context.OutputMerger.ResetTargets();
                        context.Dispose();
                    }
                }
            }

            base.CreateSizeDependentResources(app);

            // Reinstate the render targets of any deferred contexts
            if (contextList != null)
            {
                SetupContextList();
            }
        }

        public override void Run()
        {
            #region Create renderers

            // Note: the renderers take care of creating their own 
            // device resources and listen for DeviceManager.OnInitialize

            // Create a axis-grid renderer
            var axisGrid = ToDispose(new AxisGridRenderer());
            axisGrid.Initialize(this);

            // Determine if the hardware driver supports CommandLists
            // If not, the Direct3D framework will emulate support.
            bool createResourcesConcurrently;
            bool nativeCommandListSupport;
            DeviceManager.Direct3DDevice.CheckThreadingSupport(out createResourcesConcurrently, out nativeCommandListSupport);
            
            // Create and initialize the mesh renderer
            var loadedMesh = Common.Mesh.LoadFromFile("Character.cmo");
            List<MeshRenderer> meshes = new List<MeshRenderer>();

            int meshRows = 10;
            int meshColumns = 10;

            Action createMeshes = () =>
            {
                // Remove meshes
                foreach (var mesh in meshes)
                {
                    mesh.Dispose();
                }
                meshes.Clear();

                // Add the same mesh multiple times, separate by the combined extent
                var minExtent = (from mesh in loadedMesh
                                 orderby new { mesh.Extent.Min.X, mesh.Extent.Min.Z }
                                 select mesh.Extent).First();
                var maxExtent = (from mesh in loadedMesh
                                 orderby new { mesh.Extent.Max.X, mesh.Extent.Max.Z } descending
                                 select mesh.Extent).First();
                var extentDiff = (maxExtent.Max - minExtent.Min);

                for (int x = -(meshColumns/2); x < (meshColumns/2); x++)
                {
                    for (int z = -(meshRows/2); z < (meshRows/2); z++)
                    {
                        var meshGroup = (from mesh in loadedMesh
                                         select ToDispose(new MeshRenderer(mesh))).ToList();

                        // Reposition based on width/depth of combined extent
                        foreach (var m in meshGroup)
                        {
                            m.World.TranslationVector = new Vector3(m.Mesh.Extent.Center.X + extentDiff.X * x, m.Mesh.Extent.Min.Y, m.Mesh.Extent.Center.Z + extentDiff.Z * z);
                        }

                        meshes.AddRange(meshGroup);
                    }
                }
                // Initialize each mesh
                meshes.ForEach(m => m.Initialize(this));
            };
            createMeshes();

            bool animationEnabled = false;
            bool changeAnimation = false;
            Action toggleAnimation = () =>
            {
                // Set the first animation as the current animation and start clock
                meshes.ForEach(m =>
                {
                    if (!animationEnabled && m.Mesh.Animations.Any())
                    {
                        m.CurrentAnimation = m.Mesh.Animations.First().Value;
                        m.Clock.Start();
                    }
                    else
                    {
                        m.CurrentAnimation = null;
                    }
                });
                animationEnabled = !animationEnabled;
            };

            var meshWorld = Matrix.Identity;

            // Create and initialize a Direct2D FPS text renderer
            var fps = ToDispose(new Common.FpsRenderer("Calibri", Color.CornflowerBlue, new Point(8, 8), 16));
            fps.Initialize(this);

            // Create and initialize a general purpose Direct2D text renderer
            // This will display some instructions and the current view and rotation offsets
            var textRenderer = ToDispose(new Common.TextRenderer("Calibri", Color.CornflowerBlue, new Point(8, 40), 12));
            textRenderer.Initialize(this);

            #endregion

            // Initialize the world matrix
            var worldMatrix = Matrix.Identity;

            // Set the camera position
            var cameraPosition = new Vector3(0, 1, 10);
            var cameraTarget = Vector3.Zero; // Looking at the origin 0,0,0
            var cameraUp = Vector3.UnitY; // Y+ is Up

            // Prepare matrices
            // Create the view matrix from our camera position, look target and up direction
            var viewMatrix = Matrix.LookAtRH(cameraPosition, cameraTarget, cameraUp);
            viewMatrix.TranslationVector += new Vector3(0, -0.98f, 0);

            // Create the projection matrix
            /* FoV 60degrees = Pi/3 radians */
            // Aspect ratio (based on window size), Near clip, Far clip
            var projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.5f, 100f);
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
                    String.Format(
                    "\nToggle animation: Backspace, Pause animation: P"
                    + "\nThreads: {0} (+/-)"
                    + " Add CPU load: {1} matrix multiplications (Shift +/-)"
                    + "\nMeshes: {2} (Up/Down, Left/Right)",
                        threadCount,
                        additionalCPULoad,
                        meshRows * meshColumns);
            };

            Dictionary<Keys, bool> keyToggles = new Dictionary<Keys, bool>();
            keyToggles[Keys.Z] = false;
            keyToggles[Keys.F] = false;

            // Support keyboard/mouse input to rotate or move camera view
            var moveFactor = 0.02f; // how much to change on each keypress
            var shiftKey = false;
            var ctrlKey = false;
            var background = Color.White;
            Window.KeyDown += (s, e) =>
            {
                var context = DeviceManager.Direct3DContext;

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
                            viewMatrix.TranslationVector -= new Vector3(0f, 0f, 1) * moveFactor * 2;
                        break;
                    case Keys.W:
                        if (shiftKey)
                            viewMatrix.TranslationVector -= new Vector3(0f, moveFactor * 2, 0f);
                        else
                            viewMatrix.TranslationVector += new Vector3(0f, 0f, 1) * moveFactor * 2;
                        break;
                    // Up/Down and Left/Right - rotates around X / Y respectively
                    // (Mouse wheel rotates around Z)
                    case Keys.Down:
                        worldMatrix *= Matrix.RotationX(moveFactor);
                        rotation += new Vector3(moveFactor, 0f, 0f);
                        break;
                    case Keys.Up:
                        worldMatrix *= Matrix.RotationX(-moveFactor);
                        rotation -= new Vector3(moveFactor, 0f, 0f);
                        break;
                    case Keys.Left:
                        worldMatrix *= Matrix.RotationY(moveFactor);
                        rotation += new Vector3(0f, moveFactor, 0f);
                        break;
                    case Keys.Right:
                        worldMatrix *= Matrix.RotationY(-moveFactor);
                        rotation -= new Vector3(0f, moveFactor, 0f);
                        break;
                    case Keys.T:
                        fps.Show = !fps.Show;
                        textRenderer.Show = !textRenderer.Show;
                        break;
                    case Keys.B:
                        if (background == Color.White)
                        {
                            background = new Color(30, 30, 34);
                        }
                        else
                        {
                            background = Color.White;
                        }
                        break;
                    case Keys.G:
                        axisGrid.Show = !axisGrid.Show;
                        break;
                    case Keys.P:
                        // Pause or resume mesh animation
                        meshes.ForEach(m => {
                            if (m.Clock.IsRunning)
                                m.Clock.Stop();
                            else
                                m.Clock.Start();
                        });
                        break;
                    case Keys.X:
                        // To test for correct resource recreation
                        // Simulate device reset or lost.
                        System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                        DeviceManager.Initialize(DeviceManager.Dpi);
                        System.Diagnostics.Debug.WriteLine(SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
                        break;
                    case Keys.Z:
                        keyToggles[Keys.Z] = !keyToggles[Keys.Z];
                        if (keyToggles[Keys.Z])
                        {
                            context.PixelShader.Set(depthPixelShader);
                        }
                        else
                        {
                            context.PixelShader.Set(pixelShader);
                        }
                        break;
                    case Keys.F:
                        keyToggles[Keys.F] = !keyToggles[Keys.F];
                        RasterizerStateDescription rasterDesc;
                        if (context.Rasterizer.State != null)
                            rasterDesc = context.Rasterizer.State.Description;
                        else
                            rasterDesc = new RasterizerStateDescription()
                            {
                                CullMode = CullMode.Back,
                                FillMode = FillMode.Solid
                            };
                        if (keyToggles[Keys.F])
                        {
                            rasterDesc.FillMode = FillMode.Wireframe;
                            context.Rasterizer.State = ToDispose(new RasterizerState(context.Device, rasterDesc));
                        }
                        else
                        {
                            rasterDesc.FillMode = FillMode.Solid;
                            context.Rasterizer.State = ToDispose(new RasterizerState(context.Device, rasterDesc));
                        }
                        break;
                    case Keys.D1:
                        context.PixelShader.Set(pixelShader);
                        break;
                    case Keys.D2:
                        context.PixelShader.Set(lambertShader);
                        break;
                    case Keys.D3:
                        context.PixelShader.Set(phongShader);
                        break;
                    case Keys.D4:
                        context.PixelShader.Set(blinnPhongShader);
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
                    viewMatrix.TranslationVector += new Vector3(0f, 0f, (e.Delta / 120f) * moveFactor * 2);
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

            SetupContextList();

            bool initializeMesh = false;
            Window.KeyDown += (s, e) =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Back:
                        changeAnimation = true;
                        break;
                    case Keys.Up:
                        meshRows += 2;
                        initializeMesh = true;
                        break;
                    case Keys.Down:
                        meshRows = Math.Max(2, meshRows - 2);
                        initializeMesh = true;
                        break;
                    case Keys.Right:
                        meshColumns += 2;
                        initializeMesh = true;
                        break;
                    case Keys.Left:
                        meshColumns = Math.Max(2, meshColumns - 2);
                        initializeMesh = true;
                        break;
                    case Keys.Add:
                        if (shiftKey)
                        {
                            additionalCPULoad += 100;
                        }
                        else
                        {
                            threadCount++;
                        }
                        break;
                    case Keys.Subtract:
                        if (shiftKey)
                        {
                            additionalCPULoad = Math.Max(0, additionalCPULoad - 100);
                        }
                        else
                        {
                            threadCount = Math.Max(1, threadCount - 1);
                        }
                        break;
                    default:
                        break;
                }
                updateText();
            };

            // Action for rendering a mesh using the context
            // from the specified index wtihin contextList
            Action<int> renderMeshGroup = (int contextIndex) =>
            {
                // Retrieve appropriate context
                var renderContext = contextList[contextIndex];

                // Create viewProjection matrix
                var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);

                // Determine the meshes to render for this context
                int batchSize = (int)Math.Floor((double)meshes.Count / contextList.Length);
                int startIndex = batchSize * contextIndex;
                int endIndex = Math.Min(startIndex + batchSize, meshes.Count - 1);
                // If this is the last context include whatever remains to be
                // rendered due to the rounding above.
                if (contextIndex == contextList.Length - 1)
                    endIndex = meshes.Count - 1;

                // Loop over the meshes for this context and render them
                var perObject = new ConstantBuffers.PerObject();
                for (var i = startIndex; i <= endIndex; i++)
                {
                    // Simulate additional CPU load
                    for (var j = 0; j < additionalCPULoad; j++)
                    {
                        viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);
                    }

                    var m = meshes[i];
                    // Update perObject constant buffer
                    perObject.World = m.World * worldMatrix;
                    perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                    perObject.WorldViewProjection = perObject.World * viewProjection;
                    perObject.Transpose();
                    renderContext.UpdateSubresource(ref perObject, perObjectBuffer);

                    // Provide the material and armature constant buffer to the mesh renderer
                    m.PerArmatureBuffer = perArmatureBuffer;
                    m.PerMaterialBuffer = perMaterialBuffer;
                    // Specify the context to render with
                    m.Render(renderContext);
                }
            };

            #region Render loop

            var lastThreadCount = threadCount;

            // Create and run the render loop
            RenderLoop.Run(Window, () =>
            {
                if (initializeMesh)
                {
                    initializeMesh = false;
                    createMeshes();
                }

                if (changeAnimation)
                {
                    toggleAnimation();
                    changeAnimation = false;
                }

                if (lastThreadCount != threadCount)
                {
                    SetupContextList();
                    lastThreadCount = threadCount;
                }

                // Start of frame:

                // Retrieve immediate context
                var immediateContext = DeviceManager.Direct3DDevice.ImmediateContext;


                // The context at index 0 is always executed first
                var context = contextList[0];

                // Clear depth stencil view
                context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
                // Clear render target view
                context.ClearRenderTargetView(RenderTargetView, background);

                // Create viewProjection matrix
                var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);

                // Extract camera position from view
                var camPosition = Matrix.Transpose(Matrix.Invert(viewMatrix)).Column4;
                cameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

                // If Keys.CtrlKey is down, auto rotate viewProjection based on time
                var time = clock.ElapsedMilliseconds / 1000.0f;
                if (ctrlKey)
                {
                    viewProjection = Matrix.RotationY(time * 1.8f) * Matrix.RotationX(time * 1f) * Matrix.RotationZ(time * 0.6f) * viewProjection;
                }
                var worldRotation = Matrix.RotationAxis(Vector3.UnitY, time);
                
                var perFrame = new ConstantBuffers.PerFrame();
                perFrame.Light.Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                var lightDir = Vector3.Transform(new Vector3(1f, -1f, -1f), worldMatrix);
                perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);// new Vector3(Vector3.Transform(new Vector3(1f, -1f, 1f), worldMatrix * worldRotation).ToArray().Take(3).ToArray());
                perFrame.CameraPosition = cameraPosition;
                context.UpdateSubresource(ref perFrame, perFrameBuffer);

                // Render each object

                var perMaterial = new ConstantBuffers.PerMaterial();
                perMaterial.Ambient = new Color4(0.2f);
                perMaterial.Diffuse = Color.White;
                perMaterial.Emissive = new Color4(0);
                perMaterial.Specular = Color.White;
                perMaterial.SpecularPower = 20f;
                perMaterial.HasTexture = 0;
                perMaterial.UVTransform = Matrix.Identity;
                context.UpdateSubresource(ref perMaterial, perMaterialBuffer);

                var perObject = new ConstantBuffers.PerObject();

                // AXIS GRID
                using (var prevPixelShader = context.PixelShader.Get())
                {
                    perMaterial.HasTexture = 0;
                    perMaterial.UVTransform = Matrix.Identity;
                    context.UpdateSubresource(ref perMaterial, perMaterialBuffer);
                    context.PixelShader.Set(pixelShader);
                    perObject.World = worldMatrix;
                    perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                    perObject.WorldViewProjection = perObject.World * viewProjection;
                    perObject.Transpose();
                    context.UpdateSubresource(ref perObject, perObjectBuffer);
                    axisGrid.RenderContext = context;
                    axisGrid.Render();
                    context.PixelShader.Set(prevPixelShader);
                }

                Task[] renderTasks = new Task[contextList.Length];
                CommandList[] commands = new CommandList[contextList.Length];
                for (var i = 0; i < contextList.Length; i++)
                {
                    // 
                    var contextIndex = i;
                    renderTasks[i] = Task.Run(() =>
                    {
                        // Render logic
                        renderMeshGroup(contextIndex);

                        // Create the command list
                        if (contextList[contextIndex].TypeInfo == DeviceContextType.Deferred)
                        {
                            commands[contextIndex] = contextList[contextIndex].FinishCommandList(true);
                        }
                    });
                }
                // Wait for all the tasks to complete
                Task.WaitAll(renderTasks);
                
                // Replay the command lists on the immediate context
                for (var i = 0; i < contextList.Length; i++)
                {
                    if (contextList[i].TypeInfo == DeviceContextType.Deferred && commands[i] != null)
                    {
                        immediateContext.ExecuteCommandList(commands[i], false);
                        commands[i].Dispose();
                        commands[i] = null;
                    }
                }

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