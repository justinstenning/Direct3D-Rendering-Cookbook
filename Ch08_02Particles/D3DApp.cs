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

namespace Ch08_02Particles
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

        public D3DApp(System.Windows.Forms.Form window) : base(window) { }

        protected override SwapChainDescription1 CreateSwapChainDescription()
        {
            var description = base.CreateSwapChainDescription();
            
            description.SampleDescription = new SampleDescription(1, 0);
            
            // Note: multi-sampling is not supported if using UnorderedAccess flag
            //       using the render target in a UAV has a number of constraints
            //       such as requiring an R8G8B8A8_... format, and no multi-sampling

            // So we can use the render target in a SRV we set the ShaderInput flag
            description.Usage |= Usage.ShaderInput;
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
	                // "TANGENT"
                    new InputElement("TANGENT", 0, Format.R32G32B32A32_Float, 68, 0),
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

            // Compile and create the Lambert pixel shader
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

            // Tell the IA what the vertices will look like
            context.InputAssembler.InputLayout = vertexLayout;

            // Set our constant buffer (to store worldViewProjection)
            context.VertexShader.SetConstantBuffer(0, perObjectBuffer);
            context.VertexShader.SetConstantBuffer(1, perFrameBuffer);
            context.VertexShader.SetConstantBuffer(2, perMaterialBuffer);
            context.VertexShader.SetConstantBuffer(3, perArmatureBuffer);

            // Set the vertex shader to run
            context.VertexShader.Set(vertexShader);

            // Set gemoetry shader buffers
            context.GeometryShader.SetConstantBuffer(0, perObjectBuffer);
            context.GeometryShader.SetConstantBuffer(1, perFrameBuffer);

            // Set our pixel constant buffers
            context.PixelShader.SetConstantBuffer(1, perFrameBuffer);
            context.PixelShader.SetConstantBuffer(2, perMaterialBuffer);

            // Set the pixel shader to run
            context.PixelShader.Set(blinnPhongShader);

            // Set our depth stencil state
            context.OutputMerger.DepthStencilState = depthStencilState;

            // No culling
            context.Rasterizer.State = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            }));
        }

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            base.CreateSizeDependentResources(app);
        }

        public override void Run()
        {
            #region Create renderers

            // Note: the renderers take care of creating their own 
            // device resources and listen for DeviceManager.OnInitialize

            var particleSystem = ToDispose(new ParticleRenderer());
            particleSystem.Initialize(this);
            var totalParticles = 100000;
            particleSystem.Constants.DomainBoundsMax = new Vector3(20, 20, 20);
            particleSystem.Constants.DomainBoundsMin = new Vector3(-20, 0, -20);
            particleSystem.Constants.ForceDirection = -Vector3.UnitY;
            particleSystem.Constants.ForceStrength = 1.8f;
            particleSystem.Constants.Radius = 0.05f;
            float particleLifetime = 13f;
            particleSystem.InitializeParticles(totalParticles, particleLifetime);

            // Create a axis-grid renderer
            var axisGrid = ToDispose(new AxisGridRenderer());
            axisGrid.Initialize(this);

            // Create and initialize the mesh renderer
            var loadedMesh = Common.Mesh.LoadFromFile("cartoon_village.cmo");
            List<MeshRenderer> meshes = new List<MeshRenderer>();
            meshes.AddRange((from mesh in loadedMesh
                             select ToDispose(new MeshRenderer(mesh))));
            foreach (var m in meshes) {
				m.Initialize(this);
				m.World = Matrix.Identity;
			}

            // Set the first animation as the current animation and start clock
            foreach (var m in meshes)
			{
                if (m.Mesh.Animations != null && m.Mesh.Animations.Any())
                    m.CurrentAnimation = m.Mesh.Animations.First().Value;
                m.Clock.Start();
            }

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

            // Set the camera position slightly behind (z)
            var cameraPosition = new Vector3(0, 1, 20);
            var cameraTarget = Vector3.Zero; // Looking at the origin 0,0,0
            var cameraUp = Vector3.UnitY; // Y+ is Up

            // Prepare matrices
            // Create the view matrix from our camera position, look target and up direction
            var viewMatrix = Matrix.LookAtRH(cameraPosition, cameraTarget, cameraUp);
            viewMatrix.TranslationVector += new Vector3(0, -0.98f, 0);

            // Create the projection matrix
            /* FoV 60degrees = Pi/3 radians */
            // Aspect ratio (based on window size), Near clip, Far clip
            var projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);
            };

            bool paused = false;
            var simTime = new System.Diagnostics.Stopwatch();
            simTime.Start();

            List<string> particleShaders = new List<string>();
            //particleShaders.Add("CS");
            particleShaders.Add("Snowfall");
            particleShaders.Add("Waves");
            particleShaders.Add("Sweeping");
            int particleIndx = 0;

            #region Rotation and window event handlers

            // Create a rotation vector to keep track of the rotation
            // around each of the axes
            var rotation = new Vector3(0.0f, 0.0f, 0.0f);

            // We will call this action to update text
            // for the text renderer
            Action updateText = () =>
            {
                textRenderer.Text =
                    String.Format("Rotation ({0}) (Up/Down Left/Right Wheel+-)\nView ({1}) (A/D, W/S, Shift+Wheel+-)"
                    //+ "\nPress 1,2,3,4,5,6,7,8 to switch shaders"
                    + "\nTime: {2:0.00} (P to toggle, R to reset scene)"
                    + "\nParticles = {3:#0} (+/- to change)"
                    //+ "\nPress Z to show/hide depth buffer - Press F to toggle wireframe"
                    //+ "\nPress 1-8 to switch shaders"
                        , rotation,
                        viewMatrix.TranslationVector,
                        simTime.Elapsed.TotalSeconds,
                        totalParticles
                        );
            };

            Dictionary<Keys, bool> keyToggles = new Dictionary<Keys, bool>();
            keyToggles[Keys.Z] = false;
            keyToggles[Keys.F] = false;
            keyToggles[Keys.I] = true;

            // Support keyboard/mouse input to rotate or move camera view
            var moveFactor = 0.02f; // how much to change on each keypress
            var shiftKey = false;
            var ctrlKey = false;
            var background = new Color(30, 30, 34);
            Vector2 rightMouseClick = new Vector2(0, 0);
            Window.KeyDown += (s, e) =>
            {
                var context = DeviceManager.Direct3DContext;

                shiftKey = e.Shift;
                ctrlKey = e.Control;

                switch (e.KeyCode)
                {
                    // WASD -> pans view
                    case Keys.A:
                        viewMatrix.TranslationVector += new Vector3(moveFactor * 12, 0f, 0f);
                        break;
                    case Keys.D:
                        viewMatrix.TranslationVector -= new Vector3(moveFactor * 12, 0f, 0f);
                        break;
                    case Keys.S:
                        if (shiftKey)
                            viewMatrix.TranslationVector += new Vector3(0f, moveFactor * 12, 0f);
                        else
                            viewMatrix.TranslationVector -= new Vector3(0f, 0f, 1) * moveFactor * 12;
                        break;
                    case Keys.W:
                        if (shiftKey)
                            viewMatrix.TranslationVector -= new Vector3(0f, moveFactor * 12, 0f);
                        else
                            viewMatrix.TranslationVector += new Vector3(0f, 0f, 1) * moveFactor * 12;
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
                        paused = !paused;
                        if (paused)
                            simTime.Stop();
                        else
                            simTime.Start();

                        // Pause or resume mesh animation
                        meshes.ForEach(m => {
                            if (m.Clock.IsRunning)
                                m.Clock.Stop();
                            else
                                m.Clock.Start();
                        });
                        updateText();
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
                                CullMode = CullMode.None,
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
                    case Keys.R:
                        // TODO: reset particles renderer
                        if (simTime.IsRunning)
                            simTime.Restart();
                        else
                            simTime.Reset();
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
                    case Keys.Add:
                        var increaseParticles = 10000;
                        if (shiftKey)
                            increaseParticles *= 10;
                        totalParticles += increaseParticles;

                        particleSystem.InitializeParticles(totalParticles, particleLifetime);

                        updateText();
                        break;
                    case Keys.Subtract:
                        var decreaseParticles = 10000;
                        if (shiftKey)
                            decreaseParticles *= 10;
                        totalParticles -= decreaseParticles;
                        totalParticles = Math.Max(totalParticles, 10000);
                        particleSystem.InitializeParticles(totalParticles, particleLifetime);
                        updateText();
                        break;
                    case Keys.Enter:
                        particleSystem.SwitchTexture();
                        break;
                    case Keys.Back:
                        if (shiftKey)
                        {
                            particleSystem.UseLightenBlend = !particleSystem.UseLightenBlend;
                        }
                        else
                        {
                            particleIndx++;
                            particleIndx = particleIndx % particleShaders.Count;
                        }
                        break;
                    case Keys.I:
                        keyToggles[Keys.I] = !keyToggles[Keys.I];
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
                } else if (e.Button == MouseButtons.Right)
                {
                    // Move the mouse cursor coordinates into the -1 to +1 range.
                    float pointX = ((2.0f * (float)e.X) / (float)Window.ClientSize.Width) - 1.0f;
                    float pointY = (((2.0f * (float)e.Y) / (float)Window.ClientSize.Height) - 1.0f) * -1.0f;
                    rightMouseClick = new Vector2(pointX, pointY);

                    var inverseViewProj = Matrix.Invert(Matrix.Multiply(viewMatrix, projectionMatrix));

                    var far = Vector3.TransformCoordinate(new Vector3(rightMouseClick, 1f), inverseViewProj);
                    var near = Vector3.TransformCoordinate(new Vector3(rightMouseClick, 0f), inverseViewProj);
                    //attractor.Z = 0;
                    particleSystem.Constants.Attractor = Vector3.Normalize(far - near) * 50;
                    particleSystem.UpdateConstants();
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
                    viewMatrix *= Matrix.RotationX(-xRotate * moveFactor);
                    viewMatrix *= Matrix.RotationY(-yRotate * moveFactor);

                    updateText();
                }
            };

            // Display instructions with initial values
            updateText();

            #endregion

            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            long elapsed = 0;
            long frames = 0;

            long elapsedShader = 0;
            float elapsedSinceGenerator = 0;
            
            
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
                context.ClearRenderTargetView(RenderTargetView, background);

                // Create viewProjection matrix
                var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);

                // Extract camera position from view
                var camPosition = Matrix.Transpose(Matrix.Invert(viewMatrix)).Column4;
                cameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

                var perFrame = new ConstantBuffers.PerFrame();
                perFrame.Light.Color = new Color(1f, 1f, 1f, 1.0f);
                var lightDir = Vector3.Transform(new Vector3(1f, -1f, -1f), worldMatrix);
                perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);// new Vector3(Vector3.Transform(new Vector3(1f, -1f, 1f), worldMatrix * Matrix.RotationAxis(Vector3.UnitY, time)).ToArray().Take(3).ToArray());

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

                // MESH
                if (particleIndx == 0)
                {
                    foreach (var m in meshes)
                    {
                        perObject.World = m.World * worldMatrix;
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);
                        // Provide the material constant buffer to the mesh renderer
                        m.PerMaterialBuffer = perMaterialBuffer;
                        m.PerArmatureBuffer = perArmatureBuffer;
                        m.Render();
                    }
                }

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
                    axisGrid.Render();
                    context.PixelShader.Set(prevPixelShader);
                }

                #region Update particle system

                // 1. Run Compute Shader to update particles
                if (simTime.IsRunning)
                {
                    particleSystem.Frame.FrameTime = ((float)simTime.Elapsed.TotalSeconds - particleSystem.Frame.Time);
                    particleSystem.Frame.Time = (float)simTime.Elapsed.TotalSeconds;
                    particleSystem.Update("Generator", particleShaders[particleIndx]);
                }
                // 2. Render the particles
                clock.Restart();
                particleSystem.Instanced = keyToggles[Keys.I];
                particleSystem.Render();
                clock.Stop();

                // Keep track of how long the dispatch calls are taking
                elapsed += particleSystem.LastDispatchTicks;
                elapsedShader += clock.ElapsedTicks;
                frames++;

                // Output core particle render statistics
                if (frames % 250 == 0)
                {
                    textRenderer.Text = string.Format("Particle: (CS:{0:F6} ms, Render:{1:F6} ms)\nCompute Shader: {4} (Backspace to cycle)\n{2} (I to toggle)\n{3} (Shift-Backspace to toggle)\nToggle texture <Enter>", (double)elapsed / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                        (double)elapsedShader / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                        keyToggles[Keys.I] ? "Instanced Vertex Shader" : "Geometry Shader",
                        particleSystem.UseLightenBlend ? "Lighten Blend" : "Darken Blend",
                        particleShaders[particleIndx]);
                }
                else if (frames > 250)
                {
                    frames = 0;
                    elapsed = 0;
                    elapsedShader = 0;
                }

                #endregion

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