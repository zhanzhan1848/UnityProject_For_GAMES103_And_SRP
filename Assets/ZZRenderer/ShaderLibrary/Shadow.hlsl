#ifndef ZZ_SHADOW_INCLUDED
#define ZZ_SHADOW_INCLUDED


#include "./LightInput.hlsl"

// RenderTextureFormat.Shadowmap ������±���ʹ�ô˺��ȡ�����Ϣ
UNITY_DECLARE_SHADOWMAP(_XMainShadowMap);

float4 _ShadowParams; // x is depth bias, y is normal bias, z is strength

// ���������������ת�������ƹ�Ĳü��ռ�
float3 WorldToShadowMapPos(float3 positionWS)
{
	float4 positionCS = mul(_XMainLightMatrixWorldToShadowMap, float4(positionWS, 1));
	positionCS /= positionCS.w;
	return positionCS;
}

// ������������Ƿ�λ�����ƹ����Ӱ�У�0��ʾ������Ӱ�У� ����0��ʾ����Ӱ�У� ��ֵ��������Ӱǿ�ȣ�
float GetMainLightShadowAtten(float3 positionWS, float3 normalWS)
{
#if _RECEIVE_SHADOWS_OFF
	return 0;
#else
	if (_ShadowParams.z == 0) return 0;
	float3 shadowMapPos = WorldToShadowMapPos(positionWS + normalWS * _ShadowParams.y);
	float depthToLight = shadowMapPos.z;
	float2 sampleUV = shadowMapPos.xy;
	// RenderTextureFormat.Shadowmap ������±���ʹ�ô˺��ȡ�����Ϣ
	float depth = UNITY_SAMPLE_SHADOW(_XMainShadowMap, shadowMapPos);
#if UNITY_REVERSED_Z
	// depthToLight < depth ��ʾ����Ӱ��
	return clamp(step(depthToLight + _ShadowParams.x, 1 - depth), 0, _ShadowParams.z); // depthToLight + _ShadowParams.x
#else
	// depthToLight > depth ��ʾ����Ӱ��
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