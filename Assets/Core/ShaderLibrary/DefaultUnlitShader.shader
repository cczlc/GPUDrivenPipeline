Shader "ShaderForGPU/DefaultUnlitShader"
{
    Properties
    {
        _MainTex ("Albedo Map", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        [Space(25)]

        _Metallic_global ("Metallic", Range(0, 1)) = 0.5
        _Roughness_global ("Roughness", Range(0, 1)) = 0.5

        [Toggle] _Use_Metal_Map ("Use Metal Map", Float) = 1
        _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        [Space(25)]
        
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [Space(25)]

        [Toggle] _Use_Occlusion_Map ("Use Occlusion Map", Float) = 1
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Space(25)]

        [Toggle] _Use_Normal_Map ("Use Normal Map", Float) = 1
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
		Tags {"LightMode" = "CustomDefault"}

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float4 TtoW0 : TEXCOORD1;  
				float4 TtoW1 : TEXCOORD2;  
				float4 TtoW2 : TEXCOORD3;
			};

            float4 _MainTex_ST;
            float4 _BaseColor;

            sampler2D _MainTex;
            sampler2D _MetallicGlossMap;
            sampler2D _EmissionMap;
            sampler2D _OcclusionMap;
            sampler2D _BumpMap;

            float _Use_Metal_Map;
            float _Use_Normal_Map;
            float _Use_Occlusion_Map;
            float _Metallic_global;
            float _Roughness_global;

            v2f vert (a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv.xy = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);  
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);  
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

                o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);  

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 albedo = tex2D(_MainTex, i.uv).rgb;
                float3 emission = tex2D(_EmissionMap, i.uv).rgb;
                float ao = tex2D(_OcclusionMap, i.uv).g;

                float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
                float3 normal = float3(i.TtoW0.z, i.TtoW1.z, i.TtoW2.z);
				fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));
   
                float metallic = _Metallic_global;
                float roughness = _Roughness_global;

                if(_Use_Metal_Map)
                {
                    float4 metal = tex2D(_MetallicGlossMap, i.uv);
                    metallic = metal.r;
                    roughness = 1.0 - metal.a;
                }
                if(_Use_Normal_Map) 
                {
                    normal = UnpackNormal(tex2D(_BumpMap, i.uv));
                }

                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo * _BaseColor.rgb;
                if(_Use_Occlusion_Map)
                {
                     ambient *= ao;
                }

                fixed3 diffuse = _LightColor0.rgb * albedo * _BaseColor.rgb * max(0, dot(normal, lightDir));

                return float4(ambient + diffuse, 1.0);
            }
            ENDCG
        }
    }
}
