using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShaderMetadataGenerator
{
	// WARNING: don't use any new C# features, because this CS script is executed with PowerShell
	public static class Main
	{

		public static bool FindAndProcessAllFiles(string assetsFolderFullPath)
		{
			var generatedFilesDirectory = Path.Combine(assetsFolderFullPath, "ShaderMetadata", "Generated");

			var dir = new DirectoryInfo(generatedFilesDirectory);
			foreach (var file in dir.EnumerateFiles("*.cs"))
				file.Delete();

			var files = new List<string>();
			files.AddRange(Directory.GetFiles(assetsFolderFullPath, "*.compute", SearchOption.AllDirectories));
			files.AddRange(Directory.GetFiles(assetsFolderFullPath, "*.cginc", SearchOption.AllDirectories));
			//files.AddRange(Directory.GetFiles(assetsFolderFullPath, "*.shader", SearchOption.AllDirectories));
			return TryProcessFiles(generatedFilesDirectory, files);
		}

		private static IEnumerable<string> AddNewLine(IEnumerable<string> lines)
		{
			foreach (var line in lines)
			{
				yield return line + CharacterStream.NewLine;
			}
		}

		public static bool TryProcessFiles(string generatedFilesDirectory, List<string> sourceFilesFullPath)
		{
			Directory.CreateDirectory(generatedFilesDirectory);

			var pathToFiles = new Dictionary<string, ParsedFile>();

			for (int i = 0; i < sourceFilesFullPath.Count; i++)
			{
				var sourceFileFullPath = sourceFilesFullPath[i];
				sourceFileFullPath = Path.GetFullPath(sourceFileFullPath);
				sourceFileFullPath = sourceFileFullPath.Replace('\\', '/');
				Console.WriteLine();
				Console.Write("Parsing " + Path.GetFileName(sourceFileFullPath));
				try
				{
					var fetchNext = AddNewLine(File.ReadLines(sourceFileFullPath));
					var parser = new Parser();
					var file = parser.Parse(sourceFileFullPath, fetchNext);
					pathToFiles.Add(sourceFileFullPath, file);
					Console.Write(" ... OK");
				}
				catch (Exception e)
				{
					Console.Write(" ... Exception");
					Console.WriteLine(e);
				}
			}
			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("Linking include files");
			// link includes with parsed files
			foreach (var pathToFile in pathToFiles)
			{
				var file1 = pathToFile.Value;
				foreach (var file1include in file1.includes)
				{
					foreach (var pathToFile2 in pathToFiles)
					{
						var file2 = pathToFile2.Value;
						if (file2.SourceFileName == file1include.name)
						{
							file1include.parsedFile = file2;
							break;
						}
					}
				}
			}

			Console.WriteLine();
			Console.WriteLine("Resolving variable types with defines");
			// resolve variable types in case they are declared with #define
			{
				foreach (var pathToFile in pathToFiles)
				{
					var file = pathToFile.Value;
					var defines = file.GatherAllDefinesInAllIncludes();
					foreach (var v in file.globalVariables)
						v.type.ResolveDefines(defines);
					foreach (var s in file.structs)
						foreach (var v in s.variables)
							v.type.ResolveDefines(defines);
				}
			}

			foreach (var pathToFile in pathToFiles)
			{
				var file = pathToFile.Value;
				Console.WriteLine();
				Console.Write("Generating " + file.SourceFileName);
				try
				{
					var outFile = Path.Combine(generatedFilesDirectory, file.GeneratedFileName);
					File.Delete(outFile);
					var generator = new Generator();
					File.WriteAllLines(outFile, generator.GenerateLines(file));
					Console.Write(" ... OK");
				}
				catch (Exception e)
				{
					Console.Write(" ... Exception");
					Console.WriteLine(e);
				}
			}
			Console.WriteLine();

			return true;
		}

	}

}