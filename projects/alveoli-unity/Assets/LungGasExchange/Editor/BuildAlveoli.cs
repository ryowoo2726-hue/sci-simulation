using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildAlveoli
{
    [InitializeOnLoadMethod]
    static void FirstImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode || File.Exists("ProjectSettings/AlveoliReady.txt") || EditorApplication.isCompiling) return;
            CreateScene();
            File.WriteAllText("ProjectSettings/AlveoliReady.txt", "Scene and render pipeline initialized.");
        };
    }
    [MenuItem("Alveoli/Create or rebuild simulation scene")]
    public static void CreateScene()
    {
        var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders = graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach (var shaderName in new[] { "Heart/LivingTissue", "Heart/AnatomyGhost" })
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception("Missing shader: " + shaderName);
            bool included = false;
            for (int i = 0; i < shaders.arraySize; i++)
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) included = true;
            if (!included) { int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index); shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/LungGasExchange/Rendering/HeartURP.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        PlayerSettings.companyName = "Science Classroom"; PlayerSettings.productName = "허파꽈리 기체 교환";
        PlayerSettings.colorSpace = ColorSpace.Linear; PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Alveolar gas exchange", typeof(AlveolarSimulation));
        Directory.CreateDirectory("Assets/LungGasExchange/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/LungGasExchange/Scenes/AlveolarGasExchange.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scene.path, true) };
        AssetDatabase.SaveAssets();
        // Exercise reused geometry and the two original molecule datasets in the actual Unity runtime.
        var mesh = AlveolarGeometry.RedCell();
        if (mesh.vertexCount < 500 || mesh.bounds.size.y >= mesh.bounds.size.x) throw new Exception("Invalid biconcave red cell");
        if (AlveolarGeometry.ReadAtoms("Molecules/Oxygen").Length != 2) throw new Exception("Invalid O2");
        Debug.Log("ALVEOLI_EDITOR_CHECK_PASS");
    }
    public static void BuildWebGL()
    {
        CreateScene();
        // GitHub Pages does not configure Unity's compressed-content response headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.WebGL, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.WebGL.initialMemorySize = 128;
#if UNITY_WEBGL
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
#endif
        var output = Path.GetFullPath("../Alveoli-WebGL");
        Directory.CreateDirectory(output);
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, output, BuildTarget.WebGL, BuildOptions.None);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("WebGL build failed: " + result.summary.result);
        Debug.Log("ALVEOLI_WEBGL_BUILD_PASS");
    }
    public static void Build()
    {
        CreateScene();
        Directory.CreateDirectory("../Alveoli-Windows");
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "../Alveoli-Windows/Alveoli.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: " + result.summary.result);
        Debug.Log("ALVEOLI_BUILD_PASS");
    }
}
