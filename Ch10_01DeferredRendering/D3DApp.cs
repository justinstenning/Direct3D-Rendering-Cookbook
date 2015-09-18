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

namespace Ch10_01DeferredRendering
{
    public class D3DApp : Common.D3DApplicationDesktop
    {
        // The vertex shader
        VertexShader vertexShader;

        // The pixel shader
        PixelShader pixelShader;

        // A pixel shader that renders the depth (black closer, white further away)
        PixelShader depthPixelShader;

        // The Blinn-Phong shader
        PixelShader blinnPhongShader;

        VertexShader fillGBufferVS;
        PixelShader fillGBufferPS;

        // Debug GBuffer pixel shaders
        List<PixelShader> DebugGBuffer = new List<PixelShader>();
        // Debug GBuffer for Multi-sampled
        List<PixelShader> DebugGBufferMS = new List<PixelShader>();

        GeometryShader debugNormals;

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
            RemoveAndDispose(ref blinnPhongShader);

            RemoveAndDispose(ref debugNormals);

            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref perObjectBuffer);
            RemoveAndDispose(ref perFrameBuffer);
            RemoveAndDispose(ref perMaterialBuffer);
            RemoveAndDispose(ref perArmatureBuffer);

            RemoveAndDispose(ref depthStencilState);

            DebugGBuffer.ForEach(ps => RemoveAndDispose(ref ps));
            DebugGBuffer.Clear();

            DebugGBufferMS.ForEach(ps => RemoveAndDispose(ref ps));
            DebugGBufferMS.Clear();

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
                    // "BLENDINDICES"
                    new InputElement("BLENDINDICES", 0, Format.R32G32B32A32_UInt, 36, 0),
                    // "BLENDWEIGHT"
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
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\BlinnPhongPS.hlsl", "PSMain", "ps_5_0"))
                blinnPhongShader = ToDispose(new PixelShader(device, bytecode));

            using (var geomShaderByteCode = HLSLCompiler.CompileFromFile(@"Shaders\GS_DebugNormals.hlsl", "GSMain", "gs_5_0"))
                debugNormals = ToDispose(new GeometryShader(device, geomShaderByteCode));

            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "VSFillGBuffer", "vs_5_0"))
                fillGBufferVS = ToDispose(new VertexShader(device, bytecode));

            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\FillGBuffer.hlsl", "PSFillGBuffer", "ps_5_0"))
            {
                fillGBufferPS = ToDispose(new PixelShader(device, bytecode));
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferDiffuse", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferDiffuse";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferNormalPacked", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferNormalPacked";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferEmissive", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferEmissive";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferSpecularPower", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferSpecularPower";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferSpecularInt", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferSpecularInt";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferDepth", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferDepth";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBuffer.hlsl", "GBufferPosition", "ps_5_0"))
            {
                DebugGBuffer.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBuffer.Last().DebugName = "GBufferPosition";
            }

            #region Debug GBuffer for multisampling

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferDiffuse", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferDiffuse";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferNormalPacked", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferNormalPacked";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferEmissive", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferEmissive";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferSpecularPower", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferSpecularPower";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferSpecularInt", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferSpecularInt";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferDepth", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferDepth";
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\DebugGBufferMS.hlsl", "GBufferPosition", "ps_5_0"))
            {
                DebugGBufferMS.Add(ToDispose(new PixelShader(device, bc)));
                DebugGBufferMS.Last().DebugName = "GBufferPosition";
            }
            #endregion


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

            // Back-face culling
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
            GBuffer gbuffer = ToDispose(new GBuffer(this.RenderTargetSize.Width, 
                this.RenderTargetSize.Height,
                new SampleDescription(1, 0),
                Format.R8G8B8A8_UNorm,
                Format.R32_UInt,
                Format.R8G8B8A8_UNorm));
            gbuffer.Initialize(this);

            GBuffer gbufferMS = ToDispose(new GBuffer(this.RenderTargetSize.Width, 
                this.RenderTargetSize.Height,
                new SampleDescription(4, 0),
                Format.R8G8B8A8_UNorm,
                Format.R32_UInt,
                Format.R8G8B8A8_UNorm));
            gbufferMS.Initialize(this);

            ScreenAlignedQuadRenderer saQuad = ToDispose(new ScreenAlignedQuadRenderer());
            saQuad.Initialize(this);

            #region Create renderers

            // Note: the renderers take care of creating their own 
            // device resources and listen for DeviceManager.OnInitialize

            // Create Light accumulator
            var sphereMesh = Common.Mesh.LoadFromFile("Sphere.cmo").FirstOrDefault();
            //var coneMesh = Common.Mesh.LoadFromFile("Cone.cmo").FirstOrDefault();

            var sphereRenderer = ToDispose(new MeshRenderer(sphereMesh));
            sphereRenderer.Initialize(this);
            //var coneRenderer = new MeshRenderer(coneMesh);
            //coneRenderer.Initialize(this);
            var lightRenderer = ToDispose(new LightRenderer(sphereRenderer, saQuad, gbuffer));
            var lightRendererMS = ToDispose(new LightRenderer(sphereRenderer, saQuad, gbufferMS));

            lightRenderer.Lights.Add(new PerLight
            {
                Color = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
                Position = new Vector3(1, 1, 1),
                Direction = new Vector3(0, 0, 1),
                Range = 10,
                Type = LightType.Ambient
            });
            //lightRenderer.Lights.Add(new PerLight
            //{
            //    Color = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
            //    Direction = new Vector3(0, -1, 1),
            //    Type = LightType.Directional
            //});

            lightRenderer.Lights.Add(new PerLight
            {
                Color = Color.LightPink,
                Position = new Vector3(22.4f, 2.6f, -8.98f),
                Range = 20,
                Type = LightType.Point
            });
            lightRenderer.Lights.Add(new PerLight
            {
                Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
                Position = new Vector3(0, 10, 1),
                Range = 10,
                Type = LightType.Point
            });
            var aLight = new PerLight
            {
                Color = new Color4(1.0f, 0.0f, 0.0f, 1.0f),
                Position = new Vector3(9.86f, 10.92f, -6.74f),
                Range = 20,
                Type = LightType.Point
            };
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Blue;
            aLight.Position.X = 2.32f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.LightYellow;
            aLight.Position.X = -1.0f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Cyan;
            aLight.Position.X = -5.11f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Lime;
            aLight.Position.X = -8.33f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.AliceBlue;
            aLight.Position.X = -12.55f;
            lightRenderer.Lights.Add(aLight);

            aLight.Position.Z = 5.02f;

            aLight.Color = Color.Red;
            aLight.Position.X = 9.86f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Blue;
            aLight.Position.X = 2.32f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.LightYellow;
            aLight.Position.X = -1.0f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Cyan;
            aLight.Position.X = -5.11f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.Lime;
            aLight.Position.X = -8.33f;
            lightRenderer.Lights.Add(aLight);
            aLight.Color = Color.AliceBlue;
            aLight.Position.X = -12.55f;
            lightRenderer.Lights.Add(aLight);

            //lightRenderer.Lights.Add(new PerLight
            //{
            //    Color = new Color4(1.0f, 1.0f, 1.0f, 1.0f),
            //    Position = new Vector3(-2, 1, 2),
            //    Direction = new Vector3(0, -1, 0),
            //    Range = 2f,
            //    Type = LightType.Spot,
            //    SpotInnerCosine = (float)Math.Cos(0.4) / 2.0f,
            //    SpotOuterCosine = (float)Math.Cos(1) / 2.0f,
            //});
            lightRenderer.Initialize(this);
            lightRendererMS.Lights.AddRange(lightRenderer.Lights);
            lightRendererMS.Initialize(this);

            // Create a axis-grid renderer
            var axisGrid = ToDispose(new AxisGridRenderer());
            axisGrid.Initialize(this);

            // Create and initialize the mesh renderer
            var loadedMesh = Common.Mesh.LoadFromFile("Sponza.cmo");
            //var loadedMesh = Common.Mesh.LoadFromFile("Scene.cmo");

            List<MeshRenderer> meshes = new List<MeshRenderer>();
            meshes.AddRange((from mesh in loadedMesh
                             select ToDispose(new MeshRenderer(mesh))));

            meshes.ForEach(m => m.Initialize(this));
            var meshWorld = Matrix.Identity;
            
            // Set the first animation as the current animation and start clock
            meshes.ForEach(m => {
                if (m.Mesh.Animations != null && m.Mesh.Animations.Any())
                    m.CurrentAnimation = m.Mesh.Animations.First().Value;
                m.Clock.Start();
            });

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
            var cameraPosition = new Vector3(15, 15, -1);
            var cameraTarget = new Vector3(-15, 5, -1); // Looking at the origin 0,0,0
            var cameraUp = Vector3.UnitY; // Y+ is Up

            // Prepare matrices
            // Create the view matrix from our camera position, look target and up direction
            var viewMatrix = Matrix.LookAtRH(cameraPosition, cameraTarget, cameraUp);
            viewMatrix.TranslationVector += new Vector3(0, -0.98f, 0);

            // Create the projection matrix
            /* FoV 60degrees = Pi/3 radians */
            // Aspect ratio (based on window size), Near clip, Far clip
            var projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 2f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 2f, 100f);
            };

            #region Rotation and window event handlers

            // Create a rotation vector to keep track of the rotation
            // around each of the axes
            var rotation = new Vector3(0.0f, 0.0f, 0.0f);

            var gbufferIndex = -1;

            Dictionary<Keys, bool> keyToggles = new Dictionary<Keys, bool>();
            keyToggles[Keys.Z] = false;
            keyToggles[Keys.F] = false;
            keyToggles[Keys.M] = false;
            keyToggles[Keys.PrintScreen] = false;

            // We will call this action to update text
            // for the text renderer
            Action updateText = () =>
            {
                textRenderer.Text =
                    String.Format(
                    "{0} (+/- to change)"+
                    "\n{1} (M to toggle)",
                    gbufferIndex > -1 ? DebugGBuffer[gbufferIndex].DebugName : gbufferIndex < -1 ? "Forward Render (1 light)" : "Deferred with 15 lights",
                    gbufferIndex == -2 || keyToggles[Keys.M] ? "Multisampled" : "No anti-aliasing"
                    );
            };

            // Support keyboard/mouse input to rotate or move camera view
            var moveFactor = 0.02f; // how much to change on each keypress
            var shiftKey = false;
            var ctrlKey = false;
            var background = Color.White;
            var showNormals = false;
            var enableNormalMap = true;
            Window.KeyDown += (s, e) =>
            {
                var context = DeviceManager.Direct3DContext;

                shiftKey = e.Shift;
                ctrlKey = e.Control;

                switch (e.KeyCode)
                {
                    case Keys.Add:
                        gbufferIndex++;

                        if (gbufferIndex >= DebugGBuffer.Count)
                            gbufferIndex = -2;
                        break;
                    case Keys.Subtract:
                        gbufferIndex--;

                        if (gbufferIndex < -2)
                            gbufferIndex = DebugGBuffer.Count - 1;
                        break;
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
                    case Keys.M:
                        keyToggles[Keys.M] = !keyToggles[Keys.M];
                        break;
                    case Keys.N:
                        if (!shiftKey)
                            showNormals = !showNormals;
                        else
                            enableNormalMap = !enableNormalMap;

                        break;
                    case Keys.D1:
                        context.PixelShader.Set(pixelShader);
                        break;
                    //case Keys.D2:
                    //    context.PixelShader.Set(lambertShader);
                    //    break;
                    //case Keys.D3:
                    //    context.PixelShader.Set(phongShader);
                    //    break;
                    case Keys.D4:
                        context.PixelShader.Set(blinnPhongShader);
                        break;
                    case Keys.Insert:
                        keyToggles[Keys.PrintScreen] = true;
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

                var boundingFrustum = new BoundingFrustum(viewProjection);

                // Extract camera position from view
                var camPosition = Matrix.Transpose(Matrix.Invert(viewMatrix)).Column4;
                cameraPosition = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

                // If Keys.CtrlKey is down, auto rotate viewProjection based on time
                var time = clock.ElapsedMilliseconds / 1000.0f;
                
                var perFrame = new ConstantBuffers.PerFrame();
                perFrame.Light.Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
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
                perMaterial.UVTransform = Matrix.Identity;
                context.UpdateSubresource(ref perMaterial, perMaterialBuffer);

                if (showNormals)
                    context.GeometryShader.Set(debugNormals);

                var perObject = new ConstantBuffers.PerObject();

                // MESH

                
                // Provide the material constant buffer to the mesh renderer
                context.VertexShader.Set(vertexShader);
                context.PixelShader.Set(blinnPhongShader);

                int drawn = 0;

                if (gbufferIndex == -2)
                {
                    // Regular forward rendering
                    meshes.ForEach((m) =>
                    {
                        perObject.World = m.World * worldMatrix;

                        var center = Vector3.Transform(m.Mesh.Extent.Center, perObject.World);
                        var offset = new Vector3(center.X, center.Y, center.Z) - m.Mesh.Extent.Center;
                        var cullCheck = boundingFrustum.Contains(new BoundingBox(m.Mesh.Extent.Min + offset, m.Mesh.Extent.Max + offset));
                        if (cullCheck == ContainmentType.Intersects || cullCheck == ContainmentType.Contains)
                        {
                            perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                            perObject.WorldViewProjection = perObject.World * viewProjection;
                            perObject.ViewProjection = viewProjection;
                            perObject.Transpose();
                            context.UpdateSubresource(ref perObject, perObjectBuffer);

                            m.EnableNormalMap = enableNormalMap;
                            m.PerMaterialBuffer = perMaterialBuffer;
                            m.PerArmatureBuffer = perArmatureBuffer;
                            m.Render();
                            drawn++;
                        }
                    });
                }
                else 
                {
                    GBuffer activeGBuffer = gbuffer;
                    LightRenderer activeLightRenderer = lightRenderer;
                    if (keyToggles[Keys.M])
                    {
                        activeGBuffer = gbufferMS;
                        activeLightRenderer = lightRendererMS;
                    }


                    // Deferred rendering
                    context.VertexShader.Set(fillGBufferVS);
                    context.PixelShader.Set(fillGBufferPS);
                    activeGBuffer.Clear(context, new Color(0, 0, 0, 0));
                    activeGBuffer.Bind(context);

                    meshes.ForEach((m) =>
                    {
                        perObject.World = m.World * worldMatrix;

                        //var center = Vector3.Transform(m.Mesh.Extent.Center, perObject.World);
                        //var offset = new Vector3(center.X, center.Y, center.Z) - m.Mesh.Extent.Center;
                        //var cullCheck = boundingFrustum.Contains(new BoundingBox(m.Mesh.Extent.Min + offset, m.Mesh.Extent.Max + offset));
                        //if (cullCheck == ContainmentType.Intersects || cullCheck == ContainmentType.Contains)
                        {
                            perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                            perObject.WorldViewProjection = perObject.World * viewProjection;
                            perObject.ViewProjection = viewProjection;
                            perObject.View = viewMatrix;
                            perObject.Projection = projectionMatrix;
                            perObject.InverseView = Matrix.Invert(viewMatrix);
                            perObject.InverseProjection = Matrix.Invert(projectionMatrix);
                            perObject.Transpose();
                            context.UpdateSubresource(ref perObject, perObjectBuffer);

                            m.EnableNormalMap = enableNormalMap;
                            m.PerMaterialBuffer = perMaterialBuffer;
                            m.PerArmatureBuffer = perArmatureBuffer;
                            m.Render();
                            drawn++;
                        }
                    });
                    activeGBuffer.Unbind(context);


                    if (gbufferIndex == -1)
                    {
                        // Light pass
                        sphereRenderer.PerMaterialBuffer = perMaterialBuffer;
                        sphereRenderer.PerArmatureBuffer = perArmatureBuffer;
                        //coneRenderer.PerMaterialBuffer = perMaterialBuffer;
                        //coneRenderer.PerArmatureBuffer = perArmatureBuffer;

                        context.PixelShader.SetConstantBuffer(0, perObjectBuffer);

                        perObject.ViewProjection = viewProjection;
                        perObject.View = viewMatrix;
                        perObject.Projection = projectionMatrix;
                        perObject.InverseView = Matrix.Invert(viewMatrix);
                        perObject.InverseProjection = Matrix.Invert(projectionMatrix);

                        activeLightRenderer.Debug = gbufferIndex;
                        activeLightRenderer.Clear(context);
                        activeLightRenderer.Frustum = new BoundingFrustum(projectionMatrix);
                        activeLightRenderer.PerObject = perObject;
                        activeLightRenderer.PerObjectBuffer = perObjectBuffer;
                        activeLightRenderer.Bind(context);
                        activeLightRenderer.Render();
                        activeLightRenderer.Unbind(context);
                        context.OutputMerger.SetRenderTargets(this.DepthStencilView, this.RenderTargetView);

                        // Render the light buffer
                        saQuad.Shader = null; // use default shader
                        saQuad.ShaderResources = new[] { activeLightRenderer.SRV };
                        saQuad.Render();
                        // reset shader resources
                        saQuad.ShaderResources = null;
                    }
                    else 
                    {
                        // Bind the output render targets
                        context.OutputMerger.SetRenderTargets(this.DepthStencilView, this.RenderTargetView);

                        // Render debug shaders
                        saQuad.ShaderResources = activeGBuffer.SRVs.ToArray().Concat(new[] { activeGBuffer.DSSRV }).ToArray();
                        perObject.World = worldMatrix;
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.ViewProjection = viewProjection;
                        perObject.View = viewMatrix;
                        perObject.InverseView = Matrix.Invert(viewMatrix);
                        perObject.Projection = projectionMatrix;
                        perObject.InverseProjection = Matrix.Invert(projectionMatrix);
                        perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);

                        context.PixelShader.SetConstantBuffer(0, perObjectBuffer);
                        if (keyToggles[Keys.M])
                            saQuad.Shader = DebugGBufferMS[gbufferIndex];
                        else
                            saQuad.Shader = DebugGBuffer[gbufferIndex];
                        saQuad.Render();
                    }
                }

                // Render FPS
                fps.Render();

                // Render instructions + position changes
                textRenderer.Render();

                if (keyToggles[Keys.PrintScreen])
                {
                    keyToggles[Keys.PrintScreen] = false;


                    //using (var resource = this.RenderTargetView.ResourceAs<Texture2D>())
                    //using (var bm = CopyTexture.SaveToBitmap(DeviceManager, resource))
                    //{
                    //    bm.Save("RT0.png");
                    //}

                    CopyTexture.SaveToFile(DeviceManager, lightRenderer.SRV.ResourceAs<Texture2D>(), "test.dds");

                    gbuffer.SaveToFiles();

                    //using (var resource = this.RenderTargetView.ResourceAs<Texture2D>())
                    //using (var bm0 = CopyTexture.SaveToWICBitmap(DeviceManager, resource))
                    //using (var bm1 = CopyTexture.SaveToWICBitmap(DeviceManager, gbuffer.RT1))
                    //using (var bm2 = CopyTexture.SaveToWICBitmap(DeviceManager, gbuffer.RT2))
                    //using (var bm3 = CopyTexture.SaveToWICBitmap(DeviceManager, gbuffer.DS0))
                    //using (var bm4 = CopyTexture.SaveToWICBitmap(DeviceManager, this.DepthBuffer))
                    //{
                        //CopyTexture.SaveToFile(DeviceManager, gbuffer.RT0, "RT0.dds");

                        //CopyTexture.SaveToFile(DeviceManager, resource, "test.dds");
                        //CopyTexture.SaveToFile(DeviceManager, this.DepthBuffer, "DepthBuffer.png", SharpDX.DXGI.Format.R32_Float);
                        //if (bm0 != null)
                        //    CopyTexture.SaveBitmap(DeviceManager, bm0, "RT0.png");
                        //if (bm1 != null)
                        //    CopyTexture.SaveBitmap(DeviceManager, bm1, "RT1.png");
                        //if (bm2 != null)
                        //    CopyTexture.SaveBitmap(DeviceManager, bm2, "RT2.png");
                        //if (bm3 != null)
                        //    CopyTexture.SaveBitmap(DeviceManager, bm3, "DS0.png");
                        //if (bm4 != null)
                        //    CopyTexture.SaveBitmap(DeviceManager, bm3, "DepthBuffer.png");
                    //}
                }

                // Present the frame
                Present();
            });
            #endregion
        }
    }
}