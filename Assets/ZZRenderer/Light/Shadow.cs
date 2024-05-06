using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZZRenderer
{
    public class ShadowCasterPass
    {
        private CommandBuffer _commandBuffer = new CommandBuffer();

        private ShadowMapTextureHandler _shadowMapHandler = new ShadowMapTextureHandler();

        public ShadowCasterPass() 
        {
            _commandBuffer.name = "ShadowCaster";
        }

        public void SetupShadowCasterView(ScriptableRenderContext context, int shadowMapResolution, ref Matrix4x4 matrixView, ref Matrix4x4 matrixProj)
        {
            _commandBuffer.Clear();

            _commandBuffer.SetViewport(new Rect(0, 0, shadowMapResolution, shadowMapResolution));

            _commandBuffer.SetViewProjectionMatrices(matrixView, matrixProj);

            _commandBuffer.SetRenderTarget(_shadowMapHandler.renderTargetIdentifier, _shadowMapHandler.renderTargetIdentifier);

            _commandBuffer.ClearRenderTarget(true, true, Color.black, 1);

            context.ExecuteCommandBuffer(_commandBuffer);
        }

        /// <summary>
        /// ͨ��ComputeDirectionalShadowMatricesAndCullingPrimitives�õ���ͶӰ�������Ӧ��x,y,z��Χ�ֱ�Ϊ��Ϊ(-1,1).
        /// ���������Ҫ��������任���󣬿��Խ���������ת����ShadowMap�������ռ䡣��Ӧ��xy��ΧΪ(0,1),z��ΧΪ(1,0)
        /// </summary>
        private static Matrix4x4 GetWorldToShadowMapSpaceMatrix(Matrix4x4 proj, Matrix4x4 view)
        {
            //���ƽ̨�Ƿ�zBuffer��ת,һ������£�z�᷽���ǳ���Ļ�ڣ�����СԶ�󡣵�����zBuffer��ת������£�z���ǳ���Ļ�⣬������ԶС��
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            // uv_depth = xyz * 0.5 + 0.5. 
            // ����xy��(-1,1)ӳ�䵽(0,1),z��(-1,1)��(1,-1)ӳ�䵽(0,1)��(1,0)
            Matrix4x4 worldToShadow = proj * view;
            var textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;

            return textureScaleAndBias * worldToShadow;
        }

        public void Execute(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults, ref LightData lightData)
        {
            // ����������
            if (!lightData.HasMainLight())
            {
                Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(0, 0, 0, 0));
                return;
            }
            // false ��ʾ�õƹ�Գ�����Ӱ�죬 ֱ�ӷ���
            if (!cullingResults.GetShadowCasterBounds(lightData.mainLightIndex, out var lightBounds))
            {
                Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(0, 0, 0, 0));
                return;
            }

            var mainLight = lightData.mainLight;
            var lightComp = mainLight.light;
            var shadowResolution = GetShadowMapResolution(lightComp);
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightData.mainLightIndex, 0, 1,
                new Vector3(1, 0, 0), shadowResolution, lightComp.shadowNearPlane, out var matrixView, out var matrixProj, out var shadowSplitData);
            var matrixWorldToShadowMapSpace = GetWorldToShadowMapSpaceMatrix(matrixProj, matrixView);

            ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(cullingResults, lightData.mainLightIndex, BatchCullingProjectionType.Orthographic);
            shadowDrawSetting.splitData = shadowSplitData;

            Shader.SetGlobalMatrix(ShaderProperties.MainLightMatrixWorldToShadowSpace, matrixWorldToShadowMapSpace);
            Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(lightComp.shadowBias, lightComp.shadowNormalBias, lightComp.shadowStrength, 0));

            // ����shadowmap texture
            _shadowMapHandler.AcquireRenderTextureIfNot(shadowResolution);

            // ����ͶӰ��ز���
            SetupShadowCasterView(context, shadowResolution, ref matrixView, ref matrixProj);

            // ������Ӱ
            context.DrawShadows(ref shadowDrawSetting);
        }

        private static int GetShadowMapResolution(Light light)
        {
            switch (light.shadowResolution)
            {
                case LightShadowResolution.VeryHigh: return 2048;
                case LightShadowResolution.High: return 1024;
                case LightShadowResolution.Medium: return 512;
                case LightShadowResolution.Low: return 256;
            }
            return 256;
        }

        public class ShadowMapTextureHandler
        {
            private RenderTargetIdentifier _renderTargetIdentifier = "_XMainShadowMap";
            private int _shadowmapId = Shader.PropertyToID("_XMainShadowMap");
            private RenderTexture _shadowmapTexture;

            public RenderTargetIdentifier renderTargetIdentifier
            {
                get
                {
                    return _renderTargetIdentifier;
                }
            }

            public void AcquireRenderTextureIfNot(int resolution)
            {
                if(_shadowmapTexture && _shadowmapTexture.width != resolution)
                {
                    // resolution changed
                    RenderTexture.ReleaseTemporary(_shadowmapTexture);
                    _shadowmapTexture = null;
                }

                if(!_shadowmapTexture)
                {
                    _shadowmapTexture = RenderTexture.GetTemporary(resolution, resolution, 16, RenderTextureFormat.Shadowmap);
                    Shader.SetGlobalTexture(ShaderProperties.MainShadowMap, _shadowmapTexture);
                    _renderTargetIdentifier = new RenderTargetIdentifier(_shadowmapTexture);
                }
            }
        }

        public static class ShaderProperties
        {
            public static readonly int MainLightMatrixWorldToShadowSpace = Shader.PropertyToID("_XMainLightMatrixWorldToShadowMap");

            // xΪdepthBias, yΪnormalBias�� zΪshadowStrenth
            public static readonly int ShadowParams = Shader.PropertyToID("_ShadowParams");
            public static readonly int MainShadowMap = Shader.PropertyToID("_XMainShadowMap");
        }
    }
}
