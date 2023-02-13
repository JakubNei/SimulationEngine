using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShaderMetadataGenerator
{
	class Generator
	{
		public IEnumerable<string> GenerateLines(ParsedFile file)
		{
			if (file.FileExtension == "compute")
				return GenerateComputeLines(file);
			if (file.FileExtension == "cginc")
				return GenerateComputeCgincLines(file);

			return new string[] { "// error file extensions not supported " + file.FileExtension };
		}

		IEnumerable<string> GenerateStructs(ParsedFile file)
		{
			yield return "	#region Structs";
			foreach (var struc in file.structs)
			{

				yield return "	[System.Serializable]";
				yield return "	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]";
				yield return "	public struct Struct" + struc.name;
				yield return "	{";
				foreach (var variable in struc.variables)
				{
					if (string.IsNullOrEmpty(variable.type.CSharpType))
						continue;
					if (!string.IsNullOrEmpty(variable.comment))
						yield return "		// " + variable.comment;
					yield return "		public " + variable.type.CSharpType + " " + variable.name + ";";
				}
				yield return "		public static int _Size => System.Runtime.InteropServices.Marshal.SizeOf(typeof(Struct" + struc.name + "));";
				yield return "	}";
			}
			yield return "	#endregion";
		}

		IEnumerable<string> GenerateComputeLines(ParsedFile file)
		{
			/*
			using UnityEngine;
			using UnityEngine.Rendering;

			public struct CellSimulationCompute 
			{	
				Vector3 kernelSize;
				ComputeShaderExecution execution;

				public static CellSimulationCompute SaveData(CommandBuffer commandBuffer, ComputeShader computeShader)
				{
					return new CellSimulationCompute()
					{
						execution = ComputeShaderExecution.For(commandBuffer, computeShader, "SaveData"),
						kernelSize = new Vector3(8, 1, 1),
					};
				}

				public int SaveDataBufferWidth { set => execution.SetIntParam("SaveDataBufferWidth", value); }

				public void Dispatch(uint x, uint y = 1, uint z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));
			}
			*/
			yield return "using UnityEngine;";
			yield return "using UnityEngine.Rendering;";
			yield return "";
			yield return "// Generated from " + file.SourceFileName;
			yield return "public struct " + file.ComputeStructName;
			yield return "{";
			yield return "	Vector3 kernelSize;";
			yield return "	ComputeShaderExecution execution;";
			yield return "";
			const string resourcesFolder = "/Resources/";
			var canLoadComputeShaderFromResources = false;
			{
				var i = file.sourceFileFullPath.IndexOf(resourcesFolder);
				if (i != -1)
				{
					var pathInResources = file.sourceFileFullPath.Substring(i + resourcesFolder.Length);
					pathInResources = pathInResources.Substring(0, pathInResources.Length - ".compute".Length);
					// OPTIMIZATION: computeShader comparison with null takes too long, because it also checks validity
					// so we use bool if (!computeShaderLoaded) instead of ComputeShader if (computeShaderInstance == null)
					yield return "	static bool computeShaderLoaded;";
					yield return "	static ComputeShader computeShaderInstance;";
					yield return "	public static ComputeShader ComputeShader";
					yield return "	{";
					yield return "		get";
					yield return "		{";
					yield return "			if (!computeShaderLoaded)";
					yield return "			{ ";
					yield return "				computeShaderInstance = Resources.Load<ComputeShader>(\"" + pathInResources + "\");";
					yield return "				if (computeShaderInstance)";
					yield return "					computeShaderLoaded = true;";
					yield return "			}";
					yield return "			return computeShaderInstance;";
					yield return "		}";
					yield return "	}";
					canLoadComputeShaderFromResources = true;
				}
			}
			yield return "";
			yield return "";
			yield return "	#region Kernels that can be executed";
			foreach (var kernelName in file.pragmaKernel)
			{
				Vector3Int kernelSize;
				if (!file.kernelNameToKernelNumThreads.TryGetValue(kernelName, out kernelSize))
					continue;
				var functionDeclaration = file.functions.FirstOrDefault(f => f.name == kernelName);
				var comment = functionDeclaration == null ? null : functionDeclaration.comment;
				if (canLoadComputeShaderFromResources)
				{
					yield return "	// Kernel " + kernelName + " with [numthreads(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ")]";
					if (!string.IsNullOrWhiteSpace(comment))
						yield return "	// " + comment;
					yield return "	public static " + file.ComputeStructName + " " + kernelName + "(CommandBuffer commandBuffer)";
					yield return "	{";
					yield return "		return " + kernelName + "(commandBuffer, ComputeShader);";
					yield return "	}";
				}
				yield return "	// Kernel " + kernelName + " with [numthreads(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ")]";
				if (!string.IsNullOrWhiteSpace(comment))
					yield return "	// " + comment;
				yield return "	public static " + file.ComputeStructName + " " + kernelName + "(CommandBuffer commandBuffer, ComputeShader computeShader)";
				yield return "	{";
				yield return "		return new " + file.ComputeStructName + "()";
				yield return "		{";
				yield return "			execution = ComputeShaderExecution.For(commandBuffer, computeShader, \"" + kernelName + "\"),";
				yield return "			kernelSize = KernelSize." + kernelName + ",";
				yield return "		};";
				yield return "	}";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Include files";
			foreach (var include in file.includes)
			{
				if (include.parsedFile == null)
					continue;
				yield return "	// " + include.comment;
				yield return "	public " + include.parsedFile.CgincStructName + " " + include.parsedFile.CgincStructName + " { get { return new " + include.parsedFile.CgincStructName + "(execution); } }";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region All setttable uniforms including those from all include files";
			var propertyIdNeeded = new HashSet<string>();
			foreach (var variable in file.GatherAllGlobalVariablesInAllIncludes())
			{
				var variableName = variable.name;

				if (variable.type.CSharpType == null || variable.type.ComputeShaderSetFunctionName == null)
					continue;
				if (!string.IsNullOrEmpty(variable.comment))
					yield return "	// " + variable.comment;
				propertyIdNeeded.Add(variableName);
				yield return "	public " + variable.type.CSharpType + " " + variableName + " { set => execution." + variable.type.ComputeShaderSetFunctionName + "(PropertyId." + variableName + ", value); }";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			foreach (var line in GenerateStructs(file))
				yield return line;
			yield return "";
			yield return "";
			yield return "	#region Dispatch call with convenience parameters";
			yield return "	public void Dispatch(int x, int y = 1, int z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));";
			yield return "	public void Dispatch(uint x, uint y = 1, uint z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));";
			yield return "	public void Dispatch(ComputeBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);";
			yield return "	public void Dispatch(GraphicsBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);";
			yield return "	public void Dispatch(Vector2Int xy) => Dispatch(xy.x, xy.y);";
			yield return "	public void Dispatch(Vector3Int xyz) => Dispatch(xyz.x, xyz.y, xyz.z);";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Kernel sizes";
			yield return "	public static class KernelSize";
			yield return "	{";
			foreach (var pair in file.kernelNameToKernelNumThreads)
			{
				var kernelName = pair.Key;
				var kernelSize = pair.Value;
				yield return "		public static readonly Vector3Int " + kernelName + " = new Vector3Int(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ");";
			}
			yield return "	}";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Hashed property ids";
			yield return "	public static class PropertyId";
			yield return "	{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "		public static readonly int " + property + ";";
			}
			yield return "		static PropertyId()";
			yield return "		{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "			" + property + " = Shader.PropertyToID(\"" + property + "\");";
			}
			yield return "		}";
			yield return "	}";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "}";
		}

		IEnumerable<string> GenerateComputeCgincLines(ParsedFile file)
		{
			/*
			using UnityEngine;
			using UnityEngine.Rendering;

			public struct ChunksAtlasCginc 
			{	
				ComputeShaderExecution execution;

				public ChunksAtlasCginc(ComputeShaderExecution execution)
				{
					this.execution = execution;
				}

				public int SaveDataBufferWidth { set => execution.SetIntParam("SaveDataBufferWidth", value); }
			}
			*/
			yield return "using UnityEngine;";
			yield return "using UnityEngine.Rendering;";
			yield return "";
			yield return "// Generated from " + file.SourceFileName;
			yield return "public struct " + file.CgincStructName;
			yield return "{";
			yield return "	ComputeShaderExecution execution;";
			yield return "";
			yield return "	public " + file.CgincStructName + "(ComputeShaderExecution execution)";
			yield return "	{";
			yield return "		this.execution = execution;";
			yield return "	}";
			yield return "";
			yield return "";
			yield return "	#region All setttable uniforms including those from all include files";
			var propertyIdNeeded = new HashSet<string>();
			foreach (var variable in file.globalVariables)
			{
				var variableName = variable.name;
				string cSharpType = null;
				string computeShaderExecution = null;
				foreach (var typeMapping in TypeMappings.typeMappings)
				{
					if (typeMapping.shaderType == variable.type.type)
					{
						cSharpType = typeMapping.cSharpType;
						computeShaderExecution = typeMapping.function;
					}
				}
				if (cSharpType == null || computeShaderExecution == null)
					continue;
				if (!string.IsNullOrEmpty(variable.comment))
					yield return "	// " + variable.comment;
				propertyIdNeeded.Add(variableName);
				yield return "	public " + cSharpType + " " + variableName + " { set => execution." + computeShaderExecution + "(PropertyId." + variableName + ", value); }";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			foreach (var line in GenerateStructs(file))
				yield return line;
			yield return "";
			yield return "";
			yield return "	#region Hashed property ids";
			yield return "	public static class PropertyId";
			yield return "	{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "		public static readonly int " + property + ";";
			}
			yield return "		static PropertyId()";
			yield return "		{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "			" + property + " = Shader.PropertyToID(\"" + property + "\");";
			}
			yield return "		}";
			yield return "	}";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "}";
		}

		IEnumerable<string> GenerateShaderLines(ParsedFile file)
		{
			/*
			using UnityEngine;
			using UnityEngine.Rendering;

			public struct DrawParticlesShader
			{	
				Vector3 kernelSize;
				ComputeShaderExecution execution;

				public static DrawParticlesShaderSaveData(CommandBuffer commandBuffer, ComputeShader computeShader)
				{
					return new CellSimulationCompute()
					{
						execution = ComputeShaderExecution.For(commandBuffer, computeShader, "SaveData"),
						kernelSize = new Vector3(8, 1, 1),
					};
				}

				public int SaveDataBufferWidth { set => execution.SetIntParam("SaveDataBufferWidth", value); }

				public void Dispatch(uint x, uint y = 1, uint z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));
			}
			*/
			yield return "using UnityEngine;";
			yield return "using UnityEngine.Rendering;";
			yield return "";
			yield return "// Generated from " + file.SourceFileName;
			yield return "public struct " + file.ShaderStructName;
			yield return "{";
			yield return "	Vector3 kernelSize;";
			yield return "	ComputeShaderExecution execution;";
			yield return "";
			const string resourcesFolder = "/Resources/";
			var canLoadComputeShaderFromResources = false;
			{
				var i = file.sourceFileFullPath.IndexOf(resourcesFolder);
				if (i != -1)
				{
					var pathInResources = file.sourceFileFullPath.Substring(i + resourcesFolder.Length);
					pathInResources = pathInResources.Substring(0, pathInResources.Length - ".compute".Length);
					// OPTIMIZATION: computeShader comparison with null takes too long, because it also checks validity
					// so we use bool if (!computeShaderLoaded) instead of ComputeShader if (computeShaderInstance == null)
					yield return "	static bool computeShaderLoaded;";
					yield return "	static ComputeShader computeShaderInstance;";
					yield return "	public static ComputeShader ComputeShader";
					yield return "	{";
					yield return "		get";
					yield return "		{";
					yield return "			if (!computeShaderLoaded)";
					yield return "			{ ";
					yield return "				computeShaderInstance = Resources.Load<ComputeShader>(\"" + pathInResources + "\");";
					yield return "				if (computeShaderInstance)";
					yield return "					computeShaderLoaded = true;";
					yield return "			}";
					yield return "			return computeShaderInstance;";
					yield return "		}";
					yield return "	}";
					canLoadComputeShaderFromResources = true;
				}
			}
			yield return "";
			yield return "";
			yield return "	#region Kernels that can be executed";
			foreach (var kernelName in file.pragmaKernel)
			{
				Vector3Int kernelSize;
				if (!file.kernelNameToKernelNumThreads.TryGetValue(kernelName, out kernelSize))
					continue;
				var functionDeclaration = file.functions.FirstOrDefault(f => f.name == kernelName);
				var comment = functionDeclaration == null ? null : functionDeclaration.comment;
				if (canLoadComputeShaderFromResources)
				{
					yield return "	// Kernel " + kernelName + " with [numthreads(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ")]";
					if (!string.IsNullOrWhiteSpace(comment))
						yield return "	// " + comment;
					yield return "	public static " + file.ComputeStructName + " " + kernelName + "(CommandBuffer commandBuffer)";
					yield return "	{";
					yield return "		return " + kernelName + "(commandBuffer, ComputeShader);";
					yield return "	}";
				}
				yield return "	// Kernel " + kernelName + " with [numthreads(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ")]";
				if (!string.IsNullOrWhiteSpace(comment))
					yield return "	// " + comment;
				yield return "	public static " + file.ComputeStructName + " " + kernelName + "(CommandBuffer commandBuffer, ComputeShader computeShader)";
				yield return "	{";
				yield return "		return new " + file.ComputeStructName + "()";
				yield return "		{";
				yield return "			execution = ComputeShaderExecution.For(commandBuffer, computeShader, \"" + kernelName + "\"),";
				yield return "			kernelSize = KernelSize." + kernelName + ",";
				yield return "		};";
				yield return "	}";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Include files";
			foreach (var include in file.includes)
			{
				if (include.parsedFile == null)
					continue;
				yield return "	// " + include.comment;
				yield return "	public " + include.parsedFile.CgincStructName + " " + include.parsedFile.CgincStructName + " { get { return new " + include.parsedFile.CgincStructName + "(execution); } }";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region All setttable uniforms including those from all include files";
			var propertyIdNeeded = new HashSet<string>();
			foreach (var variable in file.GatherAllGlobalVariablesInAllIncludes())
			{
				var variableName = variable.name;

				if (variable.type.CSharpType == null || variable.type.ComputeShaderSetFunctionName == null)
					continue;
				if (!string.IsNullOrEmpty(variable.comment))
					yield return "	// " + variable.comment;
				propertyIdNeeded.Add(variableName);
				yield return "	public " + variable.type.CSharpType + " " + variableName + " { set => execution." + variable.type.ComputeShaderSetFunctionName + "(PropertyId." + variableName + ", value); }";
			}
			yield return "	#endregion";
			yield return "";
			yield return "";
			foreach (var line in GenerateStructs(file))
				yield return line;
			yield return "";
			yield return "";
			yield return "	#region Dispatch call with convenience parameters";
			yield return "	public void Dispatch(int x, int y = 1, int z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));";
			yield return "	public void Dispatch(uint x, uint y = 1, uint z = 1) => execution.Dispatch(Mathf.CeilToInt(x / kernelSize.x), Mathf.CeilToInt(y / kernelSize.y), Mathf.CeilToInt(z / kernelSize.z));";
			yield return "	public void Dispatch(ComputeBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);";
			yield return "	public void Dispatch(GraphicsBuffer indirectBuffer, uint argsOffset = 0) => execution.Dispatch(indirectBuffer, argsOffset);";
			yield return "	public void Dispatch(Vector2Int xy) => Dispatch(xy.x, xy.y);";
			yield return "	public void Dispatch(Vector3Int xyz) => Dispatch(xyz.x, xyz.y, xyz.z);";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Kernel sizes";
			yield return "	public static class KernelSize";
			yield return "	{";
			foreach (var pair in file.kernelNameToKernelNumThreads)
			{
				var kernelName = pair.Key;
				var kernelSize = pair.Value;
				yield return "		public static readonly Vector3Int " + kernelName + " = new Vector3Int(" + kernelSize.x + ", " + kernelSize.y + ", " + kernelSize.z + ");";
			}
			yield return "	}";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "	#region Hashed property ids";
			yield return "	public static class PropertyId";
			yield return "	{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "		public static readonly int " + property + ";";
			}
			yield return "		static PropertyId()";
			yield return "		{";
			foreach (var property in propertyIdNeeded)
			{
				yield return "			" + property + " = Shader.PropertyToID(\"" + property + "\");";
			}
			yield return "		}";
			yield return "	}";
			yield return "	#endregion";
			yield return "";
			yield return "";
			yield return "}";
		}

	}
}