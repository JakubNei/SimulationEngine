using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ShaderMetadataGenerator_UnityHooks
{
	static string RunGeneratorPs1 => Path.Combine(Application.dataPath, "ShaderMetadata", "Generator", "Editor", "RunGenerator.ps1");

	public static bool FindAndProcessAllFiles()
	{
		return ShaderMetadataGenerator.Main.FindAndProcessAllFiles(Application.dataPath);

		// running powershall so generator is possible to run awlays regardless of compilation errors in other code
		// solved this by using different Unity C# assemblies (.asmdef)		
		var p = new Process();
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.CreateNoWindow = true;
		p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
		p.StartInfo.FileName = "powershell";
		p.StartInfo.Arguments = RunGeneratorPs1;
		p.StartInfo.RedirectStandardError = false;
		p.StartInfo.RedirectStandardOutput = false;
		p.StartInfo.UseShellExecute = false;
		p.StartInfo.CreateNoWindow = true;
		p.Start();
		p.WaitForExit();
		return p.ExitCode == 0;
	}

	public static bool TryProcessFiles(string[] sourceFilesFullPath)
	{
		if (!sourceFilesFullPath.Any(f => f.EndsWith(".compute"))) return false;

		return FindAndProcessAllFiles();
	}

	// Add a menu item named "Do Something" to MyMenu in the menu bar.
	[MenuItem("ShaderMetadataGenerator/Process All Files")]
	static void ProcessAllFiles()
	{
		if (FindAndProcessAllFiles())
			AssetDatabase.Refresh();
	}
}

public class ShaderMetadataGenerator_AssetPostprocessor : AssetPostprocessor
{
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
	{
		if (Application.isPlaying)
			return;

		if (ShaderMetadataGenerator_UnityHooks.TryProcessFiles(importedAssets))
			AssetDatabase.Refresh();
	}
}