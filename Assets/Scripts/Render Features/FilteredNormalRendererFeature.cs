using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FilteredNormalRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public LayerMask layerMask;
        public Material normalsTextureMaterial;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPrePasses;
    }

    public Setting setting = new Setting();

    public class DrawNormalTexturePass : ScriptableRenderPass
    {
        private Setting setting;
        ShaderTagId shaderTag = new ShaderTagId("DepthOnly");
        FilteringSettings filter;

        public DrawNormalTexturePass(Setting setting, FilteredNormalRendererFeature feature)
        {
            this.setting = setting;
            RenderQueueRange queue = new RenderQueueRange();
            queue.lowerBound = 1000;
            queue.upperBound = 3500;
            filter = new FilteringSettings(queue, setting.layerMask);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            int temp = Shader.PropertyToID("_NormalTex");
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            cmd.GetTemporaryRT(temp, desc);
            ConfigureTarget(temp);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("_NormalTex");
            var draw = CreateDrawingSettings(shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            draw.overrideMaterial = setting.normalsTextureMaterial;
            draw.overrideMaterialPassIndex = 0;
            context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);
            CommandBufferPool.Release(cmd);
        }
    }

    private DrawNormalTexturePass _DrawNormalTexPass;

    public override void Create()
    {
        _DrawNormalTexPass = new DrawNormalTexturePass(setting, this);
        _DrawNormalTexPass.renderPassEvent = setting.passEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) => renderer.EnqueuePass(_DrawNormalTexPass);    
}


