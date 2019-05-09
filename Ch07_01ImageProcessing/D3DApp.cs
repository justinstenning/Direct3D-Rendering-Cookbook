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

namespace Ch07_01ImageProcessing
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

            // Set our pixel constant buffers
            context.PixelShader.SetConstantBuffer(1, perFrameBuffer);
            context.PixelShader.SetConstantBuffer(2, perMaterialBuffer);

            // Set the pixel shader to run
            context.PixelShader.Set(blinnPhongShader);

            // Set our depth stencil state
            context.OutputMerger.DepthStencilState = depthStencilState;

            // Back-face culling
            context.Rasterizer.State = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            }));
        }

        #region Additional Image Processing resources
        Texture2D renderTarget;
        ShaderResourceView renderTargetSRV;
        Texture2D resolvedTarget;
        ShaderResourceView resolvedTargetSRV;
        ShaderResourceView altRenderTargetSRV;
        RenderTargetView altRenderTargetRTV;
        UnorderedAccessView altRenderTargetUAV;
        UnorderedAccessView altRenderTargetUIntUAV;

        ShaderResourceView alt2RenderTargetSRV;
        RenderTargetView alt2RenderTargetRTV;
        UnorderedAccessView alt2RenderTargetUAV;
        UnorderedAccessView alt2RenderTargetUIntUAV;
        #endregion

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            RemoveAndDispose(ref renderTargetSRV);
            RemoveAndDispose(ref resolvedTargetSRV);
            RemoveAndDispose(ref altRenderTargetRTV);
            RemoveAndDispose(ref altRenderTargetSRV);
            RemoveAndDispose(ref altRenderTargetUAV);
            RemoveAndDispose(ref altRenderTargetUIntUAV);
            RemoveAndDispose(ref renderTarget);
            RemoveAndDispose(ref resolvedTarget);

            base.CreateSizeDependentResources(app);

            #region Image Processing
            var device = this.DeviceManager.Direct3DDevice;
            renderTarget = ToDispose(RenderTargetView.ResourceAs<Texture2D>());
            {
                renderTarget.DebugName = "Render Target";
                renderTargetSRV = ToDispose(new ShaderResourceView(device, renderTarget));

                // Initialize a target to resolve multi-sampled render target
                var resolvedDesc = renderTarget.Description;
                resolvedDesc.BindFlags = BindFlags.ShaderResource;
                resolvedDesc.SampleDescription = new SampleDescription(1, 0);

                resolvedTarget = ToDispose(new Texture2D(device, resolvedDesc));
                {
                    resolvedTargetSRV = ToDispose(new ShaderResourceView(device, resolvedTarget));
                }

                // Create two alternative render targets
                // These are specially configured so they can be bound as SRV, RTV, and UAV
                // and also as a R32_UInt UAV (read/write to texture resource support)
                var rtDesc = renderTarget.Description;
                rtDesc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess;
                rtDesc.SampleDescription = new SampleDescription(1, 0);
                rtDesc.Format = Format.R8G8B8A8_Typeless; // so it can be bound to the R32_UInt UAV
                rtDesc.MipLevels = 0;

                using (var altTarget = new Texture2D(device, rtDesc))
                {
                    altRenderTargetRTV = ToDispose(new RenderTargetView(device, altTarget
                        , new RenderTargetViewDescription
                    {
                        Format = Format.R8G8B8A8_UNorm,
                        Dimension = RenderTargetViewDimension.Texture2D
                    }
                    ));
                    altRenderTargetSRV = ToDispose(new ShaderResourceView(device, altTarget
                        , new ShaderResourceViewDescription
                    {
                        Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                        Format = Format.R8G8B8A8_UNorm,
                        Texture2D = new ShaderResourceViewDescription.Texture2DResource
                        {
                            MipLevels = 1,
                            MostDetailedMip = 0
                        }
                    }
                    ));
                    altRenderTargetUAV = ToDispose(new UnorderedAccessView(device, altTarget
                            , new UnorderedAccessViewDescription
                        {
                            Dimension = UnorderedAccessViewDimension.Texture2D,
                            Format = Format.R8G8B8A8_UNorm,
                        }
                    ));
                    altRenderTargetUIntUAV = ToDispose(new UnorderedAccessView(device, altTarget
                        , new UnorderedAccessViewDescription
                    {
                        Dimension = UnorderedAccessViewDimension.Texture2D,
                        Format = Format.R32_UInt,
                    }
                    ));
                }

                using (var alt2Target = new Texture2D(device, rtDesc))
                {
                    alt2RenderTargetRTV = ToDispose(new RenderTargetView(device, alt2Target
                        , new RenderTargetViewDescription
                        {
                            Format = Format.R8G8B8A8_UNorm,
                            Dimension = RenderTargetViewDimension.Texture2D
                        }
                    ));
                    alt2RenderTargetSRV = ToDispose(new ShaderResourceView(device, alt2Target
                        , new ShaderResourceViewDescription
                        {
                            Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                            Format = Format.R8G8B8A8_UNorm,
                            Texture2D = new ShaderResourceViewDescription.Texture2DResource
                            {
                                MipLevels = 1,
                                MostDetailedMip = 0
                            }
                        }
                    ));
                    alt2RenderTargetUAV = ToDispose(new UnorderedAccessView(device, alt2Target
                            , new UnorderedAccessViewDescription
                            {
                                Dimension = UnorderedAccessViewDimension.Texture2D,
                                Format = Format.R8G8B8A8_UNorm,
                            }
                    ));
                    alt2RenderTargetUIntUAV = ToDispose(new UnorderedAccessView(device, alt2Target
                        , new UnorderedAccessViewDescription
                        {
                            Dimension = UnorderedAccessViewDimension.Texture2D,
                            Format = Format.R32_UInt,
                        }
                    ));
                }
            }
            #endregion
        }

        public override void Run()
        {
            #region Create renderers

            // Note: the renderers take care of creating their own 
            // device resources and listen for DeviceManager.OnInitialize

            List<string[]> ipShaders = new List<string[]>();
            ipShaders.Add(new[] { "DesaturateCS" });
            ipShaders.Add(new[] { "SaturateCS" });
            ipShaders.Add(new[] { "NegativeCS" });
            ipShaders.Add(new[] { "ContrastCS" });
            ipShaders.Add(new[] { "BrightnessCS" });
            ipShaders.Add(new[] { "SepiaCS" });
            ipShaders.Add(new[] { "BoxFilter3TapHorizontalCS", "BoxFilter3TapVerticalCS" });
            ipShaders.Add(new[] { "BoxFilter5TapHorizontalCS", "BoxFilter5TapVerticalCS" });
            ipShaders.Add(new[] { "BlurFilterHorizontalCS", "BlurFilterVerticalCS" });
            ipShaders.Add(new[] { "SobelEdgeCS" });
            ipShaders.Add(new[] { "ApproxMedianHorizontalCS", "ApproxMedianVerticalCS" });
            ipShaders.Add(new[] { "Median3x3TapSinglePassCS" });
            ipShaders.Add(new[] { "SobelEdgeOverlayCS" });
            int shaderIndex = 0;

            string[] images = new[]{
                "rendered scene",
                "Village.png",
                "Grass.jpg",
                "Sun.jpg",
                "Sand1.jpg",
                "Sand2.jpg",
                "QT1.jpg",
                "QT2.jpg",
                "QT3.jpg"
            };
            var imageIndex = 1;

            var ip32x32 = ToDispose(new ImageProcessingCS());
            ip32x32.ThreadsX = 32;
            ip32x32.ThreadsY = 32;
            ip32x32.LoadSourceImage(images[imageIndex]);
            ip32x32.Initialize(this);

            var ip16x4 = ToDispose(new ImageProcessingCS());
            ip16x4.ThreadsX = 16;
            ip16x4.ThreadsY = 4;
            ip16x4.LoadSourceImage(images[imageIndex]);
            ip16x4.Initialize(this);

            var ip32x4 = ToDispose(new ImageProcessingCS());
            ip32x4.ThreadsX = 32;
            ip32x4.ThreadsY = 4;
            ip32x4.LoadSourceImage(images[imageIndex]);
            ip32x4.Initialize(this);

            var ip128x4 = ToDispose(new ImageProcessingCS());
            ip128x4.ThreadsX = 128;
            ip128x4.ThreadsY = 4;
            ip128x4.LoadSourceImage(images[imageIndex]);
            ip128x4.Initialize(this);

            var screenAlignedQuad = ToDispose(new ScreenAlignedQuadRenderer());
            screenAlignedQuad.Initialize(this);

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

            // Set the camera position
            var cameraPosition = new Vector3(0, 0, 2);
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
            var lerpT = 1.0f;
            // We will call this action to update text
            // for the text renderer
            Action updateText = () =>
            {
                string shaders = String.Empty;
                foreach (var s in ipShaders[shaderIndex])
                    shaders += s + " ";
                textRenderer.Text =
                    String.Format("Rotation ({0}) (Up/Down Left/Right Wheel+-)\nView ({1}) (A/D, W/S, Shift+Wheel+-)"
                    + "\nShader {4}(PgUp/PgDn to change)"
                    + "\nImage: {3} (Ctrl +/- to change)"
                    + "\nLerp (t) = {2:#0.00} (+/- to change)"
                    //+ "\nPress Z to show/hide depth buffer - Press F to toggle wireframe"
                    //+ "\nPress 1-8 to switch shaders"
                        , rotation,
                        viewMatrix.TranslationVector,
                        lerpT,
                        images[imageIndex],
                        shaders
                        );
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
                    case Keys.PageUp:
                        shaderIndex++;
                        if (shaderIndex > ipShaders.Count - 1)
                            shaderIndex = 0;
                        updateText();
                        break;
                    case Keys.PageDown:
                        shaderIndex--;
                        if (shaderIndex < 0)
                            shaderIndex = ipShaders.Count - 1;
                        updateText();
                        break;
                    case Keys.Add:
                        var lerpAdd = 0.01f;
                        if (ctrlKey)
                        {
                            imageIndex++;
                            if (imageIndex > images.Length - 1)
                            {
                                imageIndex = 0;
                            }
                            else if (imageIndex != 0)
                            {
                                ip128x4.LoadSourceImage(images[imageIndex]);
                                ip32x32.LoadSourceImage(images[imageIndex]);
                                ip16x4.LoadSourceImage(images[imageIndex]);
                                ip32x4.LoadSourceImage(images[imageIndex]);
                            }
                            break;
                        }
                        if (shiftKey)
                            lerpAdd *= 10;
                        lerpT += lerpAdd;


                        updateText();
                        break;
                    case Keys.Subtract:
                        var lerpSub = 0.01f;

                        if (ctrlKey)
                        {
                            imageIndex--;
                            if (imageIndex < 0)
                            {
                                imageIndex = images.Length - 1;
                            }
                            if (imageIndex > 0)
                            {
                                ip128x4.LoadSourceImage(images[imageIndex]);
                                ip32x32.LoadSourceImage(images[imageIndex]);
                                ip16x4.LoadSourceImage(images[imageIndex]);
                                ip32x4.LoadSourceImage(images[imageIndex]);
                            }
                            break;
                        }

                        if (shiftKey)
                            lerpSub *= 10;
                        lerpT -= lerpSub;
                        updateText();
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

            StringBuilder stats = new StringBuilder();
            long[] elapsed = new long[5];
            //long elapsed = 0;
            long frames = 0;

            long elapsedPS = 0;

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

                foreach (var m in meshes)
                {
	                // MESH
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


                #region Image Processing

                // This step ensures that we have an image that is not 
                // multisampled as the CS cannot use multisampled textures
                // Note: resolvedTarget uses the same format as renderTarget
                if (renderTarget.Description.SampleDescription.Count > 1 || renderTarget.Description.SampleDescription.Quality > 0)
                {
                    context.ResolveSubresource(renderTarget, 0, resolvedTarget, 0, resolvedTarget.Description.Format);
                }
                else
                {
                    // Not multisampled, so just copy to the resolvedTarget
                    context.CopyResource(renderTarget, resolvedTarget);
                }


                // TODO: This should be moved outside of the render loop               
                TexturePingPong pp = new TexturePingPong();
                pp.SetSRVs(altRenderTargetSRV, alt2RenderTargetSRV);
                pp.SetUAVs(altRenderTargetUAV, alt2RenderTargetUAV);
                pp.SetUIntUAVs(altRenderTargetUIntUAV, alt2RenderTargetUIntUAV);

                ip128x4.Constants.LerpT = lerpT;
                ip32x32.Constants.LerpT = lerpT;
                ip16x4.Constants.LerpT = lerpT;
                ip32x4.Constants.LerpT = lerpT;
                
                // By default shaders are located in ImageProcessingCS.hlsl
                // other source files can be used by using the imageProcessing.CompileComputeShader function
                // Be sure to set the threadX/Y counts first

                // An array of shaders to apply
                string[] shaders = ipShaders[shaderIndex];// { "BlurFilterHorizontalCS", "BlurFilterVerticalCS" }; //"DesaturateCS" };// 

                // Median3x3TapSinglePassCS
                // SobelEdgeOverlayCS
                // SobelEdgeCS
                // SepiaCS
                // BrightnessCS
                // ContrastCS
                // NegativeCS
                // SaturateCS
                // DesaturateCS

                // Possible to control the thread count for each shader if necessary
                ImageProcessingCS.ComputeConfig[] shaderConfig = null;
                //new[] { // Horizontal filter
                //    new ImageProcessingCS.ComputeConfig { 
                //        Constants = imageProcessing.Constants,
                //        ThreadsX = 1024,
                //        ThreadsY = 1
                //    },  // Vertical filter
                //    new ImageProcessingCS.ComputeConfig { 
                //        Constants = imageProcessing.Constants,
                //        ThreadsX = 1,
                //        ThreadsY = 1024
                //    }, 
                //}

                // Ctrl +/- changes the imageIndex
                // Page Up/Down changes the shader code
                if (imageIndex > 0)
                {
                    // Process the loaded static image
                    //ip128x4.RunChainedCS(pp, shaders, shaderConfig);
                    //ip32x32.RunChainedCS(pp, shaders, shaderConfig);
                    ip16x4.RunChainedCS(pp, shaders, shaderConfig);
                    //ip32x4.RunChainedCS(pp, shaders, shaderConfig);
                }
                else
                {
                    // Process the currently rendered scene
                    //ip128x4.RunChainedCS(resolvedTargetSRV, pp, shaders, shaderConfig);
                    //ip32x32.RunChainedCS(resolvedTargetSRV, pp, shaders, shaderConfig);
                    ip16x4.RunChainedCS(resolvedTargetSRV, pp, shaders, shaderConfig);
                    //ip32x4.RunChainedCS(pp, shaders, shaderConfig);
                }

                // Run histogram shader and retrieve result
                //var histo = imageProcessing.Histogram();

                // Keep track of how long the dispatch calls are taking
                //elapsed[0] += ip128x4.LastDispatchTicks;
                //elapsed[1] += ip32x32.LastDispatchTicks;
                elapsed[2] += ip16x4.LastDispatchTicks;
                //elapsed[3] += ip32x4.LastDispatchTicks;

                clock.Restart();
                // Render the result to the render target with our screen-aligned quad
                context.PixelShader.SetShaderResource(0, pp.GetCurrentAsSRV());
                screenAlignedQuad.Render();
                elapsedPS += clock.ElapsedTicks;

                frames++;


                // Output core statistics
                // Warning!!! the timings for the CS are the accumulated Dispatch calls only, not setting / clearing resources etc..
                // A comparison with the screen-aligned pixel shader is provided - this way an operation can easily be tested for performance in both
                if (frames % 2500 == 0)
                {
                    string shader = String.Empty;
                    foreach (var s in ipShaders[shaderIndex])
                        shader += s + " ";
                    textRenderer.Text = string.Format("CS: ({0:F6} ms) - {2}\nPS: ({1:F6} ms)",
                        (double)elapsed[2] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                        (double)elapsedPS / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                        shader);

                    //textRenderer.Text = string.Format("CS:\n1024x1:{0:F6} ms)\n32x32:{1:F6} ms\n16x4:{2:F6} ms\n256x4:{3:F6} ms)\nPS: ({4:F6} ms)",
                    //    (double)elapsed[0] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[1] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[2] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[3] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsedPS / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0);

                    //stats.AppendLine(string.Format("{0:F9},{1:F9},{2:F9},{3:F9}", 
                    //    (double)elapsed[0] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[1] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[2] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[3] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0
                    //    ));

                    //System.IO.File.AppendAllText("stats.csv", string.Format("{0:F9},{1:F9},{2:F9},{3:F9}\r\n",
                    //    (double)elapsed[0] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[1] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[2] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0,
                    //    (double)elapsed[3] / (double)frames / System.Diagnostics.Stopwatch.Frequency * 1000.0
                    //    ));
                }
                else if (frames > 2500)
                {
                    frames = 0;
                    elapsed[0] = 0;
                    elapsed[1] = 0;
                    elapsed[2] = 0;
                    elapsed[3] = 0;
                    elapsedPS = 0;
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