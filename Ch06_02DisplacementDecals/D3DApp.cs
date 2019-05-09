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

namespace Ch06_02DisplacementDecals
{
    public class D3DApp : Common.D3DApplicationDesktop
    {
        // The vertex shader
        public VertexShader vertexShader;

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

        VertexShader tessellateVertexShader;
        HullShader tessellateTriIntegerShader;
        HullShader tessellateTriPow2Shader;
        HullShader tessellateTriFractionalEvenShader;
        HullShader tessellateTriFractionalOddShader;
        DomainShader tessellateTriDomainShader;

        DomainShader tessellatePhongDomainShader;

        HullShader pnTriIntegerShader;
        HullShader pnTriPow2Shader;
        HullShader pnTriFractionalEvenShader;
        HullShader pnTriFractionalOddShader;
        DomainShader pnTriDomainShader;

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

        // A buffer that is used to update the current displacement decal
        Buffer decalBuffer;

        ShaderResourceView decalDiffuse;
        ShaderResourceView decalDisplacement;
        ShaderResourceView decalNormal;

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

            RemoveAndDispose(ref tessellateVertexShader);
            RemoveAndDispose(ref tessellateTriIntegerShader);
            RemoveAndDispose(ref tessellateTriPow2Shader);
            RemoveAndDispose(ref tessellateTriFractionalEvenShader);
            RemoveAndDispose(ref tessellateTriFractionalOddShader);
            RemoveAndDispose(ref tessellateTriDomainShader);

            RemoveAndDispose(ref tessellatePhongDomainShader);

            RemoveAndDispose(ref debugNormals);

            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref perObjectBuffer);
            RemoveAndDispose(ref perFrameBuffer);
            RemoveAndDispose(ref perMaterialBuffer);
            RemoveAndDispose(ref perArmatureBuffer);

            RemoveAndDispose(ref decalBuffer);
            RemoveAndDispose(ref decalDiffuse);
            RemoveAndDispose(ref decalDisplacement);
            RemoveAndDispose(ref decalNormal);

            RemoveAndDispose(ref depthStencilState);

            // Get a reference to the Device1 instance and immediate context
            var device = deviceManager.Direct3DDevice;
            var context = deviceManager.Direct3DContext;


            // Compile and create the vertex shader and input layout
            using (var vertexShaderBytecode = HLSLCompiler.CompileFromFile(@"Shaders\VS.hlsl", "VSMain", "vs_5_0"))
            using (var vertexTessBytecode = HLSLCompiler.CompileFromFile(@"Shaders\VS.hlsl", "VSPassThruTessellate", "vs_5_0"))
            {
                vertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));
                tessellateVertexShader = ToDispose(new VertexShader(device, vertexTessBytecode));

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
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\DiffusePS.hlsl", "PSMain", "ps_5_0"))
                lambertShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the Lambert pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\BlinnPhongPS.hlsl", "PSMain", "ps_5_0"))
                blinnPhongShader = ToDispose(new PixelShader(device, bytecode));

            // Compile and create the Lambert pixel shader
            using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\PhongPS.hlsl", "PSMain", "ps_5_0"))
                phongShader = ToDispose(new PixelShader(device, bytecode));

            #region Tessellation Shaders

            using (var triIntegerBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellateTri.hlsl", "HS_TrianglesInteger", "hs_5_0", null))
            using (var triPow2Bytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellateTri.hlsl", "HS_TrianglesPow2", "hs_5_0", null))
            using (var triFractionalEvenBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellateTri.hlsl", "HS_TrianglesFractionalEven", "hs_5_0", null))
            using (var triFractionalOddBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellateTri.hlsl", "HS_TrianglesFractionalOdd", "hs_5_0", null))
            using (var triDomainShaderBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellateTri.hlsl", "DS_Triangles", "ds_5_0", null))
            {
                tessellateTriIntegerShader = ToDispose(new HullShader(device, triIntegerBytecode));
                tessellateTriPow2Shader = ToDispose(new HullShader(device, triPow2Bytecode));
                tessellateTriFractionalEvenShader = ToDispose(new HullShader(device, triFractionalEvenBytecode));
                tessellateTriFractionalOddShader = ToDispose(new HullShader(device, triFractionalOddBytecode));
                tessellateTriDomainShader = ToDispose(new DomainShader(device, triDomainShaderBytecode));
            }

            using (var phongDomainShaderBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePhong.hlsl", "DS_PhongTessellation", "ds_5_0", null))
            {
                tessellatePhongDomainShader = ToDispose(new DomainShader(device, phongDomainShaderBytecode));
            }

            using (var pnTriIntegerBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePNTri.hlsl", "HS_PNTrianglesInteger", "hs_5_0", null))
            using (var pnTriPow2Bytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePNTri.hlsl", "HS_PNTrianglesPow2", "hs_5_0", null))
            using (var pnTriFractionalEvenBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePNTri.hlsl", "HS_PNTrianglesFractionalEven", "hs_5_0", null))
            using (var pnTriFractionalOddBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePNTri.hlsl", "HS_PNTrianglesFractionalOdd", "hs_5_0", null))
            using (var pnTriDomainShaderBytecode = HLSLCompiler.CompileFromFile(@"Shaders\TessellatePNTri.hlsl", "DS_PNTriangles", "ds_5_0", null))
            {
                pnTriIntegerShader = ToDispose(new HullShader(device, pnTriIntegerBytecode));
                pnTriPow2Shader = ToDispose(new HullShader(device, pnTriPow2Bytecode));
                pnTriFractionalEvenShader = ToDispose(new HullShader(device, pnTriFractionalEvenBytecode));
                pnTriFractionalOddShader = ToDispose(new HullShader(device, pnTriFractionalOddBytecode));
                pnTriDomainShader = ToDispose(new DomainShader(device, pnTriDomainShaderBytecode));
            }

            using (var geomShaderByteCode = HLSLCompiler.CompileFromFile(@"Shaders\GS_DebugNormals.hlsl", "GSMain", "gs_5_0", null))
            {
                debugNormals = ToDispose(new GeometryShader(device, geomShaderByteCode));
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

            // Create the decal buffer
            decalBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<ConstantBuffers.DecalBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            // Load the decal textures
            decalDiffuse = ToDispose(TextureLoader.ShaderResourceViewFromFile(device, "Crater_Diffuse.png"));
            decalDisplacement = ToDispose(TextureLoader.ShaderResourceViewFromFile(device, "Crater_Displacement.png"));
            decalNormal = ToDispose(TextureLoader.ShaderResourceViewFromFile(device, "Crater_Normal.png"));

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
            context.VertexShader.SetConstantBuffer(0, perObjectBuffer);
            context.VertexShader.SetConstantBuffer(1, perFrameBuffer);
            context.VertexShader.SetConstantBuffer(2, perMaterialBuffer);
            context.VertexShader.SetConstantBuffer(3, perArmatureBuffer);

            // Set the vertex shader to run
            context.VertexShader.Set(vertexShader);

            // Set our hull shader constant buffers
            context.HullShader.SetConstantBuffer(0, perObjectBuffer); 
            context.HullShader.SetConstantBuffer(1, perFrameBuffer);

            // Set default Hull Shader
            context.HullShader.Set(tessellateTriIntegerShader);

            context.DomainShader.SetConstantBuffer(0, perObjectBuffer);
            context.DomainShader.SetConstantBuffer(1, perFrameBuffer);
            context.DomainShader.SetConstantBuffer(2, perMaterialBuffer);
            context.DomainShader.Set(tessellateTriDomainShader);

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

            // Add the decal buffer to the pixel, hull and domain shaders (it uses the 5th slot 0-indexed)
            context.HullShader.SetConstantBuffer(4, decalBuffer);
            context.DomainShader.SetConstantBuffer(4, decalBuffer);
            context.PixelShader.SetConstantBuffer(4, decalBuffer);

            // No culling
            context.Rasterizer.State = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None
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

            // Create a axis-grid renderer
            var axisGrid = ToDispose(new AxisGridRenderer());
            axisGrid.Initialize(this);

            // Create and initialize the mesh renderer
            var loadedMesh = Common.Mesh.LoadFromFile("Tree.cmo");
            loadedMesh.AddRange(Common.Mesh.LoadFromFile("Plane.cmo"));
            loadedMesh.AddRange(Common.Mesh.LoadFromFile("Cube.cmo"));

            List<DisplacedMeshRenderer> meshes = new List<DisplacedMeshRenderer>();
            meshes.AddRange((from mesh in loadedMesh
                             select ToDispose(new DisplacedMeshRenderer(mesh))));
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

            var triangle = new TriangleRenderer();
            triangle.Initialize(this);

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
            var projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);
            };

            #region Rotation and window event handlers

            // Create a rotation vector to keep track of the rotation
            // around each of the axes
            var rotation = new Vector3(0.0f, 0.0f, 0.0f);
            var tessellationFactor = 2f; // default to 5 
            var tessellationPartition = "Integer";
            var tessellationMethod = "Phong";
            var displacementScale = 0.02f;
            // We will call this action to update text
            // for the text renderer
            Action updateText = () =>
            {
                textRenderer.Text =
                    String.Format("Tessellation Factor: {0:#0.0} (+/- to change)"
                    + "\nPartitioning: {1} (F1,F2,F3,F4 to change)"
                    + "\nTessellation: {2} (F5,F6,F7)"
                    + "\nDisplacement: {3:#0.000} (Shift +/- to change)"
                    + "\nPress F to toggle wireframe | Shift+N toggle Normal Map | N show normals"
                    + "\nPress 1-4 to switch pixel shaders | Backspace to cycle meshes",
                        tessellationFactor,
                        tessellationPartition,
                        tessellationMethod,
                        displacementScale,
                        DeviceManager.Direct3DDevice.NativePointer);
            };

            Dictionary<Keys, bool> keyToggles = new Dictionary<Keys, bool>();
            keyToggles[Keys.Z] = false;
            keyToggles[Keys.F] = false;

            // Support keyboard/mouse input to rotate or move camera view
            var moveFactor = 0.02f; // how much to change on each keypress
            var shiftKey = false;
            var ctrlKey = false;
            var background = Color.White;
            var activeTriTessellator = tessellateTriIntegerShader;
            var activePnTriTessellator = pnTriIntegerShader;
            var usePhongTessellation = true;
            var usePNTessellation = false;
            var showNormals = false;
            var enableNormalMap = true;
            var meshIndex = 0;
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
                    case Keys.N:
                        if (!shiftKey)
                            showNormals = !showNormals;
                        else
                            enableNormalMap = !enableNormalMap;

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
                        var multiple = 1.0f;
                        if (ctrlKey)
                            multiple = 3.0f;
                        if (!shiftKey)
                        {
                            tessellationFactor += 0.2f * multiple;
                            tessellationFactor = Math.Min(tessellationFactor, 64);
                        }
                        else
                        {
                            displacementScale += 0.005f * multiple;
                        }
                        break;
                    case Keys.Subtract:
                        multiple = 1.0f;
                        if (ctrlKey)
                            multiple = 3.0f;
                        if (!shiftKey)
                        {
                            tessellationFactor -= 0.2f * multiple;
                            tessellationFactor = Math.Max(tessellationFactor, 0);
                        }
                        else
                        {
                            displacementScale -= 0.005f * multiple;
                        }
                        break;
                    case Keys.F1:
                        tessellationPartition = "Integer";
                        activeTriTessellator = tessellateTriIntegerShader;
                        activePnTriTessellator = pnTriIntegerShader;
                        break;
                    case Keys.F2:
                        tessellationPartition = "Pow2";
                        activeTriTessellator = tessellateTriPow2Shader;
                        activePnTriTessellator = pnTriPow2Shader;
                        break;
                    case Keys.F3:
                        tessellationPartition = "Fractional Even";
                        activeTriTessellator = tessellateTriFractionalEvenShader;
                        activePnTriTessellator = pnTriFractionalEvenShader;
                        break;
                    case Keys.F4:
                        tessellationPartition = "Fractional Odd";
                        activeTriTessellator = tessellateTriFractionalOddShader;
                        activePnTriTessellator = pnTriFractionalOddShader;
                        break;
                    case Keys.F5:
                        usePhongTessellation = false;
                        usePNTessellation = false;
                        tessellationMethod = "Tri + displacement";
                        break;
                    case Keys.F6:
                        usePhongTessellation = true;
                        usePNTessellation = false;
                        tessellationMethod = "Phong + displacement";
                        break;
                    case Keys.F7:
                        usePhongTessellation = false;
                        usePNTessellation = true;
                        tessellationMethod = "PN-Triangle + displacement";
                        break;
                    case Keys.Back:
                        meshIndex = (meshIndex + 1) % meshes.Count;
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

                // If Keys.CtrlKey is down, auto rotate viewProjection based on time
                var time = clock.ElapsedMilliseconds / 1000.0f;
                
                var perFrame = new ConstantBuffers.PerFrame();
                perFrame.Light.Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                var lightDir = Vector3.Transform(new Vector3(1f, -1f, -1f), worldMatrix);
                perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);// new Vector3(Vector3.Transform(new Vector3(1f, -1f, 1f), worldMatrix * worldRotation).ToArray().Take(3).ToArray());
                perFrame.CameraPosition = cameraPosition;
                perFrame.TessellationFactor = tessellationFactor;
                context.UpdateSubresource(ref perFrame, perFrameBuffer);

                // Set decal shader resource views
                //context.PixelShader.SetShaderResources(3, decalDiffuse, decalNormal);
                context.DomainShader.SetShaderResource(2, decalDisplacement);

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
                context.VertexShader.Set(tessellateVertexShader);
                context.HullShader.Set(activeTriTessellator);
                if (usePhongTessellation)
                {
                    context.DomainShader.Set(tessellatePhongDomainShader);
                }
                else if (usePNTessellation)
                {
                    context.HullShader.Set(activePnTriTessellator);
                    context.DomainShader.Set(pnTriDomainShader);
                }
                else
                    context.DomainShader.Set(tessellateTriDomainShader);

                var m = meshes[meshIndex];
                //meshes.ForEach((m) =>
                //{

                // Assign decal textures
                m.TextureViews[3] = decalDiffuse;
                m.TextureViews[4] = decalNormal;

                // Apply decal 
                var decal = new ConstantBuffers.DecalBuffer();
                decal.DecalDisplaceScale = 0.10f;
                var decalVectors = new Vector3[3];
                if (meshIndex != 1)
                {
                    Vector3.Orthonormalize(decalVectors, new[] { new Vector3(0, 0.5f, 0.5f), Vector3.UnitX, -Vector3.UnitY });
                    decal.DecalPosition = new Vector3(0, 1, 1);
                }
                else
                {
                    Vector3.Orthonormalize(decalVectors, new[] { new Vector3(0, 0.5f, 0.2f), Vector3.UnitX, Vector3.UnitZ });
                    decal.DecalPosition = new Vector3(0, 0, 0);
                }
                decal.DecalNormal = decalVectors[0];
                decal.DecalTangent = decalVectors[1]; // U-axis of tex
                decal.DecalBitangent = decalVectors[2]; // V-axis of tex

                decal.DecalRadius = 0.5f;
                context.UpdateSubresource(ref decal, decalBuffer);
                // TODO: set decal buffer on mouse click using "Picking"
                
                
                
                
                
                // Provide the material constant buffer to the mesh renderer
                perObject.World = m.World * worldMatrix;
                perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                perObject.WorldViewProjection = perObject.World * viewProjection;
                perObject.ViewProjection = viewProjection;
                perObject.Transpose();
                context.UpdateSubresource(ref perObject, perObjectBuffer);
				
                m.EnableNormalMap = enableNormalMap;
                m.DisplacementScale = displacementScale;
                m.DisplaceMidLevel = 0.5f;
                m.PerMaterialBuffer = perMaterialBuffer;
                m.PerArmatureBuffer = perArmatureBuffer;
                m.Render();

                if (showNormals)
                {
                    using (var prevPixelShader = context.PixelShader.Get())
                    {
                        perMaterial.HasTexture = 0;
                        perMaterial.UVTransform = Matrix.Identity;
                        context.UpdateSubresource(ref perMaterial, perMaterialBuffer);
                        context.PixelShader.Set(pixelShader);
                        
                        context.GeometryShader.Set(debugNormals);

                        m.Render();

                        context.PixelShader.Set(prevPixelShader);
                        context.GeometryShader.Set(null);
                    }
                }
                
                //});

                // TRIANGLE
                //triangle.Render();

                //// QUAD
                //context.VertexShader.Set(tessellateVertexShader);
                //context.HullShader.Set(activeQuadTessellator);
                //context.DomainShader.Set(tessellateQuadDomainShader);
                
                //quad.Render();

                // BEZIER
                //context.VertexShader.Set(tessellateVertexShader);
                //context.HullShader.Set(activeBezierTessellator);
                //context.DomainShader.Set(tessellateBezierDomainShader);

                //bezier.Render();


                // AXIS GRID
                context.VertexShader.Set(vertexShader);
                context.HullShader.Set(null);
                context.DomainShader.Set(null);
                context.GeometryShader.Set(null);

                using (var prevPixelShader = context.PixelShader.Get())
                {
                    perMaterial.HasTexture = 0;
                    perMaterial.UVTransform = Matrix.Identity;
                    context.UpdateSubresource(ref perMaterial, perMaterialBuffer);
                    context.PixelShader.Set(pixelShader);
                    perObject = new ConstantBuffers.PerObject();
                    perObject.World = worldMatrix;
                    perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                    perObject.WorldViewProjection = perObject.World * viewProjection;
                    perObject.ViewProjection = viewProjection;
                    perObject.Transpose();
                    context.UpdateSubresource(ref perObject, perObjectBuffer);
                    axisGrid.Render();
                    context.PixelShader.Set(prevPixelShader);
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