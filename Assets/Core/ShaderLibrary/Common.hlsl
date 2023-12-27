#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
//存储一些常用的函数，如空间变换

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

// 将Unity内置着色器变量转换为SRP库需要的变量
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection

// 定义SHADOWS_SHADOWMASK用于让遮蔽探针支持GPU Instancing
#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

// 我们直接使用SRP库中已经帮我们写好的函数
// 在include UnityInstancing.hlsl之前需要定义Unity_Matrix_M和其他宏，以及SpaceTransform.hlsl
// UnityInstancing.hlsl重新定义了一些宏用于访问实例化数据数组
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
// 用于解码法线贴图
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

// 平方操作的函数，用于计算高光项的BRDF方程
float Square (float x) {
	return x * x;
}

// 计算两点距离的平方（不开根号）
float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}

// 用于LOD抖动过渡 clip
void ClipLOD (Fragment fragment, float fade) {
	#if defined(LOD_FADE_CROSSFADE)
		// 使用和透明物体阴影相同的抖动算法
		float dither = InterleavedGradientNoise(fragment.positionSS, 0);
		// 考虑下一LOD的负值
		clip(fade + (fade < 0.0 ? dither : -dither));
	#endif
}

// 解码法线贴图，传入未解码的法线贴图采样结果和系数
float3 DecodeNormal (float4 sample, float scale) {
	#if defined(UNITY_NO_DXT5nm)
	    return normalize(UnpackNormalRGB(sample, scale));
	#else
	    return normalize(UnpackNormalmapRGorAG(sample, scale));
	#endif
}

// 将切线空间下的法线转换到世界空间下的法线
float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
	// 根据世界空间下的插值后顶点法线、顶点切线和w分量（-1或1，控制副切线方向）构建 切线空间到世界空间的变换矩阵
	float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
	return TransformTangentToWorld(normalTS, tangentToWorld);
}

#endif