
Shader "Unlit/DrawParticles"
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

			#include "MatrixQuaternion.cginc"
			int AllParticles_Length;
			StructuredBuffer<float4> AllParticles_Position;
			StructuredBuffer<float4> AllParticles_Velocity;
			StructuredBuffer<float4> AllParticles_Rotation;
			float Scale;

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
				float3 vertexPos = v.vertex.xyz;

				float4 positionData = AllParticles_Position[instanceID];
				float4 velocityData = AllParticles_Velocity[instanceID];
				float4 rotation = AllParticles_Rotation[instanceID];
				
				float4x4 modelMatrix = quaternion_to_matrix(rotation);
				vertexPos = mul(modelMatrix, vertexPos * Scale) + positionData.xyz;
				float3 worldNormal = mul(modelMatrix, v.normal);

				float3 color;	
				{
					float debugValue = velocityData.w;
					float speed = saturate(length(velocityData.xyz) / 3.0f);
					float neighbours = saturate(debugValue / 20.0f);
					color = float3(speed, (1.5 - neighbours) * (1.1 - speed), 0.1 + neighbours);
				}

				v2f o;
				o.pos = mul(UNITY_MATRIX_VP, float4(vertexPos, 1.0f));
				o.color = color * lerp(0.1, 1, 0.1 + max(0, dot(worldNormal, float3(0,1,0))));
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