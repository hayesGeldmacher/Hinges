using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SliceWarpFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent passEvent = RenderPassEvent.AfterRendering;
        public Material material = null;
    }

    public Settings settings = new Settings();
    SliceWarpPass _pass;

    public override void Create()
    {
        _pass = new SliceWarpPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null)
            return;

        _pass.renderPassEvent = settings.passEvent;
        _pass.SetMaterial(settings.material);

        // IMPORTANT: Do not access renderer.cameraColorTargetHandle here in URP 15.
        renderer.EnqueuePass(_pass);
    }

    class SliceWarpPass : ScriptableRenderPass
    {
        static readonly string kTag = "SliceWarpFullscreenPass";

        Material _material;
        RTHandle _cameraColorTarget;
        RTHandle _tempColor;

        public void SetMaterial(Material mat) => _material = mat;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Safe place in URP15
            _cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempColor, desc, name: "_SliceWarpTempColor");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(kTag);

            // cameraColor -> temp (apply)
            Blitter.BlitCameraTexture(cmd, _cameraColorTarget, _tempColor, _material, 0);
            // temp -> cameraColor
            Blitter.BlitCameraTexture(cmd, _tempColor, _cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
