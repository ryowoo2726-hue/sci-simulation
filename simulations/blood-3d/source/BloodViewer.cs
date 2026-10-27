using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BloodEntry
{
    public string title;
    public string category;
    public string description;
    public GameObject prefab;
}

public class AtomPick : MonoBehaviour { public string element; }

public class BloodViewer : MonoBehaviour
{
    public BloodEntry[] entries;
    public Shader cellShader;
    public Font uiFont;
    private int category, selection, view;
    private GameObject displayed, pivot, tube;
    private Transform mixed, plasma, buffy, red;
    private float separation, zoom = 1f;
    private bool separating;
    private bool shellVisible, hasShell;
    private string atomInfo = "";
    private Vector3 mouseDown;
    private Camera cam;
    private Vector2 scroll;
    private GUISkin skin;
    private readonly List<Material> generatedMaterials = new List<Material>();
    private readonly List<Mesh> generatedMeshes = new List<Mesh>();
    private readonly string[] categories = { "혈구", "혈관", "분자", "단백질" };

    void Start()
    {
        cam = Camera.main;
        cam.rect = new Rect(.29f,0,.71f,1);
        Show(0);
    }

    public void Show(int index)
    {
        if (index < 0 || index >= entries.Length || entries[index].prefab == null) return;
        selection = index; view = 0; zoom = 1;
        atomInfo = "";
        if (tube) Destroy(tube);
        if (pivot) Destroy(pivot);
        ClearMaterials();
        pivot = new GameObject("관찰 모델");
        displayed = Instantiate(entries[index].prefab, pivot.transform);
        var renderers = displayed.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        hasShell = false;
        foreach (var renderer in renderers)
            if (renderer.name.Contains("Cell membrane") || renderer.name.Contains("Resting platelet")) hasShell = true;
        shellVisible = !entries[index].title.Contains("구") || entries[index].title == "적혈구";
        if (entries[index].category == "혈구" || entries[index].category == "혈관")
        {
            var shader = cellShader;
            if (shader != null) foreach (var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;
                for (int i=0;i<materials.Length;i++) if (materials[i] != null)
                {
                    Color c=materials[i].color;
                    var material=new Material(shader); material.color=c;
                    material.SetFloat("_Scatter", entries[index].title=="적혈구" ? .32f : .08f);
                    generatedMaterials.Add(material); materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
        }
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest <= 0) return;
        displayed.transform.localScale *= 5f / largest;
        displayed.transform.localPosition -= bounds.center * (5f / largest);
        if (entries[index].category == "분자" || entries[index].category == "단백질") CreateAtomColliders();
        pivot.transform.rotation = Quaternion.Euler(50, 15, 0);
        SetShell(shellVisible);
        cam.transform.position = new Vector3(0, 0, -14);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true; cam.orthographicSize = 4.6f;
        cam.nearClipPlane = .01f; cam.farClipPlane = 200f;
    }

    void Update()
    {
        if (view == 0 && pivot != null && Input.mousePosition.x > Screen.width * .29f)
        {
            if (Input.GetMouseButtonDown(0)) mouseDown = Input.mousePosition;
            if (Input.GetMouseButton(0)) {
                pivot.transform.Rotate(Vector3.up, -Input.GetAxis("Mouse X") * 3, Space.World);
                pivot.transform.Rotate(Vector3.right, Input.GetAxis("Mouse Y") * 3, Space.World);
            }
            if (Input.GetMouseButtonUp(0) && (Input.mousePosition - mouseDown).sqrMagnitude < 36)
                PickAtom();
            float wheel = Input.mouseScrollDelta.y;
            zoom = Mathf.Clamp(zoom - wheel * .1f, .5f, 2.5f);
            cam.orthographicSize = 4.6f * zoom;
        }
        if (view == 1 && separating)
        {
            separation = Mathf.MoveTowards(separation, 1, Time.deltaTime / 6f);
            UpdateTube();
            if (separation >= 1) separating = false;
        }
    }

    void TubeView()
    {
        view = 1; separation = 0; separating = false; scroll = Vector2.zero;
        if (pivot) Destroy(pivot);
        if (tube) Destroy(tube);
        ClearMaterials();
        tube = new GameObject("원심분리 시험관");
        mixed = Layer("혼합 혈액", new Color(.32f,.015f,.025f), 0, 6);
        red = Layer("적혈구층", new Color(.25f,.008f,.012f), -1.65f, 2.7f);
        buffy = Layer("백혈구 및 혈소판층", new Color(.93f,.88f,.72f), -.285f, .03f);
        plasma = Layer("혈장층", new Color(.90f,.72f,.31f), 1.365f, 3.27f);
        var left = Layer("시험관 왼쪽 테두리", new Color(.52f,.66f,.75f), 0, 6.2f);
        left.localPosition = new Vector3(-1.34f,0,0); left.localScale = new Vector3(.07f,3.1f,.07f);
        var right = Layer("시험관 오른쪽 테두리", new Color(.52f,.66f,.75f), 0, 6.2f);
        right.localPosition = new Vector3(1.34f,0,0); right.localScale = new Vector3(.07f,3.1f,.07f);
        var bottom = Layer("시험관 바닥", new Color(.52f,.66f,.75f), -3.1f, 2.75f);
        bottom.localScale = new Vector3(.07f,1.375f,.07f); bottom.localRotation=Quaternion.Euler(0,0,90);
        cam.transform.position = new Vector3(0, .4f, -16); cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true; cam.orthographicSize = 4.5f;
        UpdateTube();
    }

    Transform Layer(string name, Color color, float y, float height)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name; obj.transform.SetParent(tube.transform);
        obj.transform.localPosition = new Vector3(0,y,0);
        obj.transform.localScale = new Vector3(2.5f,height/2,2.5f);
        var material = new Material(Shader.Find("Standard"));
        material.color = color; material.SetFloat("_Glossiness", .35f);
        generatedMaterials.Add(material);
        obj.GetComponent<Renderer>().material = material;
        return obj.transform;
    }

    void ClearMaterials()
    {
        foreach (var material in generatedMaterials) if (material) Destroy(material);
        generatedMaterials.Clear();
        foreach (var mesh in generatedMeshes) if (mesh) Destroy(mesh);
        generatedMeshes.Clear();
    }

    void CreateAtomColliders()
    {
        // ponytail: one non-convex collider per element group; huge PDB files may load slowly.
        foreach (var filter in displayed.GetComponentsInChildren<MeshFilter>())
        {
            if (!filter.name.StartsWith("Atoms_") || !filter.sharedMesh || !filter.sharedMesh.isReadable) continue;
            var source=filter.sharedMesh;
            var vertices=source.vertices;
            var toPivot=pivot.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i=0;i<vertices.Length;i++) vertices[i]=toPivot.MultiplyPoint3x4(vertices[i]);
            var mesh=new Mesh();
            if (vertices.Length>65535) mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices=vertices; mesh.triangles=source.triangles; mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            var target=new GameObject("원자 선택 " + filter.name.Substring(6));
            target.transform.SetParent(pivot.transform, false);
            target.AddComponent<AtomPick>().element=filter.name.Substring(6).ToUpperInvariant();
            target.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        Physics.SyncTransforms();
    }

    void PickAtom()
    {
        if (entries[selection].category != "분자" && entries[selection].category != "단백질") return;
        Physics.SyncTransforms();
        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 200f))
        {
            var atom=hit.collider.GetComponent<AtomPick>();
            if (atom != null) atomInfo=ElementInfo(atom.element);
        }
    }

    static string ElementInfo(string symbol)
    {
        switch (symbol)
        {
            case "H": return "수소 (H): 물과 유기 분자를 이루는 원소";
            case "C": return "탄소 (C): 유기 분자의 기본 골격을 이루는 원소";
            case "N": return "질소 (N): 아미노산과 단백질 등에 들어 있는 원소";
            case "O": return "산소 (O): 물과 여러 생체 분자에 들어 있는 원소";
            case "P": return "인 (P): 인산과 ATP 등에 들어 있는 원소";
            case "S": return "황 (S): 일부 아미노산과 단백질에 들어 있는 원소";
            case "FE": return "철 (Fe): 헤모글로빈의 헴 등에 포함되는 원소";
            case "ZN": return "아연 (Zn): 일부 효소의 기능에 필요한 금속 원소";
            case "CA": return "칼슘 (Ca): 혈액 응고와 세포 신호에 관여하는 원소";
            case "NA": return "나트륨 (Na): 혈장에 녹아 있는 주요 양이온";
            case "K": return "칼륨 (K): 세포 기능에 중요한 원소";
            case "CL": return "염소 (Cl): 혈장에서 주로 염화 이온 형태로 존재";
            case "MG": return "마그네슘 (Mg): 여러 효소 반응에 관여하는 원소";
            default: return "원소 기호: " + symbol + " (원본 구조 파일 기준)";
        }
    }

    void SetShell(bool visible)
    {
        shellVisible = visible;
        if (!displayed) return;
        foreach (var renderer in displayed.GetComponentsInChildren<Renderer>())
            if (renderer.name.Contains("Cell membrane") || renderer.name.Contains("Resting platelet")) renderer.enabled = visible;
    }

    void UpdateTube()
    {
        // ponytail: interpolated layers illustrate separation; replace with a validated sedimentation solver for quantitative work.
        mixed.gameObject.SetActive(separation < .999f);
        mixed.localScale = new Vector3(2.5f, Mathf.Max(.001f,3f*(1-separation)),2.5f);
        mixed.localPosition = new Vector3(0, 3f*separation, 0);
        foreach (var layer in new[] { red, buffy, plasma }) layer.gameObject.SetActive(separation > .001f);
        red.localScale = new Vector3(2.5f,1.35f*separation,2.5f);
        red.localPosition = new Vector3(0,-3+1.35f*separation,0);
        buffy.localScale = new Vector3(2.5f,.015f*separation,2.5f);
        buffy.localPosition = new Vector3(0,-3+2.715f*separation,0);
        plasma.localScale = new Vector3(2.5f,1.635f*separation,2.5f);
        plasma.localPosition = new Vector3(0,-3+4.365f*separation,0);
    }

    void OnGUI()
    {
        if (skin == null)
        {
            skin = Instantiate(GUI.skin);
            var korean = uiFont != null ? uiFont : Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 16);
            if (korean != null) skin.font = korean;
            skin.label.fontSize = 16; skin.button.fontSize = 15;
            skin.label.wordWrap = true; skin.button.wordWrap = true;
            skin.box.fontSize = 18; skin.box.normal.textColor = Color.white;
            skin.label.normal.textColor = new Color(.9f,.92f,.94f);
        }
        GUI.skin = skin;
        float w = Mathf.Clamp(Screen.width * .28f, 285, 410);
        GUI.Box(new Rect(0,0,w,Screen.height), "혈액 3D 관찰");
        GUILayout.BeginArea(new Rect(14,42,w-28,Screen.height-56));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("구성 요소", GUILayout.Height(38))) { if (view != 0) Show(selection); }
        if (GUILayout.Button("원심분리", GUILayout.Height(38))) { TubeView(); }
        GUILayout.EndHorizontal();
        if (view == 0)
        {
            GUILayout.Label("분류");
            GUILayout.BeginHorizontal();
            for (int i=0;i<categories.Length;i++) if (GUILayout.Button(categories[i],GUILayout.Height(42))) { category=i; scroll=Vector2.zero; }
            GUILayout.EndHorizontal();
            if (hasShell && GUILayout.Button(shellVisible ? "외막 숨기고 내부 보기" : "외막 보이기", GUILayout.Height(32))) SetShell(!shellVisible);
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i=0;i<entries.Length;i++)
                if (entries[i].category == categories[category] && GUILayout.Button((i==selection ? "● " : "") + entries[i].title, GUILayout.Height(34))) Show(i);
            GUILayout.EndScrollView();
            GUILayout.Label("마우스 드래그: 회전   |   휠: 확대·축소");
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("항응고 처리한 혈액의 원심분리", GUILayout.Height(32));
            if (GUILayout.Button(separating ? "분리 중..." : "원심분리 시작", GUILayout.Height(42)) && !separating) { if (separation >= 1) TubeView(); separating=true; }
            if (GUILayout.Button("다시 섞기", GUILayout.Height(36))) TubeView();
            GUILayout.Space(8);
            GUILayout.Label("혈장: 약 54.5%\n백혈구·혈소판층: 약 0.5%\n적혈구층: 약 45%", GUILayout.Height(78));
            GUILayout.Label("예시 비율이며 개인과 시료에 따라 달라집니다. 분리 과정은 교육용 애니메이션입니다.", GUILayout.Height(90));
            if (GUILayout.Button("적혈구 자세히 보기")) ShowByName("적혈구");
            if (GUILayout.Button("백혈구 자세히 보기")) ShowByName("호중구");
            if (GUILayout.Button("혈소판 자세히 보기")) ShowByName("비활성 혈소판");
            if (GUILayout.Button("혈장 성분 자세히 보기")) { category=2; ShowByName("물"); }
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();
        float rightX = w + 22;
        if (view == 0 && entries != null && entries.Length > selection)
        {
            float infoWidth=Mathf.Min(620,Screen.width-rightX-20);
            GUI.Box(new Rect(rightX,18,infoWidth,115), entries[selection].title);
            GUI.Label(new Rect(rightX+12,51,infoWidth-24,78), entries[selection].description);
            if (entries[selection].category == "분자" || entries[selection].category == "단백질")
            {
                GUI.Box(new Rect(rightX,Screen.height-92,infoWidth,74), "원자 알아보기");
                GUI.Label(new Rect(rightX+12,Screen.height-62,infoWidth-24,42), string.IsNullOrEmpty(atomInfo) ? "원자 구를 클릭하세요. 같은 원소는 같은 색으로 표시됩니다." : atomInfo);
            }
        }
        if (view == 1 && separation > .98f)
            GUI.Label(new Rect(Screen.width-265,Screen.height*.38f,250,150), "위: 혈장\n\n가운데: 백혈구·혈소판\n\n아래: 적혈구");
    }

    void ShowByName(string title)
    {
        for (int i=0;i<entries.Length;i++) if (entries[i].title == title) { category=Array.IndexOf(categories, entries[i].category); Show(i); return; }
    }
}
