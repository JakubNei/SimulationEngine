using System.Collections.Generic;
using System.Linq;

namespace ShaderMetadataGenerator
{
	class Parser
	{
		ParsedFile result;
		Vector3Int? lastKernelNumThreads = null;

		CharacterStream stream;

		// HLSL grammar https://github.com/tgjones/hlsl-parser-nitra/blob/master/src/HlslParser/HlslGrammar.nitra
		public ParsedFile Parse(string fileFullPath, IEnumerable<string> fetchNext)
		{
			result = new ParsedFile();
			result.sourceFileFullPath = fileFullPath;
			stream = new CharacterStream(fetchNext.GetEnumerator());

			if (result.FileExtension.ToLowerInvariant() == ".shader")
			{
				stream.EatAllUntilAndExclude("CGPROGRAM");
				var contents = stream.EatAllUntilAndExclude("ENDCG");
				stream = new CharacterStream(contents);
			}

			TryEatTopLevel();

			return result;
		}

		static Type EatType(CharacterStream stream)
		{
			var result = new Type();
			var a = stream.EatAllUntiWhiteSpace();

			var parts = a.Split('<', ',', '>');

			result.type = parts[0];
			if (parts.Length > 1)
				result.templateTypes = parts.Skip(1).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();

			return result;
		}

		static string TryEatBlock(CharacterStream stream)
		{
			var contents = string.Empty;
			var blocksLevel = 0;
			do
			{
				var start = stream.PeekTryFind("{");
				var end = stream.PeekTryFind("}");
				if (start.HasValue && end.HasValue && start.Value < end.Value)
				{
					++blocksLevel;
					contents += stream.EatAllUntilAndInclude("{");
				}
				else
				{
					--blocksLevel;
					contents += stream.EatAllUntilAndInclude("}");
				}
			} while (blocksLevel > 0);
			return contents;
		}

		static VariableDeclaration EatVariableDeclaration(CharacterStream stream)
		{
			var type = EatType(stream);

			var all = stream.EatAllUntilAndExclude(";");
			var variable = new VariableDeclaration
			{
				name = all,
				type = type,
			};

			var assign = all.IndexOf("=");
			if (assign != -1)
			{
				variable.initialValue = all.Substring(assign).Trim();
				variable.name = all.Substring(0, assign).Trim();
			}

			stream.TryEatAllWhitespaces();

			if (stream.TryEat("//"))
				variable.comment = stream.EatAllUntilNewLine().Trim();

			if (stream.TryEat("/*"))
				variable.comment = stream.EatAllUntilAndExclude("*/").Trim();

			return variable;
		}

		void TryEatTopLevel()
		{
			while (!stream.IsEnd)
			{
				stream.TryEatAllWhitespaces();
				if (stream.IsEnd) break;

				var comment = stream.PeekUntilNewLine();
				if (stream.TryEat("#"))
				{
					if (stream.TryEat("pragma") && stream.TryEatAllWhitespaces() && stream.TryEat("kernel"))
						result.pragmaKernel.Add(stream.EatAllUntilNewLine().Trim());
					else if (stream.TryEat("include"))
						result.includes.Add(new Include() { comment = comment, name = stream.EatAllUntilNewLine().Trim().Trim('\"') });
					else if (stream.TryEat("define"))
					{

						var contents = stream.EatAllUntilNewLine();
						while (contents.Trim().EndsWith("\\") && !stream.IsEnd)
							contents += stream.EatAllUntilNewLine();
						var d = new Define() { sourceContent = contents };
						var s = contents.Trim().Split(' ');
						if (s.Length >= 1)
							d.name = s[0];
						if (s.Length >= 2)
							d.content = string.Join(" ", s.Skip(1).ToArray());
						result.defines.Add(d);
					}
					else
						stream.EatAllUntilNewLine();
				}
				else if (stream.TryEat("["))
				{
					if (stream.TryEat("numthreads("))
					{
						var nums = stream.EatAllUntilAndExclude(")]").Split(',');
						if (nums.Length == 3)
						{
							int x, y, z;
							if (
								int.TryParse(nums[0].Trim(), out x) &&
								int.TryParse(nums[1].Trim(), out y) &&
								int.TryParse(nums[2].Trim(), out z)
							)
								lastKernelNumThreads = new Vector3Int(x, y, z);
						}
					}
				}
				else if (stream.TryEat("struct") || stream.TryEat("class"))
				{
					// struct CellDataStruct
					// {
					// 	MATERIAL_TYPE MaterialId; // 16 bits
					// 	min16uint MoveDirection; // 2 bits
					// 	min16uint Charge; // 3 bits
					// };

					var name = stream.EatAllUntilAndExclude("{").Trim();
					var contents = stream.EatAllUntilAndInclude("};");
					var streamForStruct = new CharacterStream(contents);
					var structDeclaration = new StructDeclaration()
					{
						name = name,
					};

					streamForStruct.TryEatAllWhitespaces();
					while (!streamForStruct.TryEat("};"))
					{
						structDeclaration.variables.Add(EatVariableDeclaration(streamForStruct));
						streamForStruct.TryEatAllWhitespaces();
					}
					result.structs.Add(structDeclaration);
				}
				else if (stream.TryEat("//"))
				{
					stream.EatAllUntilNewLine();
				}
				else if (stream.TryEat("/*"))
				{
					stream.EatAllUntilAndExclude("*/");
				}
				else
				{
					//Texture2D<CELL_DATA_PACKED> CellData;
					//float BlueNoise(float2 U)
					var semicolon = stream.PeekTryFind(";");
					var bracket = stream.PeekTryFind("(");

					// global variable
					if ((semicolon.HasValue && !bracket.HasValue) || (semicolon.HasValue && bracket.HasValue && semicolon.Value < bracket.Value))
					{
						var v = EatVariableDeclaration(stream);
						v.comment = comment.Trim();
						result.globalVariables.Add(v);
					}
					//void UpdateForceField(uint3 DispatchThreadID : SV_DispatchThreadID)
					else
					{
						var type = EatType(stream);
						var functionName = stream.EatAllUntilAndExclude("(");

						var function = new Function()
						{
							comment = comment,
							name = functionName,
							returnType = type,
							contents = TryEatBlock(stream),
						};

						if (type.type == "void" && lastKernelNumThreads.HasValue)
						{
							result.kernelNameToKernelNumThreads.Add(functionName, lastKernelNumThreads.Value);
						}

						result.functions.Add(function);
					}

					lastKernelNumThreads = null;
				}
			}
		}
	}

}