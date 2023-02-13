$assetsFolderFullPath = "$PSScriptRoot/../../../"

Add-Type -Path "$PSScriptRoot/Helpers.cs",
"$PSScriptRoot/TypeMapping.cs",
"$PSScriptRoot/CharacterStream.cs",
"$PSScriptRoot/AbstractSyntaxTree.cs",
"$PSScriptRoot/Parser.cs",
"$PSScriptRoot/Generator.cs",
"$PSScriptRoot/Main.cs"

[ShaderMetadataGenerator.Main]::FindAndProcessAllFiles($assetsFolderFullPath)