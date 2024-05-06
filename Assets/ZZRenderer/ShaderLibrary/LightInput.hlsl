#ifndef ZZ_LIGHT_INPUT_INCLUDED
#define ZZ_LIGHT_INPUT_INCLUDED

#include "HLSLSupport.cginc"

CBUFFER_START(UnityLighting)
//������
half4 _XAmbientColor;
//���ƹⷽ��
float4 _XMainLightDirection;
//���ƹ���ɫ
half4 _XMainLightColor;

//���ƹ� ����ռ�->ͶӰ�ռ�任����
float4x4 _XMainLightMatrixWorldToShadowMap;

CBUFFER_END

#endif