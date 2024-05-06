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
        /// 通过ComputeDirectionalShadowMatricesAndCullingPrimitives得到的投影矩阵，其对应的x,y,z范围分别为均为(-1,1).
        /// 因此我们需要构造坐标变换矩阵，可以将世界坐标转换到ShadowMap齐次坐标空间。对应的xy范围为(0,1),z范围为(1,0)
        /// </summary>
        private static Matrix4x4 GetWorldToShadowMapSpaceMatrix(Matrix4x4 proj, Matrix4x4 view)
        {
            //检查平台是否zBuffer反转,一般情况下，z轴方向是朝屏幕内，即近小远大。但是在zBuffer反转的情况下，z轴是朝屏幕外，即近大远小。
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            // uv_depth = xyz * 0.5 + 0.5. 
            // 即将xy从(-1,1)映射到(0,1),z从(-1,1)或(1,-1)映射到(0,1)或(1,0)
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
            // 场景无主灯
            if (!lightData.HasMainLight())
            {
                Shader.SetGlobalVector(ShaderProperties.ShadowParams, new Vector4(0, 0, 0, 0));
                return;
            }
            // false 表示该灯光对场景无影响， 直接返回
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

            // 生成shadowmap texture
            _shadowMapHandler.AcquireRenderTextureIfNot(shadowResolution);

            // 设置投影相关参数
            SetupShadowCasterView(context, shadowResolution, ref matrixView, ref matrixProj);

            // 绘制阴影
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

            // x为depthBias, y为normalBias， z为shadowStrenth
            public static readonly int ShadowParams = Shader.PropertyToID("_ShadowParams");
            public static readonly int MainShadowMap = Shader.PropertyToID("_XMainShadowMap");
        }
    }
}
