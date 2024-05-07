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

        private Matrix4x4[] _worldToCascadeShadowMapMatrices = new Matrix4x4[4];
        private Vector4[] _cascadeCullingSpheres = new Vector4[4];

        public ShadowCasterPass() 
        {
            _commandBuffer.name = "ShadowCaster";
        }

        public void SetupShadowCasterView(ScriptableRenderContext context, int shadowMapResolution)
        {
            _commandBuffer.Clear();

            _commandBuffer.SetViewport(new Rect(0, 0, shadowMapResolution, shadowMapResolution));

            _commandBuffer.SetRenderTarget(_shadowMapHandler.renderTargetIdentifier, _shadowMapHandler.renderTargetIdentifier);

            _commandBuffer.ClearRenderTarget(true, true, Color.black, 1);

            context.ExecuteCommandBuffer(_commandBuffer);
        }

        private void SetupShadowCascade(ScriptableRenderContext context, Vector2 offsetInAtlas, int resolution, ref Matrix4x4 matrixView, ref Matrix4x4 matrixProj) 
        {
            _commandBuffer.Clear();
            _commandBuffer.SetViewport(new Rect(offsetInAtlas.x, offsetInAtlas.y, resolution, resolution));
            _commandBuffer.SetViewProjectionMatrices(matrixView, matrixProj);
            context.ExecuteCommandBuffer(_commandBuffer);
        }

        /// <summary>
        /// ͨ��ComputeDirectionalShadowMatricesAndCullingPrimitives�õ���ͶӰ�������Ӧ��x,y,z��Χ�ֱ�Ϊ��Ϊ(-1,1).
        /// ���������Ҫ��������任���󣬿��Խ���������ת����ShadowMap�������ռ䡣��Ӧ��xy��ΧΪ(0,1),z��ΧΪ(1,0)
        /// </summary>
        private static Matrix4x4 GetWorldToShadowMapSpaceMatrix(Matrix4x4 proj, Matrix4x4 view, Vector4 cascadeOffsetAndScale)
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

            // �ٽ�uvӳ�䵽cascadeShadowMap�Ŀռ�
            var cascadeOffsetAndScaleMatrix = Matrix4x4.identity;

            cascadeOffsetAndScaleMatrix.m00 = cascadeOffsetAndScale.z;
            cascadeOffsetAndScaleMatrix.m03 = cascadeOffsetAndScale.x;

            cascadeOffsetAndScaleMatrix.m11 = cascadeOffsetAndScale.w;
            cascadeOffsetAndScaleMatrix.m13 = cascadeOffsetAndScale.y;

            return cascadeOffsetAndScaleMatrix * textureScaleAndBias * worldToShadow;
        }

        public void Execute(ScriptableRenderContext context, Camera camera, ref ShadowCasterSetting setting)
        {
            ref var lightData = ref setting.lightData;
            ref var cullingResults = ref setting.cullingResults;
            var shadowSetting = setting.shadowSetting;

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
            // ����shadowmap texture
            _shadowMapHandler.AcquireRenderTextureIfNot(shadowResolution);

            var cascadeRatio = shadowSetting.cascadeRatio;

            // ����ͶӰ��ز���
            this.SetupShadowCasterView(context, shadowResolution);

            var cascadeAtlasGridSize = Mathf.CeilToInt(Mathf.Sqrt(shadowSetting.cascadeCount));
            var cascadeResolution = shadowResolution / cascadeAtlasGridSize;

            for(var i = 0; i < shadowSetting.cascadeCount; i++)
            {
                var x = i % cascadeAtlasGridSize;
                var y = i / cascadeAtlasGridSize;
                var offsetInAtlas = new Vector2(x * cascadeResolution, y * cascadeResolution);

                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightData.mainLightIndex, i, shadowSetting.cascadeCount,
                        cascadeRatio, cascadeResolution, lightComp.shadowNearPlane, out var matrixView, out var matrixProj, out var shadowSplitData);

                ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(cullingResults, lightData.mainLightIndex, BatchCullingProjectionType.Orthographic);
                shadowDrawSetting.splitData = shadowSplitData;

                SetupShadowCascade(context, offsetInAtlas, cascadeResolution, ref matrixView, ref matrixProj);

                // ������Ӱ
                context.DrawShadows(ref shadowDrawSetting);

                // ����Cascade ShadowMap�ռ�ͶӰ����Ͱ�ΧԲ
                var cascadeOffsetAndScale = new Vector4(offsetInAtlas.x, offsetInAtlas.y, cascadeResolution, cascadeResolution) / shadowResolution;
                var matrixWorldToShadowMapSpace = GetWorldToShadowMapSpaceMatrix(matrixProj, matrixView, cascadeOffsetAndScale);
                _worldToCascadeShadowMapMatrices[i] = matrixWorldToShadowMapSpace;
                _cascadeCullingSpheres[i] = shadowSplitData.cullingSphere;
            }

            Shader.SetGlobalMatrixArray(ShaderProperties.WorldToMainLightCascadeShadowMapSpaceMatrices, _worldToCascadeShadowMapMatrices);
            Shader.SetGlobalVectorArray(ShaderProperties.CascadeCullingSpheres, _cascadeCullingSpheres);

            Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(lightComp.shadowBias, lightComp.shadowNormalBias, lightComp.shadowStrength, shadowSetting.cascadeCount));
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

        public struct ShadowCasterSetting
        {
            public ShadowSetting shadowSetting;
            public CullingResults cullingResults;
            public LightData lightData;
        }

        public static class ShaderProperties
        {
            public static readonly int MainLightMatrixWorldToShadowSpace = Shader.PropertyToID("_XMainLightMatrixWorldToShadowMap");
            public static readonly int WorldToMainLightCascadeShadowMapSpaceMatrices = Shader.PropertyToID("_XWorldToMainLightCascadeShadowMapSpaceMatrices");

            // xΪdepthBias, yΪnormalBias�� zΪshadowStrenth
            public static readonly int ShadowParams = Shader.PropertyToID("_ShadowParams");
            public static readonly int MainShadowMap = Shader.PropertyToID("_XMainShadowMap");

            // ÿ��cascade�Ŀռ�ü���Χ��
            public static readonly int CascadeCullingSpheres = Shader.PropertyToID("_XCascadeCullingSpheres");
        }
    }
}
