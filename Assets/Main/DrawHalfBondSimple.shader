Shader "Unlit/DrawHalfBondSimple"
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

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"
			#include "DrawAtom.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;				
				uint instanceID : SV_InstanceID;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 color : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				DrawHalfBondResult a = DrawHalfBond(v.vertex.xyz, v.normal, v.instanceID);
				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(a.vertexPositionWorldSpace, 1.0f));
				o.color = a.color * lerp(0.1, 1, 0.1 + max(0, dot(a.normalWorldSpace, float3(0,1,0))));
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