using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateBloodViewer
{
    static readonly Dictionary<string,string> Labels = new Dictionary<string,string> {
        {"CELL_RBC","적혈구"},{"CELL_Neutrophil","호중구"},{"CELL_Eosinophil","호산구"},
        {"CELL_Basophil","호염기구"},{"CELL_Lymphocyte","림프구"},{"CELL_Monocyte","단핵구"},
        {"CELL_Platelet_Resting","비활성 혈소판"},{"CELL_Platelet_Activated","활성 혈소판"},{"VESSEL_cutaway","혈관 절개 모형"},
        {"SDF_water_H2O_CID962","물"},{"SDF_oxygen_O2_CID977","산소"},{"SDF_carbon_dioxide_CO2_CID280","이산화 탄소"},
        {"SDF_bicarbonate_HCO3_CID769","탄산수소 이온"},{"SDF_glucose_C6H12O6_CID5793","포도당"},
        {"SDF_alanine_amino_acid_CID5950","알라닌"},{"SDF_palmitic_fatty_acid_CID985","팔미트산"},
        {"SDF_glycerol_CID753","글리세롤"},{"SDF_vitaminC_ascorbic_acid_CID54670067","비타민 C"},
        {"SDF_atp_energy_CID5957","ATP"},{"SDF_pyruvic_acid_CID1060","피루브산"},
        {"SDF_lactic_acid_lactate_CID612","젖산"},{"SDF_citric_acid_CID311","시트르산"},
        {"SDF_cholesterol_CID5997","콜레스테롤"},{"SDF_phosphocholine_headgroup_CID65091","포스포콜린"},
        {"SDF_sialic_acid_Neu5Ac_glycocalyx_CID445063","시알산"},{"SDF_protoporphyrin_IX_heme_ring_CID4971","프로토포르피린 IX"},
        {"SDF_bilirubin_pigment_CID5280352","빌리루빈"},{"SDF_epinephrine_adrenaline_CID5816","아드레날린"},
        {"SDF_urea_waste_CID1176","요소"},{"SDF_ammonia_NH3_CID222","암모니아"},
        {"SDF_creatinine_waste_CID588","크레아티닌"},{"SDF_uric_acid_waste_CID1175","요산"},
        {"PDB_1GZX_oxyhemoglobin","산소 결합 헤모글로빈 (1GZX)"},{"PDB_2HHB_deoxyhemoglobin","탈산소 헤모글로빈 (2HHB)"},
        {"PDB_1CA2_carbonic_anhydrase_II","탄산무수화효소 II (1CA2)"},{"PDB_1AFO_glycophorin_A_membrane","글리코포린 A (1AFO)"},
        {"PDB_1AO6_albumin","알부민 (1AO6)"},{"PDB_1E7I_albumin_palmitic_acid_complex","알부민·지방산 복합체 (1E7I)"},
        {"PDB_1A8E_transferrin_iron_carrier","트랜스페린 (1A8E)"},{"PDB_4INS_insulin_hormone","인슐린 (4INS)"},
        {"PDB_1IGT_immunoglobulin_IgG","면역글로불린 G (1IGT)"},{"PDB_1G1Q_P_selectin_PSGL1_rolling","P-셀렉틴 복합체 (1G1Q)"},
        {"PDB_3GHG_fibrinogen","피브리노겐 (3GHG)"},{"PDB_1PPB_thrombin_coagulation","트롬빈 (1PPB)"},
        {"PDB_1AUQ_vWF_A1_platelet_adhesion","폰 빌레브란트 인자 A1 (1AUQ)"},{"PDB_1BKV_collagen_triple_helix","콜라겐 삼중 나선 (1BKV)"}
    };

    [MenuItem("Blood/교육용 관찰 장면 만들기")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var paths = Directory.GetFiles("Assets/BloodPrefabs", "*.prefab");
        if (paths.Length != 46) throw new Exception("46 component prefabs are required.");
        foreach (var guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/BloodModels"}))
        {
            var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
            if (importer != null && !importer.isReadable) { importer.isReadable=true; importer.SaveAndReimport(); }
        }
        Array.Sort(paths, StringComparer.Ordinal);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("관찰 카메라", typeof(Camera)).GetComponent<Camera>();
        camera.tag="MainCamera"; camera.backgroundColor=new Color(.025f,.035f,.06f);
        camera.clearFlags=CameraClearFlags.SolidColor;
        var key = new GameObject("따뜻한 조명",typeof(Light)).GetComponent<Light>();
        key.type=LightType.Directional; key.intensity=1.2f; key.color=new Color(1,.83f,.72f);
        key.transform.rotation=Quaternion.Euler(35,-35,0);
        var fill = new GameObject("보조 조명",typeof(Light)).GetComponent<Light>();
        fill.type=LightType.Directional; fill.intensity=.5f; fill.color=new Color(.65f,.78f,1);
        fill.transform.rotation=Quaternion.Euler(20,145,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.27f,.28f,.31f);
        var viewer = new GameObject("혈액 관찰 및 원심분리",typeof(BloodViewer)).GetComponent<BloodViewer>();
        viewer.cellShader=Shader.Find("Blood/SoftCell");
        if (viewer.cellShader == null) throw new Exception("Blood/SoftCell shader not found");
        viewer.uiFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR.ttf");
        if (viewer.uiFont == null) throw new Exception("Korean font is missing");
        var list=new List<BloodEntry>();
        foreach (string raw in paths)
        {
            string path=raw.Replace('\\','/'); string name=Path.GetFileNameWithoutExtension(path);
            if (!Labels.TryGetValue(name,out string title)) throw new Exception("Missing Korean label: "+name);
            string category=name.StartsWith("CELL_") ? "혈구" : name.StartsWith("SDF_") ? "분자" : name.StartsWith("PDB_") ? "단백질" : "혈관";
            if (!BloodDescriptions.Text.TryGetValue(title,out string description)) throw new Exception("Missing description: "+title);
            list.Add(new BloodEntry {title=title,category=category,description=description,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path)});
        }
        list.Sort((a,b)=>a.title=="적혈구" ? -1 : b.title=="적혈구" ? 1 : string.Compare(a.title,b.title,StringComparison.Ordinal));
        viewer.entries=list.ToArray();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Blood_Viewer.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Blood_Viewer.unity",true)};
        AssetDatabase.SaveAssets();
        Debug.Log("BLOOD_VIEWER_SCENE_READY " + list.Count);
    }

    [MenuItem("Blood/교육용 실행 파일 만들기")]
    public static void Build()
    {
        if (!File.Exists("Assets/Scenes/Blood_Viewer.unity")) Create();
        Directory.CreateDirectory("Build");
        var result=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Blood_Viewer.unity"},"Build/BloodViewer.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(result.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+result.summary.result);
        Debug.Log("BLOOD_VIEWER_BUILD_OK");
    }

    public static void BuildWeb()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        Directory.CreateDirectory("WebBuild");
        var result=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Blood_Viewer.unity"},"WebBuild",BuildTarget.WebGL,BuildOptions.None);
        if(result.summary.result!=BuildResult.Succeeded) throw new Exception("WebGL build failed: "+result.summary.result);
        Debug.Log("BLOOD_WEB_BUILD_OK");
    }
}
