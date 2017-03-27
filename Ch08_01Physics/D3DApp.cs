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
using BulletSharp;

namespace Ch08_01Physics
{
    public class D3DApp : Common.D3DApplicationDesktop
    {
        // The vertex shader
        VertexShader vertexShader;
        VertexShader waterVertexShader;

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
            RemoveAndDispose(ref lambertShader);
            RemoveAndDispose(ref blinnPhongShader);
            RemoveAndDispose(ref phongShader);

            RemoveAndDispose(ref debugNormals);

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

            using (var geomShaderByteCode = HLSLCompiler.CompileFromFile(@"Shaders\GS_DebugNormals.hlsl", "GSMain", "gs_5_0", null))
            {
                debugNormals = ToDispose(new GeometryShader(device, geomShaderByteCode));
            }

            using (var bc = HLSLCompiler.CompileFromFile(@"Shaders\WaterVS.hlsl", "VSMain", "vs_5_0"))
                waterVertexShader = ToDispose(new VertexShader(device, bc));

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
                CullMode = CullMode.None,
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
            var loadedMesh = Common.Mesh.LoadFromFile("PhysicsScene1.cmo");
            List<MeshRenderer> meshes = new List<MeshRenderer>();
            meshes.AddRange(from mesh in loadedMesh
                             select ToDispose(new MeshRenderer(mesh)));
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

            
            loadedMesh = Common.Mesh.LoadFromFile("SubdividedPlane.cmo");
            var waterMesh = ToDispose(new MeshRenderer(loadedMesh.First()));
            waterMesh.Initialize(this);

            loadedMesh = Common.Mesh.LoadFromFile("Bataux.cmo");
            List<MeshRenderer> shipMeshes = new List<MeshRenderer>();
            shipMeshes.AddRange((from mesh in loadedMesh
                                 select ToDispose(new MeshRenderer(mesh))));
            foreach (var m in shipMeshes)
            {
                m.Initialize(this);
                m.World = Matrix.Scaling(3) * Matrix.RotationAxis(Vector3.UnitY, -1.57079f);
            }

            //var anchor = new SphereRenderer(0.05f);
            //anchor.Initialize(this);
            //var anchorWorld = Matrix.Identity;

            //var sphere = new SphereRenderer();
            //sphere.Initialize(this);
            //var sphereWorld = Matrix.Identity;

            // Create and initialize a Direct2D FPS text renderer
            var fps = ToDispose(new Common.FpsRenderer("Calibri", Color.CornflowerBlue, new Point(8, 8), 16));
            fps.Initialize(this);

            // Create and initialize a general purpose Direct2D text renderer
            // This will display some instructions and the current view and rotation offsets
            var textRenderer = ToDispose(new Common.TextRenderer("Calibri", Color.CornflowerBlue, new Point(8, 40), 12));
            textRenderer.Initialize(this);

            #endregion

            #region Initialize physics engine

            CollisionConfiguration defaultConfig = new DefaultCollisionConfiguration();
            ConstraintSolver solver = new SequentialImpulseConstraintSolver();
            BulletSharp.Dispatcher dispatcher = new CollisionDispatcher(defaultConfig);
            BroadphaseInterface broadphase = new DbvtBroadphase();
            DynamicsWorld world = null;
            Action initializePhysics = () =>
            {
                RemoveAndDispose(ref world);
                world = ToDispose(new BulletSharp.DiscreteDynamicsWorld(dispatcher, broadphase, solver, defaultConfig));
                world.Gravity = new BulletSharp.Math.Vector3(0, -10, 0);

                // For each mesh, create a RigidBody and add to "world" for simulation
                meshes.ForEach(m =>
                {
                    // We use the name of the mesh to determine the correct body
                    if (String.IsNullOrEmpty(m.Mesh.Name))
                        return;

                    var name = m.Mesh.Name.ToLower();
                    var extent = m.Mesh.Extent;

                    BulletSharp.CollisionShape shape;

                    #region Create collision shape
                    if (name.Contains("box") || name.Contains("cube"))
                    {
                        // Assumes the box/cube has an axis-aligned neutral orientation
                        shape = new BulletSharp.BoxShape(
                            Math.Abs(extent.Max.Z - extent.Min.Z) / 2.0f,
                            Math.Abs(extent.Max.Y - extent.Min.Y) / 2.0f,
                            Math.Abs(extent.Max.X - extent.Min.X) / 2.0f);
                    }
                    else if (name.Contains("sphere"))
                    {
                        shape = new BulletSharp.SphereShape(extent.Radius);
                    }
                    else // use mesh vertices directly
                    {
                        // for each SubMesh, retrieve the vertex and index buffers
                        // to create a TriangleMeshShape for collision detection.
                        List<Vector3> vertices = new List<Vector3>();
                        List<int> indices = new List<int>();
                        int vertexOffset = 0;
                        foreach (var sm in m.Mesh.SubMeshes)
                        {
                            vertexOffset += vertices.Count;
                            indices.AddRange(
                                (from indx in m.Mesh.IndexBuffers[(int)sm.IndexBufferIndex]
                                select vertexOffset + (int)indx));
                            vertices.AddRange(
                                (from v in m.Mesh
                                .VertexBuffers[(int)sm.VertexBufferIndex]
                                select v.Position - extent.Center));
                        }
                        // Create the collision shape
                        var iva = new BulletSharp.TriangleIndexVertexArray(indices.ToArray(), vertices.ToArray().ToBulletVector3Array());
                        shape = new BulletSharp.BvhTriangleMeshShape(iva, true);
                    }
                    #endregion

                    m.World = Matrix.Identity; // Reset mesh location
                    float mass; Vector3 vec;
                    BulletSharp.Math.Vector3 bsVec;
                    shape.GetBoundingSphere(out bsVec, out mass);
                    vec = bsVec.ToSharpDXVector3();
                    var body = new BulletSharp.RigidBody(
                        new BulletSharp.RigidBodyConstructionInfo(name.Contains("static") ? 0 : mass, 
                            new MeshMotionState(m),
                            shape, shape.CalculateLocalInertia(mass)));
                    if (body.IsStaticObject)
                    {
                        body.Restitution = 1f;
                        body.Friction = 0.4f;
                    }
                    // Add to the simulation
                    world.AddRigidBody(body);
                });

#if DEBUG
                world.DebugDrawer = ToDispose(new PhysicsDebugDraw(this.DeviceManager));
                world.DebugDrawer.DebugMode = DebugDrawModes.DrawAabb | DebugDrawModes.DrawWireframe;
#endif
            };
            initializePhysics();


            // Newton's Cradle

            //var box = new Jitter.Dynamics.RigidBody(new Jitter.Collision.Shapes.BoxShape(7, 1, 2));
            //box.Position = new Jitter.LinearMath.JVector(0, 8, 0);
            //world.AddBody(box);
            //box.IsStatic = true;

            //var anchorBody = new Jitter.Dynamics.RigidBody(new Jitter.Collision.Shapes.SphereShape(0.05f));
            //anchorBody.Position = new Jitter.LinearMath.JVector(0, 4, 0);
            //world.AddBody(anchorBody);
            //anchorBody.IsStatic = true;

            //for (var bodyCount = -3; bodyCount < 4; bodyCount++)
            //{
            //    var testBody = new Jitter.Dynamics.RigidBody(new Jitter.Collision.Shapes.SphereShape(0.501f));
            //    testBody.Position = new Jitter.LinearMath.JVector(bodyCount, 0, 0);

            //    world.AddBody(testBody);

            //    world.AddConstraint(new Jitter.Dynamics.Constraints.PointPointDistance(box, testBody,
            //        testBody.Position + Jitter.LinearMath.JVector.Up * 8f + Jitter.LinearMath.JVector.Forward * 3f + Jitter.LinearMath.JVector.Down * 0.5f,
            //        testBody.Position) { Softness = 1.0f, BiasFactor = 0.8f });

            //    world.AddConstraint(new Jitter.Dynamics.Constraints.PointPointDistance(box, testBody,
            //        testBody.Position + Jitter.LinearMath.JVector.Up * 8f + Jitter.LinearMath.JVector.Backward * 3f + Jitter.LinearMath.JVector.Down * 0.5f,
            //        testBody.Position) { Softness = 1.0f, BiasFactor = 0.8f });

            //    testBody.Material.Restitution = 1.0f;
            //    testBody.Material.StaticFriction = 1.0f;
            //}

            #endregion

            // Initialize the world matrix
            var worldMatrix = Matrix.Identity;

            // Set the camera position slightly behind (z)
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
            var projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);

            // Maintain the correct aspect ratio on resize
            Window.Resize += (s, e) =>
            {
                projectionMatrix = Matrix.PerspectiveFovRH((float)Math.PI / 3f, Width / (float)Height, 0.1f, 100f);
            };

            bool debugDraw = false;
            bool paused = false;

            var simTime = new System.Diagnostics.Stopwatch();
            simTime.Start();
            float time = 0.0f;
            float timeStep = 0.0f;

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
                    + "\nPhysics debug draw: {3} (E to toggle)"
                    + "\nBackspace: toggle between Physics and Waves",
                        rotation,
                        viewMatrix.TranslationVector,
                        simTime.Elapsed.TotalSeconds,
                        debugDraw);
            };

            Dictionary<Keys, bool> keyToggles = new Dictionary<Keys, bool>();
            keyToggles[Keys.Z] = false;
            keyToggles[Keys.F] = false;
            keyToggles[Keys.Back] = false;

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
                    case Keys.N:
                        if (!shiftKey)
                            showNormals = !showNormals;
                        else
                            enableNormalMap = !enableNormalMap;
						break;
                    case Keys.E:
                        debugDraw = !debugDraw;
                        break;
                    case Keys.R:
                        
                        //world = new Jitter.World(new Jitter.Collision.CollisionSystemSAP());
                        initializePhysics();
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
                    case Keys.Back:
                        keyToggles[Keys.Back] = !keyToggles[Keys.Back];
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
                // Update simulation, at 60fps
                if (!paused)
                {
                    if ((float)simTime.Elapsed.TotalSeconds < time)
                    {
                        time = 0;
                        timeStep = 0;
                    }
                    timeStep = ((float)simTime.Elapsed.TotalSeconds - time);
                    time = (float)simTime.Elapsed.TotalSeconds;
                    world.StepSimulation(timeStep, 7);
                    // For how to choose the maxSubSteps see:
                    // http://www.bulletphysics.org/mediawiki-1.5.8/index.php/Stepping_The_World
                }

                updateText();
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
                perFrame.Light.Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                var lightDir = Vector3.Transform(new Vector3(1f, -1f, -1f), worldMatrix);
                perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);
                perFrame.CameraPosition = cameraPosition;
                perFrame.Time = (float)simTime.Elapsed.TotalSeconds; // Provide simulation time to shader
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

                if (!keyToggles[Keys.Back])
                {

                    meshes.ForEach((m) =>
                    {
                        perObject.World = m.World * worldMatrix;
                        // Provide the material constant buffer to the mesh renderer
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.ViewProjection = viewProjection;
					    perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);

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
                    });

                    if (debugDraw)
                    {
                        perObject.World = Matrix.Identity;
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.ViewProjection = viewProjection;
                        perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);

                        (world.DebugDrawer as PhysicsDebugDraw).DrawDebugWorld(world);
                        context.VertexShader.Set(vertexShader);
                        context.PixelShader.Set(pixelShader);
                        context.InputAssembler.InputLayout = vertexLayout;
                    }
                }
                else
                {
                    perObject.World = waterMesh.World * worldMatrix;
                    perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                    perObject.WorldViewProjection = perObject.World * viewProjection;
                    perObject.ViewProjection = viewProjection;					
                    perObject.Transpose();
                    context.UpdateSubresource(ref perObject, perObjectBuffer);

                    waterMesh.EnableNormalMap = enableNormalMap;
                    waterMesh.PerMaterialBuffer = perMaterialBuffer;
                    waterMesh.PerArmatureBuffer = perArmatureBuffer;

                    context.VertexShader.Set(waterVertexShader);
                    waterMesh.Render();

                    if (showNormals)
                    {
                        using (var prevPixelShader = context.PixelShader.Get())
                        {
                            perMaterial.HasTexture = 0;
                            perMaterial.UVTransform = Matrix.Identity;
                            context.UpdateSubresource(ref perMaterial, perMaterialBuffer);
                            context.PixelShader.Set(pixelShader);

                            context.GeometryShader.Set(debugNormals);

                            waterMesh.Render();

                            context.PixelShader.Set(prevPixelShader);
                            context.GeometryShader.Set(null);
                        }
                    }

                    context.VertexShader.Set(vertexShader);

                    foreach (var m in shipMeshes)
                    {
                        perObject.World = m.World * worldMatrix;
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);
                        // Provide the material constant buffer to the mesh renderer
                        perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                        perObject.WorldViewProjection = perObject.World * viewProjection;
                        perObject.ViewProjection = viewProjection;
                        perObject.Transpose();
                        context.UpdateSubresource(ref perObject, perObjectBuffer);

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
                    }
                }

                perMaterial.Ambient = new Color4(0.2f);
                perMaterial.Diffuse = Color.White;
                perMaterial.Emissive = new Color4(0);
                perMaterial.Specular = Color.White;
                perMaterial.SpecularPower = 20f;
                perMaterial.UVTransform = Matrix.Identity;
                context.UpdateSubresource(ref perMaterial, perMaterialBuffer);

                // AXIS GRID
                context.HullShader.Set(null);
                context.DomainShader.Set(null);
                context.GeometryShader.Set(null);

                using (var prevPixelShader = context.PixelShader.Get())
                using (var prevVertexShader = context.VertexShader.Get())
                {
                    context.VertexShader.Set(vertexShader);
                    context.PixelShader.Set(pixelShader);
                    perObject.World = worldMatrix;
                    perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                    perObject.WorldViewProjection = perObject.World * viewProjection;
                    perObject.ViewProjection = viewProjection;
                    perObject.Transpose();
                    context.UpdateSubresource(ref perObject, perObjectBuffer);
                    axisGrid.Render();
                    context.PixelShader.Set(prevPixelShader);
                    context.VertexShader.Set(prevVertexShader);
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