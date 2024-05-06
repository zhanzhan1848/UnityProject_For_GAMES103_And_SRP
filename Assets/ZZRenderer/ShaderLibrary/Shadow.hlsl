#ifndef ZZ_SHADOW_INCLUDED
#define ZZ_SHADOW_INCLUDED


#include "./LightInput.hlsl"

// RenderTextureFormat.Shadowmap 的情况下必须使用此宏读取深度信息
UNITY_DECLARE_SHADOWMAP(_XMainShadowMap);

float4 _ShadowParams; // x is depth bias, y is normal bias, z is strength

// 将坐标从世界坐标转换到主灯光的裁剪空间
float3 WorldToShadowMapPos(float3 positionWS)
{
	float4 positionCS = mul(_XMainLightMatrixWorldToShadowMap, float4(positionWS, 1));
	positionCS /= positionCS.w;
	return positionCS;
}

// 检查世界坐标是否位于主灯光的阴影中（0表示不在阴影中， 大雨0表示在阴影中， 数值代表了阴影强度）
float GetMainLightShadowAtten(float3 positionWS, float3 normalWS)
{
#if _RECEIVE_SHADOWS_OFF
	return 0;
#else
	if (_ShadowParams.z == 0) return 0;
	float3 shadowMapPos = WorldToShadowMapPos(positionWS + normalWS * _ShadowParams.y);
	float depthToLight = shadowMapPos.z;
	float2 sampleUV = shadowMapPos.xy;
	// RenderTextureFormat.Shadowmap 的情况下必须使用此宏读取深度信息
	float depth = UNITY_SAMPLE_SHADOW(_XMainShadowMap, shadowMapPos);
#if UNITY_REVERSED_Z
	// depthToLight < depth 表示在阴影中
	return clamp(step(depthToLight + _ShadowParams.x, 1 - depth), 0, _ShadowParams.z); // depthToLight + _ShadowParams.x
#else
	// depthToLight > depth 表示在阴影中
	return clamp(step(depth, depthToLight - _ShadowParams.x), 0, _ShadowParams.z); // depthToLight - _ShadowParams.x
#endif
#endif
}

/**
========= Shadoiw Caster Region ========
**/

struct ShadowCasterAttributes
{
	float4	positionOS : POSITION;
};

struct ShadowCasterVaryings
{
	float4	positionCS : SV_POSITION;
};

ShadowCasterVaryings ShadowCasterVertex(ShadowCasterAttributes input)
{
	ShadowCasterVaryings output;
	float4 positionCS = UnityObjectToClipPos(input.positionOS);
	output.positionCS = positionCS;
	return output;
}

half4 ShadowCasterFragment(ShadowCasterVaryings input) : SV_Target
{
	return 0;
}

#endif