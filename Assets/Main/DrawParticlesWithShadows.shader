

// help from, instanced surface shader: https://gist.github.com/sagarpatel/376ed0b42211a65db0ebdb71b91b7617
Shader "Unlit/DrawParticlesWithShadows" 
{
	Properties 
	{

	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		#pragma surface my_surf Standard addshadow fullforwardshadows vertex:my_vert	
		#pragma target 4.5

		#include "UnityCG.cginc"
		#include "DrawParticle.cginc"

		struct appdata_id
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;			
			uint instanceID : SV_InstanceID;
		};

		struct Input 
		{
			float4 col;
		};

		void my_vert(inout appdata_id v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);			

			#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)
				uint instanceID = (uint)v.instanceID;
				ParticleStruct particle = DrawParticle(v.vertex.xyz, v.normal.xyz, instanceID);
				v.vertex.xyz = particle.vertexPositionWorldSpace;
				v.normal.xyz = particle.normalWorldSpace;
				o.col = float4(particle.color,1);
			#endif
		}

		void my_surf(Input IN, inout SurfaceOutputStandard o) 
		{
			o.Albedo = IN.col;			
		}
		ENDCG
	}
	FallBack "Diffuse"
}



// Shader "Unlit/DrawParticles"
// {
// 	Properties
// 	{
// 	}
// 	SubShader
// 	{
// 		Tags { "RenderType" = "Opaque" }
// 		Tags { "Queue" = "Geometry" }
// 		LOD 100

// 		Pass
// 		{
// 			Blend One Zero
// 			//ZTest Always

// 			CGPROGRAM
// 			#pragma vertex vert
// 			#pragma fragment frag
// 			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
// 			#pragma target 4.5

// 			#include "UnityCG.cginc"

// 			#include "MatrixQuaternion.cginc"
// 			int AllParticles_Length;
// 			StructuredBuffer<float4> AllParticles_Position;
// 			StructuredBuffer<float4> AllParticles_Velocity;
// 			StructuredBuffer<float4> AllParticles_Rotation;
// 			float Scale;

// 			struct appdata
// 			{
// 				float4 vertex : POSITION;
// 				float3 normal : NORMAL;
// 			};

// 			struct v2f
// 			{
// 				float4 pos : SV_POSITION;
// 				float3 color : TEXCOORD1;
// 			};

// 			v2f vert(appdata v, uint instanceID : SV_InstanceID)
// 			{
// 				float3 vertexPos = v.vertex.xyz;

// 				float4 positionData = AllParticles_Position[instanceID];
// 				float4 velocityData = AllParticles_Velocity[instanceID];
// 				float4 rotation = AllParticles_Rotation[instanceID];
				
// 				float4x4 modelMatrix = quaternion_to_matrix(rotation);
// 				vertexPos = mul(modelMatrix, vertexPos * Scale) + positionData.xyz;
// 				float3 worldNormal = mul(modelMatrix, v.normal);

// 				float3 color;	
// 				{
// 					float debugValue = velocityData.w;
// 					float speed = saturate(length(velocityData.xyz) / 3.0f);
// 					float neighbours = saturate(debugValue / 20.0f);
// 					color = float3(speed, (1.5 - neighbours) * (1 - speed), neighbours);
// 					color *= lerp(0.1, 1, 0.1 + max(0, dot(worldNormal, float3(0,1,0))));
// 				}

// 				v2f o;
// 				o.pos = mul(UNITY_MATRIX_VP, float4(vertexPos, 1.0f));
// 				o.color = color;
// 				return o;
// 			}

// 			fixed4 frag(v2f i) : SV_Target
// 			{
// 				return float4(i.color, 1);
// 			}

// 			ENDCG
// 		}
// 	}
// }