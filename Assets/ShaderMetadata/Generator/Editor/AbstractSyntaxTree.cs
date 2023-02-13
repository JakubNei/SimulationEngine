using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShaderMetadataGenerator
{
	class ParsedFile
	{
		public string sourceFileFullPath;
		public List<VariableDeclaration> globalVariables = new List<VariableDeclaration>();
		public List<Function> functions = new List<Function>();
		public List<string> pragmaKernel = new List<string>();
		public List<Include> includes = new List<Include>();
		public List<Define> defines = new List<Define>();
		public List<StructDeclaration> structs = new List<StructDeclaration>();
		public Dictionary<string, Vector3Int> kernelNameToKernelNumThreads = new Dictionary<string, Vector3Int>();
		public string SourceFileName { get { return Path.GetFileName(sourceFileFullPath); } }
		public string FileExtension { get { return SourceFileName.Split('.').LastOrDefault() ?? "__error__"; } }
		private string FileName { get { return SourceFileName.Split('.').FirstOrDefault() ?? "__error__"; } }
		public string ComputeStructName { get { return FileName + "Compute"; } }
		public string CgincStructName { get { return FileName + "Cginc"; } }
		public string ShaderStructName { get { return FileName + "Shader"; } }

		public string GeneratedFileName
		{
			get
			{
				var e = FileExtension.ToLowerInvariant();
				if (e == "compute")
					return ComputeStructName + ".cs";
				if (e == "cginc")
					return CgincStructName + ".cs";
				if (e == "shader")
					return ShaderStructName + ".cs";
				return FileName + "__error__.cs";
			}
		}
		/// <summary>
		/// Returns this fie plus all include files
		/// </summary>
		/// <returns></returns>
		public List<ParsedFile> GatherAllRelevantFiles()
		{
			var result = new List<ParsedFile>();
			var filesProcessed = new HashSet<string>();
			var filesToProcess = new List<ParsedFile>();
			result.Add(this);
			filesToProcess.Add(this);
			while (filesToProcess.Count > 0)
			{
				var i = filesToProcess.Count - 1;
				var fileToProcess = filesToProcess[i];
				filesToProcess.RemoveAt(i);
				filesProcessed.Add(fileToProcess.sourceFileFullPath);
				foreach (var include in fileToProcess.includes)
				{
					if (include.parsedFile == null) continue;
					if (filesProcessed.Contains(include.parsedFile.sourceFileFullPath)) continue;
					filesToProcess.Add(include.parsedFile);
					result.Add(include.parsedFile);
				}
			}

			return result;
		}

		public List<VariableDeclaration> GatherAllGlobalVariablesInAllIncludes()
		{
			var result = new List<VariableDeclaration>();
			var gathered = new HashSet<string>();
			foreach (var file in GatherAllRelevantFiles())
			{
				foreach (var v in file.globalVariables)
				{
					if (gathered.Contains(v.name)) continue;
					gathered.Add(v.name);
					result.Add(v);
				}
			}
			return result;
		}

		public List<Define> GatherAllDefinesInAllIncludes()
		{
			var result = new List<Define>();
			var gathered = new HashSet<string>();
			foreach (var file in GatherAllRelevantFiles())
			{
				foreach (var d in file.defines)
				{
					if (gathered.Contains(d.name)) continue;
					gathered.Add(d.name);
					result.Add(d);
				}
			}
			return result;
		}
	}

	class VariableDeclaration
	{
		public string comment;
		public string name;
		public Type type;
		public string initialValue;
	}

	class Type
	{
		public string type;
		public string[] templateTypes = new string[0];
		public string CSharpType
		{
			get
			{
				foreach (var typeMapping in TypeMappings.typeMappings)
					if (typeMapping.shaderType == type)
						return typeMapping.cSharpType;
				return null;
			}
		}
		public string ComputeShaderSetFunctionName
		{
			get
			{
				foreach (var typeMapping in TypeMappings.typeMappings)
					if (typeMapping.shaderType == type)
						return typeMapping.function;
				return null;
			}
		}
		// resolve variable types in case they are declared with #define
		public void ResolveDefines(IEnumerable<Define> defines)
		{
			foreach (var d in defines)
			{
				if (type == d.name)
					type = d.content;
				for (int i = 0; i < templateTypes.Length; i++)
					if (templateTypes[i] == d.name)
						templateTypes[i] = d.content;
			}
		}
	}

	class Function
	{
		public string comment;
		public string name;
		public Type returnType;
		public string contents;
	}

	/// <summary>
	/// #include
	/// </summary>
	class Include
	{
		public string comment;
		public string name;
		public ParsedFile parsedFile;
	}

	/// <summary>
	/// #define <contents>
	/// #define CELL_DATA_PACKED uint
	/// </summary>
	class Define
	{
		public string sourceContent;
		public string name;
		public string content;
	}

	class StructDeclaration
	{
		public string name;
		public List<VariableDeclaration> variables = new List<VariableDeclaration>();
	}


}