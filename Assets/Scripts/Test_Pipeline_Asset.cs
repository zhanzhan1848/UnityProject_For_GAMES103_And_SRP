using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Test_Pipeline_Asset")]
public class Test_Pipeline_Asset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new Test_Pipeline_Instance();
    }
}
