#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
//�洢һЩ���õĺ�������ռ�任

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

// ��Unity������ɫ������ת��ΪSRP����Ҫ�ı���
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection

// ����SHADOWS_SHADOWMASK�������ڱ�̽��֧��GPU Instancing
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

// ����ֱ��ʹ��SRP�����Ѿ�������д�õĺ���
// ��include UnityInstancing.hlsl֮ǰ��Ҫ����Unity_Matrix_M�������꣬�Լ�SpaceTransform.hlsl
// UnityInstancing.hlsl���¶�����һЩ�����ڷ���ʵ������������
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
// ���ڽ��뷨����ͼ
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

bool IsOrthographicCamera () {
	return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear (float rawDepth) {
	#if UNITY_REVERSED_Z
		rawDepth = 1.0 - rawDepth;
	#endif
	return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

#include "Fragment.hlsl"

// ƽ�������ĺ��������ڼ���߹����BRDF����
float Square (float x) {
	return x * x;
}

// ������������ƽ�����������ţ�
float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}

// ����LOD�������� clip
void ClipLOD (Fragment fragment, float fade) {
	#if defined(LOD_FADE_CROSSFADE)
		// ʹ�ú�͸��������Ӱ��ͬ�Ķ����㷨
		float dither = InterleavedGradientNoise(fragment.positionSS, 0);
		// ������һLOD�ĸ�ֵ
		clip(fade + (fade < 0.0 ? dither : -dither));
	#endif
}

// ���뷨����ͼ������δ����ķ�����ͼ���������ϵ��
float3 DecodeNormal (float4 sample, float scale) {
	#if defined(UNITY_NO_DXT5nm)
	    return normalize(UnpackNormalRGB(sample, scale));
	#else
	    return normalize(UnpackNormalmapRGorAG(sample, scale));
	#endif
}

// �����߿ռ��µķ���ת��������ռ��µķ���
float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
	// ��������ռ��µĲ�ֵ�󶥵㷨�ߡ��������ߺ�w������-1��1�����Ƹ����߷��򣩹��� ���߿ռ䵽����ռ�ı任����
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}

#endif