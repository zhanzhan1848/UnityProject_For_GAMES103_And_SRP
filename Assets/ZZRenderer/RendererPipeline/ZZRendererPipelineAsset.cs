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

    [SerializeField]
    private ShadowSetting _shadowSetting = new ShadowSetting();

    public bool enableSrpBatcher
    {
        get
        {
            return _srpBatcher;
        }
    }

    public ShadowSetting shadowSetting
    {
        get
        {
            return _shadowSetting;
        }
    }

    protected override RenderPipeline CreatePipeline()
    {
        return new ZZRenderPipeline(this);
    }
}


public class ZZRenderPipeline : RenderPipeline
{
    // Standard shader �ܶ��lightmode����ForwardBase
    private ShaderTagId _shaderTag = new ShaderTagId("ForwardBase");
    private ShaderTagId _shaderTag_ZForward = new ShaderTagId("ZForwardBase");
    // �����Զ���ƹ���󣬱��������֧���µƹ�
    private LightConfigurator _lightConfigurator = new LightConfigurator();
    // ����shadow pass������
    private ShadowCasterPass _shadowCastPass = new ShadowCasterPass();
    private CommandBuffer _command = new CommandBuffer();
    private ZZRendererPipelineAsset _setting;
    public ZZRenderPipeline(ZZRendererPipelineAsset setting)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = setting.enableSrpBatcher;
        
        _command.name = "RenderCamera";
        _setting = setting;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(var camera in cameras)
        {
            RenderOpaqueObjectPerCamera(context, camera);
            // RenderTransparentObjectPerCamera(context, camera);
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
        // д��camera ��ز���
        context.SetupCameraProperties(camera);
        // ��պ�
        context.DrawSkybox(camera);
        // �ü�����
        camera.TryGetCullingParameters(out var cullingParams);
        // ����shadowDistance������shadowmap�ľ�������¸�������������
        cullingParams.shadowDistance = Mathf.Min(_setting.shadowSetting.shadowDistance, camera.farClipPlane - camera.nearClipPlane);
        var cullingResults = context.Cull(ref cullingParams);
        // �����Զ���ƹ���󣬱��������֧���µƹ�
        var lightData = _lightConfigurator.SetupShaderLightParams(context, ref cullingResults);

        var casterSetting = new ShadowCasterPass.ShadowCasterSetting();
        casterSetting.cullingResults = cullingResults;
        casterSetting.lightData = lightData;
        casterSetting.shadowSetting = _setting.shadowSetting;

        // ͶӰpass
        _shadowCastPass.Execute(context, camera, ref casterSetting);

        // ����camera ��ز���
        context.SetupCameraProperties(camera);

        // ����������
        ClearCameraTarget(context, camera);

        // ����������Ⱦʱ������
        var sortingSetting = new SortingSettings(camera);
        var drawSetting = new DrawingSettings();
        drawSetting.SetShaderPassName(1, _shaderTag);
        drawSetting.SetShaderPassName(2, _shaderTag_ZForward);
        drawSetting.sortingSettings = sortingSetting;
        // drawSetting.enableDynamicBatching = true;
        // ������Ҫ��Ⱦ������
        var filterSetting = new FilteringSettings(RenderQueueRange.opaque);
        // ��������
        context.DrawRenderers(cullingResults, ref drawSetting, ref filterSetting);
    }

    private void RenderTransparentObjectPerCamera(ScriptableRenderContext context, Camera camera)
    {
        // д��camera ��ز���
        context.SetupCameraProperties(camera);
        // ��պ�
        context.DrawSkybox(camera);
        // �ü�����
        camera.TryGetCullingParameters(out var cullingParams);
        // ����shadowDistance������shadowmap�ľ�������¸�������������
        cullingParams.shadowDistance = Mathf.Min(_setting.shadowSetting.shadowDistance, camera.farClipPlane - camera.nearClipPlane);
        var cullingResults = context.Cull(ref cullingParams);
        // �����Զ���ƹ���󣬱��������֧���µƹ�
        var lightData = _lightConfigurator.SetupShaderLightParams(context, ref cullingResults);

        var casterSetting = new ShadowCasterPass.ShadowCasterSetting();
        casterSetting.cullingResults = cullingResults;
        casterSetting.lightData = lightData;
        casterSetting.shadowSetting = _setting.shadowSetting;

        // ͶӰpass
        _shadowCastPass.Execute(context, camera, ref casterSetting);

        // ����camera ��ز���
        context.SetupCameraProperties(camera);

        // ����������
        ClearCameraTarget(context, camera);

        // ����������Ⱦʱ������
        var sortingSetting = new SortingSettings(camera);
        var drawSetting = new DrawingSettings();
        drawSetting.SetShaderPassName(1, _shaderTag);
        drawSetting.SetShaderPassName(2, _shaderTag_ZForward);
        drawSetting.sortingSettings = sortingSetting;
        // drawSetting.enableDynamicBatching = true;
        // ������Ҫ��Ⱦ������
        var filterSetting = new FilteringSettings(RenderQueueRange.transparent);
        // ��������
        context.DrawRenderers(cullingResults, ref drawSetting, ref filterSetting);
    }
}