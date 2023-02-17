// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Outline"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		Tags { "Queue" = "Geometry" }
		LOD 100

		Pass
		{
			Blend One Zero
			//ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : TEXCOORD1;
				float3 viewDirection : TEXCOORD2;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1.0f));
				o.normal = UnityObjectToWorldNormal(v.normal);o.normal = v.normal;
				o.viewDirection = WorldSpaceViewDir(v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				if (abs(dot(normalize(i.viewDirection), normalize(i.normal))) > 0.5)
					discard;
				return float4(1, 1, 1, 1);
			}

			ENDCG
		}
	}
}