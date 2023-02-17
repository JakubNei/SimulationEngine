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
		#pragma surface my_surf Standard vertex:my_vert	

		#pragma target 5.0

		#include "UnityCG.cginc"

		#ifdef SHADER_API_D3D11		

		StructuredBuffer<uint> HashCodeToParticles;
		StructuredBuffer<float4> AllParticles_Position;
		StructuredBuffer<float4> AllParticles_Velocity;
		
		#endif
		
		int AllParticles_Length;
		float Scale;

		#include "Common.cginc"


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

		float3 HUEToRGB(float H)
		{
			float R = abs(H * 6 - 3) - 1;
			float G = 2 - abs(H * 6 - 2);
			float B = 2 - abs(H * 6 - 4);
			return saturate(float3(R, G, B));
		}

		void my_vert(inout appdata_id v, out Input o)
		{
			float3 vertexPos = v.vertex.xyz;
			UNITY_INITIALIZE_OUTPUT(Input, o);			
			
			vertexPos *= Scale;
			float3 c = 1;

			#ifdef SHADER_API_D3D11		

			float h = 0;			
			float4 positionData = AllParticles_Position[(uint)v.instanceID];
			float3 worldPosition = positionData.xyz;
			vertexPos += worldPosition;

			//h = (((uint)round(worldPosition.y / 3.0f)) % 10) / 10.0f; 
			//h = min(HashCodeToParticles[GetHashCode(worldPosition) * 2 + 1], 20) / 20.0f;
			//h = GetHashCode(worldPosition) == 5 ? 1 : 0.5; 
			//h = GetHashCode(worldPosition) % 10 / 10.0f; 
			//h = GetHashCode(worldPosition) % 1000 / 1000.0f; 
			//h = v.instanceID/(float)AllParticles_Length;
			//c = HUEToRGB(h);
			
			c = float3(positionData.w / 5.0f, 1 - positionData.w / 5.0f, 0);

			#endif


			v.vertex.xyz = vertexPos;
			o.col = float4(c,1);
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
// 			ZTest Always

// 			CGPROGRAM
// 			#pragma vertex vert
// 			#pragma fragment frag
// 			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
// 			#pragma target 4.5

// 			#include "UnityCG.cginc"

// 			int AllParticles_Length;
// 			StructuredBuffer<float3> AllParticles_Position;
// 			StructuredBuffer<float3> AllParticles_Velocity;
// 			float Scale;

// 			struct appdata
// 			{
// 				float4 vertex : POSITION;
// 				float2 uv : TEXCOORD0;
// 			};

// 			struct v2f
// 			{
// 				float4 pos : SV_POSITION;
// 				float3 color : TEXCOORD1;
// 			};

// 			v2f vert(appdata v, uint instanceID : SV_InstanceID)
// 			{
// 				float3 worldPosition = AllParticles_Position[instanceID];
// 				v2f o;
// 				o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz * Scale + worldPosition, 1.0f));
// 				o.color = float4(1,1,1,1);
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