using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;

public class PortalStackFeature : ScriptableRendererFeature
{
    class PortalPass : ScriptableRenderPass
    {
        Material material;
        List<PortalPlane> planes;

        RTHandle rtA;
        RTHandle rtB;

        public PortalPass(Material mat)
        {
            material = mat;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void Setup(List<PortalPlane> p)
        {
            planes = p;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref rtA, desc, name: "_PortalA");
            RenderingUtils.ReAllocateIfNeeded(ref rtB, desc, name: "_PortalB");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (planes == null || planes.Count == 0) return;

            CommandBuffer cmd = CommandBufferPool.Get("Portal Stack");
            var cam = renderingData.cameraData.camera;

            RTHandle src = renderingData.cameraData.renderer.cameraColorTargetHandle;
            RTHandle dst = rtA;

            // ³õÊ¼¿½±´
            Blitter.BlitCameraTexture(cmd, src, dst);

            bool ping = true;

            foreach (var p in planes)
            {
                Vector3 wp = p.transform.position;
                Vector3 sp = cam.WorldToViewportPoint(wp);

                material.SetFloat("_AngleRad", p.rotationAngle * Mathf.Deg2Rad);
                material.SetVector("_Pivot", new Vector2(sp.x, sp.y));

                if (ping)
                    Blitter.BlitCameraTexture(cmd, rtA, rtB, material, 0);
                else
                    Blitter.BlitCameraTexture(cmd, rtB, rtA, material, 0);

                ping = !ping;
            }

            // Êä³ö»ØÆÁÄ»
            Blitter.BlitCameraTexture(
                cmd,
                ping ? rtA : rtB,
                src
            );

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd) { }
    }

    public Material portalMaterial;
    PortalPass pass;

    public override void Create()
    {
        pass = new PortalPass(portalMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var planes = Object.FindObjectsOfType<PortalPlane>()
            .OrderBy(p => p.order)
            .ToList();

        if (planes.Count == 0) return;

        pass.Setup(planes);
        renderer.EnqueuePass(pass);
    }
}
