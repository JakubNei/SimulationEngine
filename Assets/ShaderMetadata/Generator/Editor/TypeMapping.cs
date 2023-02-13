using System.Collections.Generic;

namespace ShaderMetadataGenerator
{
	struct TypeMapping
	{
		public string shaderType;
		public string cSharpType;
		public string function;
		public TypeMapping(string shaderType, string cSharpType, string function)
		{
			this.shaderType = shaderType;
			this.cSharpType = cSharpType;
			this.function = function;
		}
	}

	static class TypeMappings
	{
		public static List<TypeMapping> typeMappings = new List<TypeMapping>()
		{
			new TypeMapping("bool", "bool", "SetBoolParam"),
			new TypeMapping("bool2", "bool[]", "SetBoolParam"),
			new TypeMapping("bool3", "bool[]", "SetBoolParam"),
			new TypeMapping("bool4", "bool[]", "SetBoolParam"),
			new TypeMapping("int", "int", "SetIntParam"),
			new TypeMapping("int2", "Vector2Int", "SetVectorParam"),
			new TypeMapping("int3", "Vector3Int", "SetVectorParam"),
			new TypeMapping("int4", "int[]", "SetIntParam"),
			new TypeMapping("min12int", "int", "SetIntParam"),
			new TypeMapping("min12int2", "Vector2Int", "SetVectorParam"),
			new TypeMapping("min12int3", "Vector3Int", "SetVectorParam"),
			new TypeMapping("min12int4", "int[]", "SetIntParam"),
			new TypeMapping("min16int", "int", "SetIntParam"),
			new TypeMapping("min16int2", "Vector2Int", "SetVectorParam"),
			new TypeMapping("min16int3", "Vector3Int", "SetVectorParam"),
			new TypeMapping("min16int4", "int[]", "SetIntParam"),
			new TypeMapping("uint", "uint", "SetIntParam"),
			new TypeMapping("uint2", "uint[]", "SetIntParam"),
			new TypeMapping("uint3", "uint[]", "SetIntParam"),
			new TypeMapping("uint4", "uint[]", "SetIntParam"),
			new TypeMapping("min16uint", "int", "SetIntParam"),
			new TypeMapping("min16uint2", "uint[]", "SetVectorParam"),
			new TypeMapping("min16uint3", "uint[]", "SetVectorParam"),
			new TypeMapping("min16uint4", "uint[]", "SetIntParam"),
			new TypeMapping("float", "float", "SetFloatParam"),
			new TypeMapping("float2", "Vector2", "SetVectorParam"),
			new TypeMapping("float3", "Vector3", "SetVectorParam"),
			new TypeMapping("float4", "Vector4", "SetVectorParam"),
			new TypeMapping("float4x4", "Matrix4x4", "SetMatrixParam"),
			new TypeMapping("min10float", "float", "SetFloatParam"),
			new TypeMapping("min10float2", "Vector2", "SetVectorParam"),
			new TypeMapping("min10float3", "Vector3", "SetVectorParam"),
			new TypeMapping("min10float4", "Vector4", "SetVectorParam"),
			new TypeMapping("min10float4x4", "Matrix4x4", "SetMatrixParam"),
			new TypeMapping("min16float", "float", "SetFloatParam"),
			new TypeMapping("min16float2", "Vector2", "SetVectorParam"),
			new TypeMapping("min16float3", "Vector3", "SetVectorParam"),
			new TypeMapping("min16float4", "Vector4", "SetVectorParam"),
			new TypeMapping("min16float4x4", "Matrix4x4", "SetMatrixParam"),
			new TypeMapping("half", "float", "SetFloatParam"),
			new TypeMapping("half2", "Vector2", "SetVectorParam"),
			new TypeMapping("half3", "Vector3", "SetVectorParam"),
			new TypeMapping("half4", "Vector4", "SetVectorParam"),
			new TypeMapping("Texture2D", "RenderTargetIdentifier", "SetTextureParam"),
			new TypeMapping("RWTexture2D", "RenderTargetIdentifier", "SetTextureParam"),
			new TypeMapping("StructuredBuffer", "ComputeBuffer", "SetBufferParam"),
			new TypeMapping("RWStructuredBuffer", "ComputeBuffer", "SetBufferParam"),
			new TypeMapping("AppendStructuredBuffer", "ComputeBuffer", "SetBufferParam"),
			new TypeMapping("ConsumeStructuredBuffer", "ComputeBuffer", "SetBufferParam"),
		};

	}
}