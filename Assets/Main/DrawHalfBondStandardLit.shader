// help from, instanced surface shader: https://gist.github.com/sagarpatel/376ed0b42211a65db0ebdb71b91b7617
Shader "Unlit/DrawHalfBondStandardLit" 
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
		#include "DrawAtom.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;		
			uint instanceID : SV_InstanceID;
		};

		struct Input 
		{
			float4 color;
		};

		void my_vert(inout appdata v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);			

			#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_D3D11_9X) || defined(SHADER_API_DESKTOP) || defined(SHADER_API_MOBILE)
				DrawHalfBondResult a = DrawHalfBond(v.vertex.xyz, v.normal.xyz, v.instanceID);
				v.vertex.xyz = a.vertexPositionWorldSpace;
				v.normal.xyz = a.normalWorldSpace;
				o.color = float4(a.color,1);
			#endif
		}

		void my_surf(Input IN, inout SurfaceOutputStandard o) 
		{
			o.Albedo = IN.color;			
		}

		ENDCG
	}
	FallBack "Diffuse"
}