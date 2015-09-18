using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Ch09_02DynamicCubeMapping
{
    /// <summary>
    /// Represents a dynamic texture cube environment map (cube map)
    /// </summary>
    public class DynamicCubeMap: Common.RendererBase
    {
        // Represents the camera for a cube face
        // Note: the View matrix includes the current position
        //       e.g. Matrix.Transpose(Matrix.Invert(View)).Column4 == Position
        public struct CubeFaceCamera
        {
            public Matrix View;
            public Matrix Projection;
        }


        // The cube map texture
        Texture2D EnvMap;
        // The RTV for all cube map faces (for single pass)
        RenderTargetView EnvMapRTV;
        // The DSV for all cube map faces (for single pass)
        DepthStencilView EnvMapDSV;

        // The RTVs, one for each face of cubemap
        RenderTargetView[] EnvMapRTVs = new RenderTargetView[6];
        // The DSVs, one for each face of cubemap
        DepthStencilView[] EnvMapDSVs = new DepthStencilView[6];

        // The TextureCube SRV for use by the reflective mesh/renderer
        public ShaderResourceView EnvMapSRV;

        // The 'per environment map buffer' to be assigned to the geometry
        // shader stage when rendering the cubemap in single pass
        // This will contain the 6 ViewProjection matrices of the
        // cube map faces.
        public Buffer PerEnvMapBuffer;

        // The viewport based on Size x Size
        ViewportF Viewport;

        DeviceContext[] contextList;

        // The renderer instance that will use the environment reflection
        public RendererBase Reflector { get; set; }
        // The cameras for each face of the cube
        public CubeFaceCamera[] Cameras = new CubeFaceCamera[6];
        // The texture size (e.g. 256x256)
        public int Size { get; private set; }
        
        // Number of threads to use
        public int Threads { get; private set; }

        public DynamicCubeMap(int size = 256)
            : this(256, 1)
        {
        }

        public DynamicCubeMap(int size = 256, int threads = 1)
            : base()
        {
            // Set the cube map resolution (e.g. 256 x 256)
            Size = size;
            Threads = Math.Max(Math.Min(threads, 6), 0);
            contextList = new DeviceContext[Threads];
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref EnvMap);
            RemoveAndDispose(ref EnvMapSRV);
            RemoveAndDispose(ref EnvMapRTV);
            RemoveAndDispose(ref EnvMapDSV);
            RemoveAndDispose(ref PerEnvMapBuffer);

            EnvMapRTVs.ToList().ForEach((rtv) => RemoveAndDispose(ref rtv));
            EnvMapDSVs.ToList().ForEach((dsv) => RemoveAndDispose(ref dsv));
            contextList.ToList().ForEach((ctx) => RemoveAndDispose(ref ctx));

            var device = this.DeviceManager.Direct3DDevice;

            // Create the cube map TextureCube (array of 6 textures)
            var textureDesc = new Texture2DDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                Height = Size,
                Width = Size,
                ArraySize = 6, // 6-sides of the cube
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps | ResourceOptionFlags.TextureCube,
                SampleDescription = new SampleDescription(1, 0),
                MipLevels = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            EnvMap = ToDispose(new Texture2D(device, textureDesc));

            // Create the SRV for the texture cube
            var descSRV = new ShaderResourceViewDescription();
            descSRV.Format = textureDesc.Format;
            descSRV.Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.TextureCube;
            descSRV.TextureCube.MostDetailedMip = 0;
            descSRV.TextureCube.MipLevels = -1;
            EnvMapSRV = ToDispose(new ShaderResourceView(device, EnvMap, descSRV));            
            
            // Create the RTVs
            var descRTV = new RenderTargetViewDescription();
            descRTV.Format = textureDesc.Format;
            descRTV.Dimension = RenderTargetViewDimension.Texture2DArray;
            descRTV.Texture2DArray.MipSlice = 0;
            // 1. Single RTV for single pass rendering
            descRTV.Texture2DArray.FirstArraySlice = 0;
            descRTV.Texture2DArray.ArraySize = 6;
            EnvMapRTV = ToDispose(new RenderTargetView(device, EnvMap, descRTV));
            // 2. RTV for each of the 6 sides of the texture cube
            descRTV.Texture2DArray.ArraySize = 1;
            for (int i = 0; i < 6; i++)
            {
                descRTV.Texture2DArray.FirstArraySlice = i;
                EnvMapRTVs[i] = ToDispose(new RenderTargetView(device, EnvMap, descRTV));
            }

            // Create DSVs
            using (var depth = new Texture2D(device, new Texture2DDescription
            {
                Format = Format.D32_Float,
                BindFlags = BindFlags.DepthStencil,
                Height = Size,
                Width = Size,
                Usage = ResourceUsage.Default,
                SampleDescription = new SampleDescription(1, 0),
                CpuAccessFlags = CpuAccessFlags.None,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.TextureCube,
                ArraySize = 6 // 6-sides of the cube
            }))
            {
                var descDSV = new DepthStencilViewDescription();
                descDSV.Format = depth.Description.Format;
                descDSV.Dimension = DepthStencilViewDimension.Texture2DArray;
                descDSV.Flags = DepthStencilViewFlags.None;
                descDSV.Texture2DArray.MipSlice = 0;
                // 1. Single DSV for single pass rendering
                descDSV.Texture2DArray.FirstArraySlice = 0;
                descDSV.Texture2DArray.ArraySize = 6;
                EnvMapDSV = ToDispose(new DepthStencilView(device, depth, descDSV));
                // 2. Create DSV for each face
                descDSV.Texture2DArray.ArraySize = 1;
                for (var i = 0; i < 6; i++)
                {
                    descDSV.Texture2DArray.FirstArraySlice = i;
                    EnvMapDSVs[i] = ToDispose(new DepthStencilView(device, depth, descDSV));
                }
            }

            // Create the viewport
            Viewport = new Viewport(0, 0, Size, Size);

            // Initialize context List for threaded rendering
            // See UpdateCubeThreaded
            if (Threads == 1)
                contextList = null;
            else
            {
                contextList = new DeviceContext[Threads];
                for (var i = 0; i < Threads; i++)
                {
                    contextList[i] = ToDispose(new DeviceContext(device));
                }
            }

            // Create the per environment map buffer (to store the 6 ViewProjection matrices)
            PerEnvMapBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<Matrix>() * 6, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            PerEnvMapBuffer.DebugName = "PerEnvMapBuffer";
        }

        // Update camera position for cube faces
        public void SetViewPoint(Vector3 camera)
        {
            // The TextureCube Texture2D assumes the
            // following order of faces.
            
            // The LookAt targets for view matrices
            var targets = new[] {
                camera + Vector3.UnitX, // +X
                camera - Vector3.UnitX, // -X
                camera + Vector3.UnitY, // +Y
                camera - Vector3.UnitY, // -Y
                camera + Vector3.UnitZ, // +Z
                camera - Vector3.UnitZ  // -Z
            };
            // The "up" vector for view matrices
            var upVectors = new[] {
                Vector3.UnitY, // +X
                Vector3.UnitY, // -X
                -Vector3.UnitZ,// +Y
                +Vector3.UnitZ,// -Y
                Vector3.UnitY, // +Z
                Vector3.UnitY, // -Z
            };
            // Create view and projection matrix for each face
            for (int i = 0; i < 6; i++)
            {
                // To remain consistent we will use a right-handed coordinate system
                // However the TextureCube will be a little backwards unless we also scale -1 on the X-axis
                // this results in needing the vertex winding order flipped (as we have done in the geometry
                // shader). Or switch the culling from back-face to front-face (or use no culling).
                Cameras[i].View = Matrix.LookAtRH(camera, targets[i], upVectors[i]) * Matrix.Scaling(-1, 1, 1);
                Cameras[i].Projection = Matrix.PerspectiveFovRH((float)Math.PI * 0.5f, 1.0f, 0.1f, 100.0f);

                // A left-handed view works without the scaling, however this still requires the vertex
                // winding order fixed as our MeshRenderer uses a .CMO which is built for right-handed.
                //Cameras[i].View = Matrix.LookAtLH(camera, targets[i], upVectors[i]);
                //Cameras[i].Projection = Matrix.PerspectiveFovLH((float)Math.PI * 0.5f, 1.0f, 0.1f, 100.0f);
            }
        }

        /// <summary>
        /// Render the 6-sides of the cube map individually
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderScene"></param>
        public void Update(DeviceContext context, Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            // Don't render the reflector itself
            if (Reflector != null)
            {
                Reflector.Show = false;
            }

            // Use "renderScene" to render the scene for each face of the cubemap
            for (var i = 0; i < 6; i++)
            {
                UpdateCubeFace(context, i, renderScene);
            }

            // Re-enable the Reflector
            if (Reflector != null)
            {
                Reflector.Show = true;
            }
        }

        private void UpdateCubeFace(DeviceContext context, int index, Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            // Prepare pipeline
            context.ClearState();
            context.OutputMerger.SetRenderTargets(EnvMapDSVs[index], EnvMapRTVs[index]);
            context.Rasterizer.SetViewport(Viewport);

            // Render the scene using the view, projection, RTV and DSV of this cube face
            renderScene(context, Cameras[index].View, Cameras[index].Projection, EnvMapRTVs[index], EnvMapDSVs[index], this);

            // Unbind the RTV and DSV
            context.OutputMerger.ResetTargets();
            // Prepare the SRV mip levels
            context.GenerateMips(EnvMapSRV);
        }

        /// <summary>
        /// Update the 6-sides of the cube map using a single pass via Geometry shader instancing with the provided context
        /// </summary>
        /// <param name="context">The context to render within</param>
        /// <param name="renderScene">The method that will render the scene</param>
        public void UpdateSinglePass(DeviceContext context, Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            // Don't render the reflector itself
            if (Reflector != null)
            {
                Reflector.Show = false;
            }

            // Prepare pipeline
            context.OutputMerger.SetRenderTargets(EnvMapDSV, EnvMapRTV);
            context.Rasterizer.SetViewport(Viewport);

            // Prepare the view projections
            Matrix[] viewProjections = new Matrix[6];
            for (var i = 0; i < 6; i++)
                viewProjections[i] = Matrix.Transpose(Cameras[i].View * Cameras[i].Projection);

            // Update per env map buffer with the ViewProjections
            context.UpdateSubresource(viewProjections, PerEnvMapBuffer);
            
            // Assign the per environment map buffer to the GS stage at slot 4
            context.GeometryShader.SetConstantBuffer(4, PerEnvMapBuffer);

            // Render the scene using the view, projection, RTV and DSV
            renderScene(context, Cameras[0].View, Cameras[0].Projection, EnvMapRTV, EnvMapDSV, this);

            // Unbind the RTV and DSV
            context.OutputMerger.ResetTargets();
            // Prepare the SRV mip levels
            context.GenerateMips(EnvMapSRV);

            // Re-enable the Reflector renderer
            if (Reflector != null)
            {
                Reflector.Show = true;
            }
        }

        /// <summary>
        /// Example of how to render the 6-sides within threads. Note: this is not compatible with running threads for the mesh renderers in
        /// the renderScene action.
        /// </summary>
        /// <param name="renderScene">The method that will render the scene</param>
        public void UpdateThreaded(Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DynamicCubeMap> renderScene)
        {
            // Don't render the reflector itself
            if (Reflector != null)
            {
                Reflector.Show = false;
            }

            var contexts = contextList ?? new DeviceContext[] { this.RenderContext };
            CommandList[] commands = new CommandList[contexts.Length];
            int batchSize = 6 / contexts.Length;

            Task[] tasks = new Task[contexts.Length];

            for (var i = 0; i < contexts.Length; i++)
            {
                var contextIndex = i;

                tasks[i] = Task.Run(() => {
                    var context = contexts[contextIndex];
                    
                    int startIndex = batchSize * contextIndex;
                    int endIndex = Math.Min(startIndex + batchSize, 5);
                    if (contextIndex == contexts.Length - 1)
                        endIndex = 5;

                    // Use "draw" to render the scene for each face of the cubemap
                    for (var j = startIndex; j <= endIndex; j++)
                    {
                        UpdateCubeFace(context, j, renderScene);
                    }

                    if (context.TypeInfo == DeviceContextType.Deferred)
                        commands[contextIndex] = ToDispose(context.FinishCommandList(false));
                });
            }
            Task.WaitAll(tasks);

            // Execute command lists (if any)
            for (var i = 0; i < contexts.Length; i++)
            {
                if (contexts[i].TypeInfo == DeviceContextType.Deferred && commands[i] != null)
                {
                    DeviceManager.Direct3DDevice.ImmediateContext.ExecuteCommandList(commands[i], false);
                    commands[i].Dispose();
                    commands[i] = null;
                }
            }

            // Re-enable the Reflector
            if (Reflector != null)
            {
                Reflector.Show = true;
            }
        }

        protected override void DoRender()
        {
            throw new NotImplementedException("Use Update/UpdateSinglePass instead.");
        }
    }
}
