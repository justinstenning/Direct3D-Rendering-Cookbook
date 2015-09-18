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

namespace Ch09_03DualParaboloidMapping
{
    /// <summary>
    /// Represents a dynamic dual paraboloid environment map
    /// </summary>
    public class DualParaboloidMap: Common.RendererBase
    {
        public struct PerEnvMap
        {
            public Matrix View;
            public float NearClip;
            public float FarClip;
            Vector2 _padding0;
        }

        public PerEnvMap DualMapView;

        // The cube map texture
        Texture2D EnvMap;
        // The RTV for all cube map faces (for single pass)
        RenderTargetView EnvMapRTV;
        // The DSV for all cube map faces (for single pass)
        DepthStencilView EnvMapDSV;

        // The TextureCube SRV for use by the reflective mesh/renderer
        public ShaderResourceView EnvMapSRV;

        // The 'per paraboloid map buffer' to be assigned to the vertex
        // and pixel shader stages when rendering the paraboloid
        // or an object using a paraboloid reflection.
        public Buffer PerEnvMapBuffer;

        // The viewport based on Size x Size
        ViewportF Viewport;

        // The renderer instance that will use the environment reflection
        public RendererBase Reflector { get; set; }
        // The texture size (e.g. 256x256)
        public int Size { get; private set; }
        
        public DualParaboloidMap(int size = 256)
            : base()
        {
            // Set the resolution (e.g. 256 x 256)
            Size = size;
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref EnvMap);
            RemoveAndDispose(ref EnvMapSRV);
            RemoveAndDispose(ref EnvMapRTV);
            RemoveAndDispose(ref EnvMapDSV);
            RemoveAndDispose(ref PerEnvMapBuffer);

            var device = this.DeviceManager.Direct3DDevice;

            // Create the cube map TextureCube (array of 6 textures)
            var textureDesc = new Texture2DDescription()
            {
                Format = Format.R8G8B8A8_UNorm,
                Height = Size,
                Width = Size,
                ArraySize = 2, // 2-paraboloids
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                SampleDescription = new SampleDescription(1, 0),
                MipLevels = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            EnvMap = ToDispose(new Texture2D(device, textureDesc));

            // Create the SRV for the texture cube
            var descSRV = new ShaderResourceViewDescription();
            descSRV.Format = textureDesc.Format;
            descSRV.Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray;
            descSRV.Texture2DArray.MostDetailedMip = 0;
            descSRV.Texture2DArray.MipLevels = -1;
            descSRV.Texture2DArray.FirstArraySlice = 0;
            descSRV.Texture2DArray.ArraySize = 2;
            EnvMapSRV = ToDispose(new ShaderResourceView(device, EnvMap, descSRV));            
            
            // Create the RTVs
            var descRTV = new RenderTargetViewDescription();
            descRTV.Format = textureDesc.Format;
            descRTV.Dimension = RenderTargetViewDimension.Texture2DArray;
            descRTV.Texture2DArray.MipSlice = 0;
            // 1. Single RTV for single pass rendering
            descRTV.Texture2DArray.FirstArraySlice = 0;
            descRTV.Texture2DArray.ArraySize = 2;
            EnvMapRTV = ToDispose(new RenderTargetView(device, EnvMap, descRTV));

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
                OptionFlags = ResourceOptionFlags.None,
                ArraySize = 2 // 2-sides of the env map
            }))
            {
                var descDSV = new DepthStencilViewDescription();
                descDSV.Format = depth.Description.Format;
                descDSV.Dimension = DepthStencilViewDimension.Texture2DArray;
                descDSV.Flags = DepthStencilViewFlags.None;
                descDSV.Texture2DArray.MipSlice = 0;
                // 1. Single DSV for single pass rendering
                descDSV.Texture2DArray.FirstArraySlice = 0;
                descDSV.Texture2DArray.ArraySize = 2;
                EnvMapDSV = ToDispose(new DepthStencilView(device, depth, descDSV));
            }

            // Create the viewport
            Viewport = new Viewport(0, 0, Size, Size);

            // Create the per cube map buffer (to store the 6 ViewProjection matrices)
            PerEnvMapBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<PerEnvMap>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            PerEnvMapBuffer.DebugName = "PerEnvMapBuffer";
        }

        // Update camera position for the environment map
        public void SetViewPoint(Vector3 camera)
        {
            this.DualMapView.View = Matrix.LookAtRH(camera, camera + Vector3.UnitZ, Vector3.UnitY);
            this.DualMapView.NearClip = 0.0f;
            this.DualMapView.FarClip = 100.0f;
        }

        /// <summary>
        /// Update the 6-sides of the cube map using a single pass via Geometry shader instancing with the provided context
        /// </summary>
        /// <param name="context">The context to render within</param>
        /// <param name="renderScene">The method that will render the scene</param>
        public void UpdateSinglePass(DeviceContext context, Action<DeviceContext, Matrix, Matrix, RenderTargetView, DepthStencilView, DualParaboloidMap> renderScene)
        {
            // Don't render the reflector itself
            if (Reflector != null)
            {
                Reflector.Show = false;
            }

            // Prepare pipeline
            context.OutputMerger.SetRenderTargets(EnvMapDSV, EnvMapRTV);
            context.Rasterizer.SetViewport(Viewport);

            // Update perCubeMap with the ViewProjections
            PerEnvMap pem = this.DualMapView;
            pem.View.Transpose(); // Must transpose the matrix for HLSL
            context.UpdateSubresource(ref pem, PerEnvMapBuffer);
            
            // Assign the per dual map buffer to the VS and PS stages at slot 4
            context.VertexShader.SetConstantBuffer(4, PerEnvMapBuffer);
            context.PixelShader.SetConstantBuffer(4, PerEnvMapBuffer);
            
            // Render the scene using the view, projection, RTV and DSV
            // Note that we use an identity matrix for the projection!
            renderScene(context, this.DualMapView.View, Matrix.Identity, EnvMapRTV, EnvMapDSV, this);

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

        protected override void DoRender()
        {
            throw new NotImplementedException("Use UpdateSinglePass instead.");
        }
    }
}
