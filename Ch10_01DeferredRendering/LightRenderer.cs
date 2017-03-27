using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;

using Common;

// Resolve class name conflicts by explicitly stating
// which class they refer to:
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Mathematics.Interop;

namespace Ch10_01DeferredRendering
{
    public enum LightType : uint
    {
        Ambient = 0,
        Directional = 1,
        Point = 2,
        //Spot = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PerLight
    {
        public Vector3 Direction;
        public LightType Type;

        public Vector3 Position;
        public float Range;

        public Color4 Color;
        
        //public float SpotInnerCosine;
        //public float SpotOuterCosine;
        
        //private Vector2 _padding0;
    }

    public class LightRenderer: Common.RendererBase
    {
        #region Members initialized by CreateDeviceDepenedentResources
        Buffer perLightBuffer;

        Texture2D lightBuffer;
        RenderTargetView RTV;
        // Light buffer SRV
        public ShaderResourceView SRV;

        VertexShader vertexShader;
        PixelShader psAmbientLight;
        PixelShader psDirectionalLight;
        PixelShader psPointLight;
        PixelShader psSpotLight;
        PixelShader psDebugLight;

        RasterizerState rsCullBack;
        RasterizerState rsCullFront;
        RasterizerState rsWireframe;

        BlendState blendStateAdd;

        DepthStencilState depthLessThan;
        DepthStencilState depthGreaterThan;
        DepthStencilState depthDisabled;

        DepthStencilView DSVReadonly;
        #endregion

        #region Members initialized by constructor
        // Set from constructor
        //MeshRenderer spotLightVolume;
        MeshRenderer pointLightVolume;
        ScreenAlignedQuadRenderer saQuad;
        GBuffer gbuffer;
        #endregion

        // Set by caller
        public List<PerLight> Lights { get; private set; }
        public BoundingFrustum Frustum { get; set;}
        public ConstantBuffers.PerObject PerObject { get; set; }
        public Buffer PerObjectBuffer { get; set; }
        public int Debug { get; set; }

        public LightRenderer(
            MeshRenderer pointLightVolume,
            //MeshRenderer spotLightVolume, 
            ScreenAlignedQuadRenderer saQuad,
            GBuffer gbuffer)
        {
            this.Lights = new List<PerLight>();
            //this.spotLightVolume = spotLightVolume;
            this.pointLightVolume = pointLightVolume;
            this.saQuad = saQuad;
            this.gbuffer = gbuffer;
        }

        protected override void CreateDeviceDependentResources()
        {
            RemoveAndDispose(ref vertexShader);
            RemoveAndDispose(ref lightBuffer);
            RemoveAndDispose(ref RTV);
            RemoveAndDispose(ref SRV);
            RemoveAndDispose(ref rsCullBack);
            RemoveAndDispose(ref rsCullFront);
            RemoveAndDispose(ref rsWireframe);
            RemoveAndDispose(ref blendStateAdd);
            RemoveAndDispose(ref depthLessThan);
            RemoveAndDispose(ref depthGreaterThan);
            RemoveAndDispose(ref depthDisabled);
            RemoveAndDispose(ref perLightBuffer);

            RemoveAndDispose(ref psAmbientLight);
            RemoveAndDispose(ref psDirectionalLight);
            RemoveAndDispose(ref psPointLight);
            RemoveAndDispose(ref psSpotLight);
            RemoveAndDispose(ref psDebugLight);
            RemoveAndDispose(ref perLightBuffer);

            // Retrieve our SharpDX.Direct3D11.Device1 instance
            var device = this.DeviceManager.Direct3DDevice;

            int width, height;
            SampleDescription sampleDesc;
            
            // Retrieve DSV from GBuffer and extract width/height
            // then create a new read-only DSV
            using (var depthTexture = gbuffer.DSV.ResourceAs<Texture2D>())
            {
                width = depthTexture.Description.Width;
                height = depthTexture.Description.Height;
                sampleDesc = depthTexture.Description.SampleDescription;

                // Initialize read-only DSV
                var dsvDesc = gbuffer.DSV.Description;
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyDepth | DepthStencilViewFlags.ReadOnlyStencil;
                DSVReadonly = ToDispose(new DepthStencilView(device, depthTexture, dsvDesc));
            }
			// Check if GBuffer is multi-sampled
            bool isMSAA = sampleDesc.Count > 1;

            // Initialize the light render target
            var texDesc = new Texture2DDescription();
            texDesc.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
            texDesc.ArraySize = 1;
            texDesc.CpuAccessFlags = CpuAccessFlags.None;
            texDesc.Usage = ResourceUsage.Default;
            texDesc.Width = width;
            texDesc.Height = height;
            texDesc.MipLevels = 1; // No mip levels
            texDesc.SampleDescription = sampleDesc;
            texDesc.Format = Format.R8G8B8A8_UNorm;

            lightBuffer = ToDispose(new Texture2D(device, texDesc));

            // Render Target View description
            var rtvDesc = new RenderTargetViewDescription();
            rtvDesc.Format = Format.R8G8B8A8_UNorm;
            rtvDesc.Dimension = isMSAA ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
            rtvDesc.Texture2D.MipSlice = 0;
            RTV = ToDispose(new RenderTargetView(device, lightBuffer, rtvDesc));

            // SRV description for render targets
            var srvDesc = new ShaderResourceViewDescription();
            srvDesc.Format = Format.R8G8B8A8_UNorm;
            srvDesc.Dimension = isMSAA ? SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled : SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D;
            srvDesc.Texture2D.MipLevels = -1;
            srvDesc.Texture2D.MostDetailedMip = 0;
            SRV = ToDispose(new ShaderResourceView(device, lightBuffer, srvDesc));

            // Initialize additive blend state (assuming single render target)
            BlendStateDescription bsDesc = new BlendStateDescription();
            bsDesc.RenderTarget[0].IsBlendEnabled = true;
            bsDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            bsDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            bsDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
            bsDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            bsDesc.RenderTarget[0].SourceBlend = BlendOption.One;
            bsDesc.RenderTarget[0].DestinationBlend = BlendOption.One;
            bsDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            blendStateAdd = ToDispose(new BlendState(device, bsDesc));

            // Initialize rasterizer states
            RasterizerStateDescription rsDesc = new RasterizerStateDescription();
            rsDesc.FillMode = FillMode.Solid;
            rsDesc.CullMode = CullMode.Back;
            rsCullBack = ToDispose(new RasterizerState(device, rsDesc));
            rsDesc.CullMode = CullMode.Front;
            rsCullFront = ToDispose(new RasterizerState(device, rsDesc));
            rsDesc.CullMode = CullMode.Front;
            rsDesc.FillMode = FillMode.Wireframe;
            rsWireframe = ToDispose(new RasterizerState(device, rsDesc));

            // Initialize depth state
            var dsDesc = new DepthStencilStateDescription();
            dsDesc.IsStencilEnabled = false;
            dsDesc.IsDepthEnabled = true;

            // Less-than depth comparison
            dsDesc.DepthComparison = Comparison.Less;
            depthLessThan = ToDispose(new DepthStencilState(device, dsDesc));
            // Greater-than depth comparison
            dsDesc.DepthComparison = Comparison.Greater;
            depthGreaterThan = ToDispose(new DepthStencilState(device, dsDesc));
            // Depth/stencil testing disabled
            dsDesc.IsDepthEnabled = false;
            depthDisabled = ToDispose(new DepthStencilState(device, dsDesc));

            // Buffer to light parameters
            perLightBuffer = ToDispose(new Buffer(device, Utilities.SizeOf<PerLight>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

            if (isMSAA)
            {
                // Compile and create the vertex shader
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "VSLight", "vs_5_0"))
                    vertexShader = ToDispose(new VertexShader(device, bytecode));
                // Compile pixel shaders
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "PSAmbientLight", "ps_5_0"))
                    psAmbientLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "PSDirectionalLight", "ps_5_0"))
                    psDirectionalLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "PSPointLight", "ps_5_0"))
                    psPointLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "PSSpotLight", "ps_5_0"))
                    psSpotLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\LightsMS.hlsl", "PSDebugLight", "ps_5_0"))
                    psDebugLight = ToDispose(new PixelShader(device, bytecode));
            }
            else
            {
                // Compile and create the vertex shader
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "VSLight", "vs_5_0"))
                    vertexShader = ToDispose(new VertexShader(device, bytecode));
                // Compile pixel shaders
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "PSAmbientLight", "ps_5_0"))
                    psAmbientLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "PSDirectionalLight", "ps_5_0"))
                    psDirectionalLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "PSPointLight", "ps_5_0"))
                    psPointLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "PSSpotLight", "ps_5_0"))
                    psSpotLight = ToDispose(new PixelShader(device, bytecode));
                using (var bytecode = HLSLCompiler.CompileFromFile(@"Shaders\Lights.hlsl", "PSDebugLight", "ps_5_0"))
                    psDebugLight = ToDispose(new PixelShader(device, bytecode));
            }
        }

        public void Bind(DeviceContext1 context)
        {
            context.OutputMerger.SetTargets(DSVReadonly, RTV);
        }

        public void Unbind(DeviceContext1 context)
        {
            context.OutputMerger.ResetTargets();
        }

        public void Clear(DeviceContext1 context)
        {
            context.ClearRenderTargetView(RTV, new Color(0, 0, 0, 1));
        }

        protected override void DoRender()
        {
            // Early exit
            if (Lights.Count == 0)
                return;

            // Retrieve device context
            var context = this.DeviceManager.Direct3DContext;

            // backup existing context state
            int oldStencilRef = 0;
            RawColor4 oldBlendFactor;
            int oldSampleMaskRef;
            using(var oldVertexLayout = context.InputAssembler.InputLayout)
            using(var oldPixelShader = context.PixelShader.Get())
            using (var oldVertexShader = context.VertexShader.Get())
            using (var oldBlendState = context.OutputMerger.GetBlendState(out oldBlendFactor, out oldSampleMaskRef))
            using (var oldDepthState = context.OutputMerger.GetDepthStencilState(out oldStencilRef))
            using (var oldRSState = context.Rasterizer.State)
            {
                // Assign shader resources - TODO: create array in CreateDeviceDependentResources instead
                context.PixelShader.SetShaderResources(0, gbuffer.SRVs.ToArray().Concat(new[] { gbuffer.DSSRV }).ToArray());

                // Assign the additive blend state
                context.OutputMerger.BlendState = blendStateAdd;

                // Retrieve camera parameters
                SharpDX.FrustumCameraParams cameraParams = Frustum.GetCameraParams();

                // For each configured light
                for (var i = 0; i < Lights.Count; i++)
                {
                    PerLight light = Lights[i];

                    PixelShader shader = null; // Assign shader
                    if (light.Type == LightType.Ambient)
                        shader = psAmbientLight;
                    else if (light.Type == LightType.Directional)
                        shader = psDirectionalLight;
                    else if (light.Type == LightType.Point)
                        shader = psPointLight;
                    //else if (light.Type == LightType.Spot)
                    //    shader = psSpotLight;

                    // Update the perLight constant buffer
                    // Calculate view space position (for frustum checks)
                    Vector3 lightDir = Vector3.Normalize(Lights[i].Direction);
                    Vector4 viewSpaceDir = Vector3.Transform(lightDir, PerObject.View);
                    light.Direction = new Vector3(viewSpaceDir.X, viewSpaceDir.Y, viewSpaceDir.Z);
                    Vector4 viewSpacePos = Vector3.Transform(Lights[i].Position, PerObject.View);
                    light.Position = new Vector3(viewSpacePos.X, viewSpacePos.Y, viewSpacePos.Z);

                    context.UpdateSubresource(ref light, perLightBuffer);
                    context.PixelShader.SetConstantBuffer(4, perLightBuffer);

                    light.Position = Lights[i].Position;
                    light.Direction = Lights[i].Direction;

                    // Check if the light should be considered full screen
                    bool isFullScreen = light.Type == LightType.Directional ||
                                        light.Type == LightType.Ambient;
                    if (!isFullScreen)
                    {
                        isFullScreen = (cameraParams.ZNear > viewSpacePos.Z - light.Range &&
                                        cameraParams.ZFar < viewSpacePos.Z + light.Range);
                    }
                    if (isFullScreen)
                    {
                        context.OutputMerger.DepthStencilState = depthDisabled;
                        // Use SAQuad
                        saQuad.ShaderResources = null;
                        saQuad.Shader = shader;
                        saQuad.Render();
                    }
                    else // Render volume
                    {
                        context.PixelShader.Set(shader);
                        context.VertexShader.Set(vertexShader);

                        Matrix world = Matrix.Identity;

                        MeshRenderer volume = null; ;
                        switch (light.Type)
                        {
                            case LightType.Point:
                                // Prepare world matrix
                                // Ensure no abrupt light edges with +50%
                                world.ScaleVector = Vector3.One * light.Range * 1.5f;
                                volume = pointLightVolume;
                                break;
                            /* TODO: Spot light support
                            case LightType.Spot:
                                // Determine rotation!
                                var D = Vector3.Normalize(light.Direction);
                                var s1 = Vector3.Cross(D, Vector3.UnitZ);
                                var s2 = Vector3.Cross(D, Vector3.UnitY);
                                Vector3 S;
                                if (s1.LengthSquared() > s2.LengthSquared())
                                    S = s1;
                                else
                                    S = s2;
                                var U = Vector3.Cross(D, S);
                                Matrix rotate = Matrix.Identity;
                                rotate.Forward = D;
                                rotate.Down = U;
                                rotate.Left = S;

                                float scaleZ = light.Range;
                                // Need to Abs - if negative it will invert our model and result in incorrect normals
                                float scaleXY = light.Range * Math.Abs((float)Math.Tan(Math.Acos(light.SpotOuterCosine*2)/2));

                                world.ScaleVector = new Vector3(scaleXY, scaleXY, scaleZ);
                                world *= rotate;
                                volume = spotLightVolume;
                                break;
                             * */
                            default:
                                continue;
                        }
                        world.TranslationVector = light.Position;
                        volume.World = world;
                        // Transpose the PerObject matrices
                        var transposed = PerObject;
                        transposed.World = volume.World;
                        transposed.WorldViewProjection = volume.World * PerObject.ViewProjection;
                        transposed.Transpose();
                        context.UpdateSubresource(ref transposed, PerObjectBuffer);

                        if (cameraParams.ZFar < viewSpacePos.Z + light.Range)
                        {
                            // Cull the back face and only render where there is something
                            // behind the front face.
                            context.Rasterizer.State = rsCullBack;
                            context.OutputMerger.DepthStencilState = depthLessThan;
                        }
                        else
                        {
                            // Cull front faces and only render where there is something 
                            // before the back face.
                            context.Rasterizer.State = rsCullFront;
                            context.OutputMerger.DepthStencilState = depthGreaterThan;
                        }
                        volume.Render();

                        // Show the light volumes for debugging
                        if (Debug > 0)
                        {
                            if (Debug == 1)
                                context.OutputMerger.SetDepthStencilState(depthGreaterThan);
                            else
                                context.OutputMerger.SetDepthStencilState(depthLessThan);
                            context.PixelShader.Set(psDebugLight);
                            context.Rasterizer.State = rsWireframe;
                            volume.Render();
                        }
                    }
                }

                // Reset pixel shader resources (all to null)
                context.PixelShader.SetShaderResources(0, new ShaderResourceView[gbuffer.SRVs.Count + 1]);
                
                // Restore context states
                context.PixelShader.Set(oldPixelShader);
                context.VertexShader.Set(oldVertexShader);
                context.InputAssembler.InputLayout = oldVertexLayout;
                context.OutputMerger.SetBlendState(oldBlendState, oldBlendFactor, oldSampleMaskRef);
                context.OutputMerger.SetDepthStencilState(oldDepthState, oldStencilRef);
                context.Rasterizer.State = oldRSState;
            }
        }
    }
}
