
Shader "Unlit/DrawParticlesNoShadows"
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
			#include "DrawParticle.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 color : TEXCOORD1;
			};

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				ParticleStruct particle = DrawParticle(v.vertex.xyz, instanceID);
				float3 worldNormal = v.normal;
				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(particle.vertexPositionWorldSpace, 1.0f));
				o.color = particle.color * lerp(0.1, 1, 0.1 + max(0, dot(worldNormal, float3(0,1,0))));
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return float4(i.color, 1);
			}

			ENDCG
		}
	}
}