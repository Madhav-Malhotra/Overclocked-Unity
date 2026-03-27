using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CPUStationScreenSpaceOutlineRendererFeature : ScriptableRendererFeature
{
    private const string CompositeShaderName = "Overclocked/CPUStationOutlineComposite";
    private const string OutlineMaskTextureName = "_CPUStationOutlineMaskTexture";
    private static readonly int OutlineMaskTextureId = Shader.PropertyToID(OutlineMaskTextureName);

    private CPUStationOutlineMaskPass maskPass;
    private CPUStationOutlineCompositePass compositePass;
    private Material compositeMaterial;

    public override void Create()
    {
        Shader compositeShader = Shader.Find(CompositeShaderName);
        if (compositeShader == null)
        {
            Debug.LogError($"CPUStationScreenSpaceOutlineRendererFeature: Could not find shader '{CompositeShaderName}'.");
            return;
        }

        compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        maskPass = new CPUStationOutlineMaskPass(OutlineMaskTextureName);
        compositePass = new CPUStationOutlineCompositePass(OutlineMaskTextureName, OutlineMaskTextureId, compositeMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (compositeMaterial == null || maskPass == null || compositePass == null)
        {
            return;
        }

        if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }

        maskPass.Setup();
#if URP_COMPATIBILITY_MODE
        compositePass.Setup(renderer.cameraColorTargetHandle);
#else
        compositePass.Setup();
#endif
        renderer.EnqueuePass(maskPass);
        renderer.EnqueuePass(compositePass);
    }

    protected override void Dispose(bool disposing)
    {
        maskPass?.Dispose();
        compositePass?.Dispose();
        CoreUtils.Destroy(compositeMaterial);
    }

    private sealed class CPUStationOutlineMaskPass : ScriptableRenderPass
    {
        private readonly string profilerTag = "CPU Station Outline Mask";
        private readonly string maskTextureName;
        private readonly ProfilingSampler passProfilingSampler = new("CPU Station Outline Mask");

#if URP_COMPATIBILITY_MODE
        private RTHandle maskTexture;
#endif

        public CPUStationOutlineMaskPass(string maskTextureName)
        {
            this.maskTextureName = maskTextureName;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup()
        {
        }

#if URP_COMPATIBILITY_MODE
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

            RenderingUtils.ReAllocateIfNeeded(ref maskTexture, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: maskTextureName);
            ConfigureTarget(maskTexture, renderingData.cameraData.renderer.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CPUStation.DrawVisibleOutlineMasks(cmd);

                cmd.SetGlobalTexture(maskTextureName, maskTexture);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        public void Dispose()
        {
#if URP_COMPATIBILITY_MODE
            maskTexture?.Release();
#endif
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.cameraType != CameraType.Game && cameraData.camera.cameraType != CameraType.SceneView)
            {
                return;
            }

            TextureDesc maskDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
            maskDesc.name = maskTextureName;
            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.clear;
            maskDesc.depthBufferBits = DepthBits.None;
            maskDesc.msaaSamples = MSAASamples.None;

            TextureHandle maskHandle = renderGraph.CreateTexture(maskDesc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(profilerTag, out var passData, passProfilingSampler))
            {
                builder.SetRenderAttachment(maskHandle, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(maskHandle, Shader.PropertyToID(maskTextureName));
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    CPUStation.DrawVisibleOutlineMasks(context.cmd);
                });
            }
        }

        private class PassData
        {
        }
    }

    private sealed class CPUStationOutlineCompositePass : ScriptableRenderPass
    {
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private readonly string profilerTag = "CPU Station Outline Composite";
        private readonly string maskTextureName;
        private readonly int maskTextureId;
        private readonly Material compositeMaterial;

#if URP_COMPATIBILITY_MODE
        private RTHandle cameraColorTarget;
        private RTHandle tempColorTexture;
#endif

        public CPUStationOutlineCompositePass(string maskTextureName, int maskTextureId, Material compositeMaterial)
        {
            this.maskTextureName = maskTextureName;
            this.maskTextureId = maskTextureId;
            this.compositeMaterial = compositeMaterial;
            renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.AfterRenderingTransparents + 1);
            requiresIntermediateTexture = true;
        }

        public void Setup()
        {
        }

#if URP_COMPATIBILITY_MODE
        public void Setup(RTHandle cameraColorTarget)
        {
            this.cameraColorTarget = cameraColorTarget;
        }
#endif

#if URP_COMPATIBILITY_MODE
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref tempColorTexture, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_CPUStationOutlineTempColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (compositeMaterial == null || cameraColorTarget == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempColorTexture);
                Blitter.BlitCameraTexture(cmd, tempColorTexture, cameraColorTarget, compositeMaterial, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        public void Dispose()
        {
#if URP_COMPATIBILITY_MODE
            tempColorTexture?.Release();
#endif
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.camera.cameraType != CameraType.Game && cameraData.camera.cameraType != CameraType.SceneView)
            {
                return;
            }

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("CPUStationScreenSpaceOutlineRendererFeature requires an intermediate color texture.");
                return;
            }

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "_CPUStationOutlineCompositeColor";
            destinationDesc.clearBuffer = false;
            destinationDesc.depthBufferBits = DepthBits.None;
            destinationDesc.msaaSamples = MSAASamples.None;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(profilerTag, out var passData))
            {
                passData.source = source;
                passData.destination = destination;
                passData.material = compositeMaterial;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseGlobalTexture(maskTextureId);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    if (data.material == null)
                    {
                        return;
                    }

                    RTHandle sourceTexture = data.source;
                    Vector2 viewportScale = sourceTexture.useScaling
                        ? new Vector2(sourceTexture.rtHandleProperties.rtHandleScale.x, sourceTexture.rtHandleProperties.rtHandleScale.y)
                        : Vector2.one;

                    data.material.SetTexture(BlitTextureId, sourceTexture);
                    Blitter.BlitTexture(context.cmd, sourceTexture, viewportScale, data.material, 0);
                });
            }

            resourceData.cameraColor = destination;
        }

        private class CompositePassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Material material;
        }
    }
}
