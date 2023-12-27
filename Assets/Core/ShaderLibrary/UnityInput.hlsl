#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED
// �洢Shader�е�һЩ���õ���������

// CBUFFER��ָ������������ԣ���Shader֧��SRP Batcher��ͬʱ�ڲ�֧��SRP Batcher��ƽ̨�Զ��ر���
// CBUFFER_START��Ҫ��һ��������������ʾ��C buffer������(Unity������һЩ���֣���UnityPerMaterial��UnityPerDraw
CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	// �ڶ��壨UnityPerDraw��CBufferʱ����ΪUnity��һ��������ݶ��鵽һ��Feature�У�
	// ��ʹ����û�õ�unity_LODFade������Ҳ��Ҫ�ŵ����CBuffer��������һ��������Feature
	// unity_LODFade��LOD����ʹ�ã���xֵ��ʾ��ǰ����ֵ������fade out��LOD��0����ʼfade out��1������ȫfade out��
	// ����fade in��LOD��-1����ʼfade in��0������ȫfade in����y��ʾ����ֵ��16�����仮���ڵ�ֵ������ʹ�õ���
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;

	float4 unity_RenderingLayer;

	// ÿ�����Դ��Ϣ
	// unity_LightData��y�����洢�˸��������Ч��Դ����
	real4 unity_LightData;
	// unity_LightIndices��ÿ��������һ����Դ���������ÿ������������8����Ч��Դ
	real4 unity_LightIndices[2];

	// �ڱ�̽��
	float4 unity_ProbesOcclusion;
	// ����̽����Ϣ������ʹ��HDR����LDR��ǿ��
	float4 unity_SpecCube0_HDR;

	// ������ͼuv�ı任�����Ƕ�����һ������չ����ʽ��
	// ����չ������Mesh��ÿ����������ӳ�䵽һ����άƽ��(UV����ϵ)
	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

	// ��г����������ϵ����һ��27����RGBͨ��ÿ��9��,ʵ��Ϊfloat3, SH : Spherical Harmonics
	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

	// LPPV������Ϣ
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_prev_MatrixM;
float4x4 unity_prev_MatrixIM;
float4x4 glstate_matrix_projection;

// ��ȡ���ò��������������ռ�λ��
float3 _WorldSpaceCameraPos;

float4 unity_OrthoParams;
// _ProjectionParams��X����ָʾ�����Ƿ���Ҫ�ֶ���ת����v����(��ֵ��ʾ��Ҫ��ת��
float4 _ProjectionParams;
float4 _ScreenParams;
float4 _ZBufferParams;

#endif