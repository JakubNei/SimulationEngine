

// help from, instanced surface shader: https://gist.github.com/sagarpatel/376ed0b42211a65db0ebdb71b91b7617
Shader "Unlit/DrawParticles" 
{
	Properties 
	{

	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		//Cull Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf Standard fullforwardshadows vertex:vert
		//#pragma multi_compile_instancing
		//#pragma surface my_surf Standard addshadow vertex:my_vert	
		#pragma surface my_surf Standard addshadow fullforwardshadows vertex:my_vert	

		#pragma target 5.0

		#include "UnityCG.cginc"

		#ifdef SHADER_API_D3D11		

			#include "MatrixQuaternion.cginc"
			int AllParticles_Length;
			StructuredBuffer<float4> AllParticles_Position;
			StructuredBuffer<float4> AllParticles_Velocity;
			StructuredBuffer<float4> AllParticles_Rotation;
			float Scale;
		
		#endif
		

		// struct for vertex input data
		struct appdata_id
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;			
			uint instanceID : SV_InstanceID;
		};

		struct Input 
		{
			//float2 uv_MainTex;
			float4 col;
		};

		void my_vert(inout appdata_id v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);			

			float3 color = 1;
			float3 vertexPos = v.vertex,xyz;

			#ifdef SHADER_API_D3D11
				uint instanceID = (uint)v.instanceID;

				float4 positionData = AllParticles_Position[instanceID];
				float4 velocityData = AllParticles_Velocity[instanceID];
				float4 rotation = AllParticles_Rotation[instanceID];
				
				//float4x4 modelMatrix = quaternion_to_matrix(rotation);
				//vertexPos = mul(modelMatrix, vertexPos * Scale) + positionData.xyz;
				vertexPos = vertexPos * Scale + positionData.xyz;

				float debugValue = velocityData.w;
				float speed = saturate(length(velocityData.xyz) / 3.0f);
				float neighbours = saturate(debugValue / 20.0f);
				color = float3(speed, (1.5 - neighbours) * (1 - speed), neighbours);
			#endif

			v.vertex.xyz = vertexPos;
			o.col = float4(color,1);
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