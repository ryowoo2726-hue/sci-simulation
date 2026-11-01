using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupProject
{
    [MenuItem("Heart/Generate and validate scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Resources");Directory.CreateDirectory("Assets/Rendering");
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Rendering/HeartRenderer.asset");
        if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Rendering/HeartRenderer.asset");}
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Rendering/HeartURP.asset");
        if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,"Assets/Rendering/HeartURP.asset");}
        pipeline.msaaSampleCount=4;pipeline.renderScale=1;pipeline.supportsHDR=true;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.companyName="Maseok Science";PlayerSettings.productName="심장의 혈액 순환";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(string shaderName in new[]{"Heart/LivingTissue","Heart/AnatomyGhost","Heart/GuideOverlay"}){
            Shader shader=Shader.Find(shaderName);bool found=false;for(int i=0;i<shaders.arraySize;i++)found|=shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader;
            if(!found){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader;}
        }
        graphics.ApplyModifiedProperties();
        ExportModel();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var preview=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/HeartAnatomy.prefab"));preview.name="Editor anatomy preview";
        var previewLight=new GameObject("Preview light").AddComponent<Light>();previewLight.transform.SetParent(preview.transform);previewLight.type=LightType.Directional;previewLight.transform.rotation=Quaternion.Euler(32,-30,0);previewLight.intensity=1.3f;
        new GameObject("심장 혈액 순환 · 3D").AddComponent<Circulation>();EditorSceneManager.SaveScene(scene,"Assets/Circulation.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Circulation.unity",true)};AssetDatabase.SaveAssets();Validate();
    }
    static void ExportModel()
    {
        Directory.CreateDirectory("Assets/Models");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Anatomical Heart · 40 mm per unit");var simulation=root.AddComponent<Circulation>();
        typeof(Circulation).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(simulation,null);
        simulation.SetView(1);
        if(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/HeartMyocardium.asset")!=null)
            AssetDatabase.MoveAsset("Assets/Resources/HeartMyocardium.asset","Assets/Models/HeartMyocardium.asset");
        Mesh stored=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/HeartMyocardium.asset");if(stored==null)AssetDatabase.CreateAsset(simulation.body,"Assets/Models/HeartMyocardium.asset");else{EditorUtility.CopySerialized(simulation.body,stored);EditorUtility.SetDirty(stored);}
        foreach(Transform t in root.GetComponentsInChildren<Transform>())if(t.name.Contains("적혈구")||t.name.StartsWith("실시간 절개면")||t.name.StartsWith("추적하는"))UnityEngine.Object.DestroyImmediate(t.gameObject);
        foreach(var line in root.GetComponentsInChildren<LineRenderer>())if(line.sharedMaterial.shader.name=="Heart/GuideOverlay")UnityEngine.Object.DestroyImmediate(line.gameObject);
        string partsPath="Assets/Models/AnatomyParts.asset";bool containerCreated=false;
        void Persist(UnityEngine.Object asset){if(asset==null||AssetDatabase.Contains(asset))return;if(!containerCreated){AssetDatabase.CreateAsset(asset,partsPath);containerCreated=true;}else AssetDatabase.AddObjectToAsset(asset,partsPath);}
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())Persist(filter.sharedMesh);
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)Persist(mat);
        foreach(var script in root.GetComponentsInChildren<MonoBehaviour>())UnityEngine.Object.DestroyImmediate(script);
        AssetDatabase.SaveAssets();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Models/HeartAnatomy.prefab");
        // A portable mesh can also be opened in Blender or other modeling applications.
        var body=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/HeartMyocardium.asset");var obj=new System.Text.StringBuilder("# Heart myocardium. One unit = 40 mm.\n");
        foreach(Vector3 v in body.vertices)obj.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0} {1} {2}\n",v.x,v.y,v.z);
        int[] triangles=body.triangles;for(int i=0;i<triangles.Length;i+=3)obj.AppendFormat("f {0} {1} {2}\n",triangles[i]+1,triangles[i+1]+1,triangles[i+2]+1);
        File.WriteAllText("Assets/Models/HeartMyocardium.obj",obj.ToString());AssetDatabase.Refresh();
    }
    [MenuItem("Heart/Validate")]
    public static void Validate()
    {
        void Check(bool ok,string why){if(!ok)throw new Exception("Heart validation failed: "+why);}
        foreach(float t in new[]{0f,.08f,.149f,.15f,.3f,.449f,.45f,.7f,.999f}){Vector4 s=CardiacCycleController.Evaluate(t);Check(t<.15f?s.x==1&&s.y==0:t<.45f?s.x==0&&s.y==1:s.x==.6f&&s.y==0,"valve phase "+t);}
        Check(SplineBloodFlowController.GateAdvance(.9f,1.1f,1,false)<1,"closed valve blocks crossing");Check(SplineBloodFlowController.GateAdvance(.9f,1.1f,1,true)>1,"open valve allows crossing");
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);Mesh first=cube.GetComponent<MeshFilter>().sharedMesh,second=UnityEngine.Object.Instantiate(first);Vector3[] shifted=second.vertices;for(int i=0;i<shifted.Length;i++)shifted[i]+=Vector3.right*.5f;second.vertices=shifted;
        Mesh overlap=AtlasHeart.Combine(new[]{first,second},"Cap union regression"),cap=AtlasHeart.Cap(overlap,0);float area=0;Vector3[] cv=cap.vertices;int[] ct=cap.triangles;
        for(int i=0;i<ct.Length;i+=3)area+=Vector3.Cross(cv[ct[i+1]]-cv[ct[i]],cv[ct[i+2]]-cv[ct[i]]).magnitude*.5f;
        UnityEngine.Object.DestroyImmediate(cube);UnityEngine.Object.DestroyImmediate(second);UnityEngine.Object.DestroyImmediate(overlap);UnityEngine.Object.DestroyImmediate(cap);Check(Mathf.Abs(area-1.5f)<.005f,"overlapping tissue forms a union, not a false gap");
        var atlas=AtlasHeart.Load();Check(atlas.Count>=30,"named atlas structures");
        foreach(string name in AtlasHeart.ChamberNames){Check(atlas.ContainsKey(name),name);Check(atlas[name].vertexCount>5000,"subdivided chamber mesh "+name);}
        foreach(string n in new[]{"Left coronary leaflet","Right coronary leaflet","Non-coronary leaflet","Anterior semilunar leaflet of pulmonary valve","Left semilunar leaflet of pulmonary valve","Right semilunar leaflet of pulmonary valve"})Check(atlas.ContainsKey(n),"native valve cusp "+n);
        Check(HeartLearningController.Titles.Length==12&&HeartLearningController.Explanation.Length==12,"twelve learning steps");
        foreach(var q in HeartLearningController.Questions){Check(q.correct>=0&&q.correct<q.choices.Length,"quiz answer");Check(q.reviewStep>=0&&q.reviewStep<12,"quiz review route");}
        Check(SplineBloodFlowController.RightRoute[3]==AtlasHeart.Tricuspid&&SplineBloodFlowController.RightRoute[7]==AtlasHeart.Pulmonary,"right route crosses its valves");
        Check(SplineBloodFlowController.LeftRoute[3]==AtlasHeart.Mitral&&SplineBloodFlowController.LeftRoute[7]==AtlasHeart.Aortic,"left route crosses its valves");
        foreach(string name in new[]{"Heart/LivingTissue","Heart/AnatomyGhost","Heart/GuideOverlay"})Check(Shader.Find(name)!=null&&!ShaderUtil.ShaderHasError(Shader.Find(name)),"shader "+name);
        Debug.Log("HEART_V2_VALIDATION_PASS · native atlas, phase gates, 12 learning steps, quiz, valve routes and shaders");
    }
    [MenuItem("Heart/Build Windows")]
    public static void Build()
    {
        CreateScene();Directory.CreateDirectory("Build/HeartLab");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Circulation.unity"},locationPathName="Build/HeartLab/HeartLab.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);Debug.Log("HEART_BUILD_PASS · "+report.summary.totalSize+" bytes");
    }
    [MenuItem("Heart/Build WebGL")]
    public static void BuildWeb()
    {
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.initialMemorySize=256;
        CreateScene();
        // Runtime reconstructs anatomy from Atlas/Heart; omit the duplicate editor preview.
        var webScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Heart Web runtime").AddComponent<Circulation>();
        EditorSceneManager.SaveScene(webScene,"Assets/HeartWeb.unity");
        string output=Path.GetFullPath("Build/HeartWeb");Directory.CreateDirectory(output);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HeartWeb.unity"},locationPathName=output,target=BuildTarget.WebGL,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed: "+report.summary.result);
        Debug.Log("HEART_WEB_BUILD_PASS · "+report.summary.totalSize+" bytes · "+output);
    }
}

