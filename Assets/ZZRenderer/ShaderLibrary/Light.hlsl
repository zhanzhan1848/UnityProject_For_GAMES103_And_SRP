#ifndef ZZ_LIGHT_INCLUDED
#define ZZ_LIGHT_INCLUDED


#include "./LightInput.hlsl"

// Lambert 漫反射
half4 LabmertDiffuse(float3 normal)
{
	return max(0, dot(normal, _XMainLightDirection.xyz)) * _XMainLightColor;
}

// BlinnPhong光照模型高光
half4 BlinnPhongSpecular(float3 viewDir, float3 normal, float shininess)
{
	float3 halfDir = normalize(viewDir + _XMainLightDirection.xyz);
	float nh = max(0, dot(halfDir, normal));
	return pow(nh, shininess) * _XMainLightColor;
}

// BlinnPhong光照模型
half4 BlinnPhongLight(float3 positionWS, float3 normalWS, float shininess, half4 diffuseColor, half4 specularColor)
{
	float3 viewDir = normalize(_WorldSpaceCameraPos - positionWS);
	return _XAmbientColor + LabmertDiffuse(normalWS) * diffuseColor + BlinnPhongSpecular(viewDir, normalWS, shininess) * specularColor;
	// return LabmertDiffuse(normalWS) * diffuseColor + BlinnPhongSpecular(viewDir, normalWS, shininess) * specularColor;
}

#endif