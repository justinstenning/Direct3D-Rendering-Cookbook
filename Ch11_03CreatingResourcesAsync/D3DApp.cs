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

using Common;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch11_03CreatingResourcesAsync
{
    class D3DApp: Common.D3DAppSwapChainPanelTarget
    {
        public D3DApp(Windows.UI.Xaml.Controls.SwapChainPanel panel)
            : base(panel)
        {
            this.VSync = true;

            Color = Windows.UI.Color.FromArgb(SharpDX.Color.LightBlue.A, SharpDX.Color.LightBlue.R, SharpDX.Color.LightBlue.G, SharpDX.Color.LightBlue.B);
        }

        public Windows.UI.Color Color { get; set; }


        #region Shaders
        // The vertex layout for the IA
        InputLayout vertexLayout;

        // The vertex shader
        VertexShader vertexShader;

        // The pixel shader
        PixelShader pixelShader;
        #endregion

        #region Pipeline state
        
        // Our configured depth stencil state
        DepthStencilState depthStencilState;

        RasterizerState rsCullBack;

        #endregion

        #region Constant buffers

        // A buffer that will be used to update the worldViewProjection 
        // constant buffer of the vertex shader
        Buffer perObjectBuffer;

        // A buffer that will be used to update the lights
        Buffer perFrameBuffer;

        // A buffer that will be used to update object materials
        Buffer perMaterialBuffer;

        // A buffer that will be used to update object armature/skeleton
        Buffer perArmatureBuffer;

        #endregion

        #region Renderers
        List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
        Common.TextRenderer textRenderer;
        Common.FpsRenderer fpsRenderer;
        #endregion

        bool ResourcesLoaded = false;

        // Example using async
        protected async override void CreateDeviceDependentResources(Common.DeviceManager deviceManager)
        {
            ResourcesLoaded = false;
            base.CreateDeviceDependentResources(deviceManager);

            RemoveAndDispose(ref fpsRenderer);
            RemoveAndDispose(ref textRenderer);
            
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref vertexLayout);
            RemoveAndDispose(ref pixelShader);

            foreach (var m in meshRenderers)
            {   
                var m2 = m;
                RemoveAndDispose(ref m2);
            }
            meshRenderers.Clear();

            var device = deviceManager.Direct3DDevice;

            #region Compile shaders
            // Compile Vertex Shader and create vertex InputLayout
            using (var bytecode = await HLSLCompiler.CompileFromFileAsync(@"Shaders\VS.hlsl", "VSMain", "vs_5_0"))
            {
                vertexShader = new VertexShader(device, bytecode);
                vertexLayout = ToDispose(new InputLayout(device,
                   bytecode.GetPart(ShaderBytecodePart.InputSignatureBlob).Data,
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
                }));
            }

            // Compile pixel shader
            using (var bytecode = await HLSLCompiler.CompileFromFileAsync(@"Shaders\BlinnPhongPS.hlsl", "PSMain", "ps_5_0"))
                pixelShader = ToDispose(new PixelShader(device, bytecode));
            #endregion

            #region Create constant buffers
            // Create constant buffers

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

            #endregion

            #region Create pipeline state variables
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
                }));

            rsCullBack = ToDispose(new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = false,
                FillMode = FillMode.Solid,
            }));
            #endregion


            #region Create Renderers
            var meshes = await Mesh.LoadFromFileAsync("Character.cmo");
            meshRenderers.AddRange((from mesh in meshes
                             select ToDispose(new MeshRenderer(mesh))));
            await Task.Run(() =>
            {
                foreach (var m in meshRenderers)
                {
                    m.Initialize(this);
                    m.PerArmatureBuffer = perArmatureBuffer;
                    m.PerMaterialBuffer = perMaterialBuffer;

                    if (m.Mesh.Animations != null && m.Mesh.Animations.Any())
                        m.CurrentAnimation = m.Mesh.Animations.First().Value;
                    m.Clock.Start();
                }
            });

            textRenderer = ToDispose(new TextRenderer("Calibri", SharpDX.Color.Black, new Point(20, 100), 48, 800));
            textRenderer.Initialize(this);

            fpsRenderer = ToDispose(new FpsRenderer("Calibri", SharpDX.Color.CornflowerBlue, new Point(20, 20), 16));
            fpsRenderer.Initialize(this);
            #endregion

            clock.Start();
            ResourcesLoaded = true;
        }

        public FrustumCameraParams Camera;
        public Matrix WorldMatrix = Matrix.Identity;
        public Matrix ViewMatrix;
        public Matrix ProjectionMatrix;
        System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();

        protected override void CreateSizeDependentResources(D3DApplicationBase app)
        {
            base.CreateSizeDependentResources(app);

            // Initialize camera
            Camera.ZNear = 0.5f;
            Camera.ZFar = 100f;
            Camera.FOV = (float)Math.PI / 3f;
            Camera.AspectRatio = Width / (float)Height;

            // Reinitialize the projection matrix
            ProjectionMatrix = Matrix.PerspectiveFovRH(Camera.FOV, Camera.AspectRatio, Camera.ZNear, Camera.ZFar);
        }

        protected void SetContextState(DeviceContext1 context)
        {
            // Tell the IA what the vertices will look like
            context.InputAssembler.InputLayout = vertexLayout;

            // Set the vertex shader constant buffers
            context.VertexShader.SetConstantBuffer(0, perObjectBuffer);
            context.VertexShader.SetConstantBuffer(1, perFrameBuffer);
            context.VertexShader.SetConstantBuffer(2, perMaterialBuffer);
            context.VertexShader.SetConstantBuffer(3, perArmatureBuffer);

            // Set the pixel shader to run
            context.VertexShader.Set(vertexShader);

            // Set the pixel shader constant buffers
            context.PixelShader.SetConstantBuffer(1, perFrameBuffer);
            context.PixelShader.SetConstantBuffer(2, perMaterialBuffer);

            // Set the pixel shader to run
            context.PixelShader.Set(pixelShader);

            context.Rasterizer.State = rsCullBack;

            // Apply the depth stencil state
            context.OutputMerger.DepthStencilState = depthStencilState;
        }

        public override void Render()
        {
            if (!ResourcesLoaded)
                return;

            var context = this.DeviceManager.Direct3DContext;

            var vs = this.vertexShader;

            // OutputMerger targets must be set for every frame in Windows Store apps
            context.OutputMerger.SetTargets(this.DepthStencilView, this.RenderTargetView);

            context.ClearDepthStencilView(this.DepthStencilView, SharpDX.Direct3D11.DepthStencilClearFlags.Depth | SharpDX.Direct3D11.DepthStencilClearFlags.Stencil, 1.0f, 0);
            context.ClearRenderTargetView(this.RenderTargetView, new SharpDX.Color(Color.R, Color.G, Color.B, 1));

            // Set the context pipeline state
            SetContextState(context);

            ViewMatrix = Matrix.LookAtRH(Camera.Position, Camera.Position + Camera.LookAtDir, Vector3.UnitY);

            var viewProjection = ViewMatrix * ProjectionMatrix;
            // Extract camera position from view
            var camPosition = Matrix.Transpose(Matrix.Invert(ViewMatrix)).Column4;
            Camera.Position = new Vector3(camPosition.X, camPosition.Y, camPosition.Z);

            var perFrame = new ConstantBuffers.PerFrame();
            perFrame.CameraPosition = Camera.Position;
            perFrame.Light.Color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
            var lightDir = Vector3.Transform(new Vector3(1f, -1f, -1f), WorldMatrix);
            perFrame.Light.Direction = new Vector3(lightDir.X, lightDir.Y, lightDir.Z);
            context.UpdateSubresource(ref perFrame, perFrameBuffer);

            var perMaterial = new ConstantBuffers.PerMaterial();
            perMaterial.Ambient = new Color4(0.2f);
            perMaterial.Diffuse = SharpDX.Color.White;
            perMaterial.Emissive = new Color4(0);
            perMaterial.Specular = SharpDX.Color.White;
            perMaterial.SpecularPower = 20f;
            context.UpdateSubresource(ref perMaterial, perMaterialBuffer);

            //var scale = this.SwapChainPanel.CompositionScaleX;
            foreach (var m in meshRenderers)
            {
                var worldRotation = Matrix.Identity;// *Matrix.RotationAxis(Vector3.UnitY, clock.ElapsedMilliseconds / 1000.0f);
                var perObject = new ConstantBuffers.PerObject();
                perObject.World = m.World * WorldMatrix * worldRotation;
                perObject.WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(perObject.World));
                perObject.WorldViewProjection = perObject.World * viewProjection;
                perObject.Transpose();
                context.UpdateSubresource(ref perObject, perObjectBuffer);

                m.Render();
            }

            //textRenderer.Text = "Async resource loading";
            //textRenderer.Render();

            fpsRenderer.Render();

            //// Draw a circle.
            //var d2dContext = DeviceManager.Direct2DContext;
            //float radiusX = this.RenderTargetSize.Width / 3.0f;
            //float radiusY = this.RenderTargetSize.Height / 3.0f;

            //float strokeWidth = 10.0f *this.SwapChainPanel.CompositionScaleX;
            //var brush = new SharpDX.Direct2D1.SolidColorBrush(d2dContext, SharpDX.Color.Black);
            //d2dContext.BeginDraw();
            //using (var sc2 = SwapChain.QueryInterface<SwapChain2>())
            //{
            //    d2dContext.Transform = sc2.MatrixTransform;
            //}
            //d2dContext.DrawEllipse(new SharpDX.Direct2D1.Ellipse(new Vector2(this.RenderTargetSize.Width / 2.0f, this.RenderTargetSize.Height / 2.0f), radiusX, radiusY), brush, strokeWidth );
            //d2dContext.EndDraw();

            // Present the render result
            Present();
        }
    }
}
