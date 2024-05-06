using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Test_Pipeline_Instance : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            context.SetupCameraProperties(camera);
            context.DrawSkybox(camera);

            camera.TryGetCullingParameters(out var parameters);
            var results = context.Cull(ref parameters);

            DrawingSettings ds = new DrawingSettings();
            ds.SetShaderPassName(1, new ShaderTagId("SRPDefaultUnlit"));
            ds.SetShaderPassName(2, new ShaderTagId("ForwardBase"));
            ds.sortingSettings = new SortingSettings() { criteria = SortingCriteria.CommonOpaque };

            FilteringSettings fs = new FilteringSettings(RenderQueueRange.opaque);

            context.DrawRenderers(results, ref ds, ref fs);
        }

        context.Submit();
    }
}
