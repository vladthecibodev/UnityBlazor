
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using System.IO;
using Newtonsoft.Json;

using static System.Array;
using Microsoft.CodeAnalysis;

public class TestScript
{
    // TODO: needs to be where the.razor file is
    private static string _asmdefParentPath = "Assets";
    private static readonly string generatedParentFolder = "__generated__";
    private static readonly string assemblyName = "UniBlazor.UserComponents";

    [MenuItem("Somewhere/AssemblyTest")]
    static void Run()
    {
        var apiVer = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
        Debug.Log(apiVer + "");

        // The generated folder doesn't exist and could not be created
        if (!AssureGeneratedFolderCreation(_asmdefParentPath, out var asmdefParentFolderPath))
        {
            return;
        }

        AssureAsmdefCreation(asmdefParentFolderPath);

        string csFile = @"/* Script is auto-generated*/
using static System.Array;
using UnityEngine;

public partial class TestCsFile 
{
    public void DoTest(string[] flavours)
    {
        ForEach(flavours, (flavour) => Debug.Log(flavour));
    }
} 
";

        var assetScriptPath = Path.Combine(asmdefParentFolderPath, "TestCsFile.g.cs");
        var scriptPath = AssetPathToAbsolute(assetScriptPath);
        File.WriteAllText(scriptPath, csFile);

        CompilationPipeline.assemblyCompilationFinished +=
            OnAssemblyCompilationFinished;

        AssetDatabase.Refresh();

        Debug.Log(EditorApplication.isCompiling ? "EDITOR IS CURRENTLY COMPILING" : "NO COMPILATION");
    }

    private static void OnAssemblyCompilationFinished(string compiledAssemblyPath, CompilerMessage[] _)
    {
        // Assembly name is ending in ".dll"
        // WE HAVE TO WAIT FOR THE COMPILATION TO END!!!
        /*var assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(assetScriptPath);
        assemblyName = assemblyName.Replace(".dll", string.Empty);
        Debug.Log(assemblyName);
        */

        // The compiled assembly path is Library/ScriptAssemblies/{ScriptName}.dll
        if (!compiledAssemblyPath.EndsWith($"{assemblyName}.dll"))
        {
            return;
        }

        // Assembly names without ".dll"
        // var allAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
        //ForEach(allAssemblies, assembly => Debug.Log(assembly.name));

        /* var componentsAssembly = (
             from assembly in AppDomain.CurrentDomain.GetAssemblies()
             where assembly.GetName().Name == assemblyName
             select assembly
         ).FirstOrDefault();

         if (componentsAssembly == null)
         {
             Debug.LogError($"Assembly is not loaded in current domain {assemblyName}");
             return;
         }

         Debug.Log(componentsAssembly.FullName);*/
        Debug.LogWarning(compiledAssemblyPath);
        MetadataReference userComponentsMetadata = MetadataReference.CreateFromFile(AssetPathToAbsolute(compiledAssemblyPath));

        Debug.Log("");
    }

    /// <summary>
    ///     Makes sure the "__generated__" folder is created
    ///     or creates it if not previously created
    /// </summary>
    /// <param name="parentFolderPath">
    ///     The path of the folder containing the razor component(s)
    ///     MUST NOT CONTAIN A TRAILING "/"
    /// </param>
    /// <returns>
    ///     true if it managed to create the folder or the folder previously existed
    /// </returns>
    private static bool AssureGeneratedFolderCreation(string parentFolderPath, out string asmdefParentFolderPath)
    {
        // Assets/[...]/__generated__
        asmdefParentFolderPath = Path.Combine(parentFolderPath, generatedParentFolder);

        if (AssetDatabase.IsValidFolder(asmdefParentFolderPath))
        {
            return true;
        }

        var createdGuid = AssetDatabase.CreateFolder(
            parentFolderPath,
            generatedParentFolder
        );

        if (createdGuid == string.Empty)
        {
            Debug.LogError(
                $"The creation of the generated components' parent folder failed at {asmdefParentFolderPath}"
            );

            return false;
        }

        return true;
    }

    // asmdefParentFolderPath is something like Assets/[...]/__generated__
    private static void AssureAsmdefCreation(string asmdefParentFolderPath)
    {
        // Assets/[...]/__generated__/{assemblyName}.asmdef
        string fullAssetAsmdefPath = Path.Combine(asmdefParentFolderPath, $"{assemblyName}.asmdef");

        // First, check if the assembly definition file has already been created
        var assetType = AssetDatabase.GetMainAssetTypeAtPath(fullAssetAsmdefPath);
        if (assetType == typeof(AssemblyDefinitionAsset))
        {
            return;
        }

        string jsonAsmdef = JsonConvert.SerializeObject(
            new AsmdefJson() { name = assemblyName },
            Formatting.Indented
        );

        // To be able to write to disk, we need the absolute path to the asmdef file
        var absoluteAsmdefPath = AssetPathToAbsolute(fullAssetAsmdefPath);
        File.WriteAllText(absoluteAsmdefPath, jsonAsmdef);
    }

    private static string AssetPathToAbsolute(string assetPath)
    {
        return Path.Combine(
            Application.dataPath.Replace("Assets", string.Empty),
            assetPath
        );
    }
}

public record AsmdefJson {
    public string name = string.Empty;
    // Currently, asmdef props other than name not needed
    public string rootNamespace = string.Empty;
    public string[] references = Empty<string>();
};