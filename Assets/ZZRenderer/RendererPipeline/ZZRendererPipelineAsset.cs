using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using ZZRenderer;

[CreateAssetMenu(menuName = "ZZRenderer/ZZRendererPipelineAsset")]
public class ZZRendererPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool _srpBatcher = true;

    public bool enableSrpBatcher
    {
        get
        {
            return _srpBatcher;
        }
    }

    protected override RenderPipeline CreatePipeline()
    {
        return new ZZRenderPipeline(this);
    }
}


public class ZZRenderPipeline : RenderPipeline
{
    // Standard shader 很多的lightmode都是ForwardBase
    private ShaderTagId _shaderTag = new ShaderTagId("ForwardBase");
    private ShaderTagId _shaderTag_ZForward = new ShaderTagId("ZForwardBase");
    // 新增自定义灯光类后，必须添加以支持新灯光
    private LightConfigurator _lightConfigurator = new LightConfigurator();
    // 新增shadow pass的设置
    private ShadowCasterPass _shadowCastPass = new ShadowCasterPass();
    private CommandBuffer _command = new CommandBuffer();

    public ZZRenderPipeline(ZZRendererPipelineAsset setting)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = setting.enableSrpBatcher;
        
        _command.name = "RenderCamera";
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(var camera in cameras)
        {
            RenderOpaqueObjectPerCamera(context, camera);
            RenderTransparentObjectPerCamera(context, camera);
        }

        context.Submit();
    }

    private void ClearCameraTarget(ScriptableRenderContext context, Camera camera)
    {
        _command.Clear();
        _command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
        _command.ClearRenderTarget(true, true, camera.backgroundColor);
        context.ExecuteCommandBuffer(_command);
    }

    private void RenderOpaqueObjectPerCamera(ScriptableRenderContext context, Camera camera)
    {
        // 写入camera 相关参数
        context.SetupCameraProperties(camera);
        // 天空盒
        context.DrawSkybox(camera);
        // 裁剪场景
        camera.TryGetCullingParameters(out var cullingParams);
        // 设置shadowDistance，不让shadowmap的距离过大导致根本看不到物体
        cullingParams.shadowDistance = 10;
        var cullingResults = context.Cull(ref cullingParams);
        // 新增自定义灯光类后，必须添加以支持新灯光
        var lightData = _lightConfigurator.SetupShaderLightParams(context, ref cullingResults);

        // 投影pass
        _shadowCastPass.Execute(context, camera, ref cullingResults, ref lightData);

        // 重设camera 相关参数
        context.SetupCameraProperties(camera);

        // 清除相机背景
        ClearCameraTarget(context, camera);

        // 计算物体渲染时的排序
        var sortingSetting = new SortingSettings(camera);
        var drawSetting = new DrawingSettings();
        drawSetting.SetShaderPassName(1, _shaderTag);
        drawSetting.SetShaderPassName(2, _shaderTag_ZForward);
        drawSetting.sortingSettings = sortingSetting;
        // drawSetting.enableDynamicBatching = true;
        // 过滤需要渲染的物体
        var filterSetting = new FilteringSettings(RenderQueueRange.opaque);
        // 绘制物体
        context.DrawRenderers(cullingResults, ref drawSetting, ref filterSetting);
    }

    private void RenderTransparentObjectPerCamera(ScriptableRenderContext context, Camera camera)
    {
        // 写入camera 相关参数
        context.SetupCameraProperties(camera);
        // 天空盒
        context.DrawSkybox(camera);
        // 裁剪场景
        camera.TryGetCullingParameters(out var cullingParams);
        // 设置shadowDistance，不让shadowmap的距离过大导致根本看不到物体
        cullingParams.shadowDistance = 10;
        var cullingResults = context.Cull(ref cullingParams);
        // 新增自定义灯光类后，必须添加以支持新灯光
        var lightData = _lightConfigurator.SetupShaderLightParams(context, ref cullingResults);

        // 投影pass
        _shadowCastPass.Execute(context, camera, ref cullingResults, ref lightData);

        // 重设camera 相关参数
        context.SetupCameraProperties(camera);

        // 清除相机背景
        // ClearCameraTarget(context, camera);

        // 计算物体渲染时的排序
        var sortingSetting = new SortingSettings(camera);
        var drawSetting = new DrawingSettings();
        drawSetting.SetShaderPassName(1, _shaderTag);
        drawSetting.SetShaderPassName(2, _shaderTag_ZForward);
        drawSetting.sortingSettings = sortingSetting;
        // drawSetting.enableDynamicBatching = true;
        // 过滤需要渲染的物体
        var filterSetting = new FilteringSettings(RenderQueueRange.transparent);
        // 绘制物体
        context.DrawRenderers(cullingResults, ref drawSetting, ref filterSetting);
    }
}