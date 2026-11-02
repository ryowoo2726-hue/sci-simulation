using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AlveolarSimulation : MonoBehaviour
{
    readonly Color oxygen = new Color(.28f, .87f, .96f), carbon = new Color(1, .73f, .26f);
    readonly Color venous = new Color(.24f, .022f, .048f), arterial = new Color(.75f, .055f, .065f);
    readonly List<Transform> cells = new List<Transform>();
    readonly List<Material> cellMaterials = new List<Material>();
    readonly List<Gas> gases = new List<Gas>();
    readonly List<Transform> sacs = new List<Transform>();
    readonly List<Transform> arrows = new List<Transform>();
    class Gas { public Transform transform; public Vector3 origin, velocity; public float phase; public bool co2, transfer, detail; }
    Transform anatomy, detail, cutRim; Camera view; Material wall, vessel, oMat, cMat; Mesh sphere, redCell;
    Vector3 previousMouse;
    Vector3[] oxygenAtoms, carbonAtoms;
    Font font; GUIStyle title, body, small, button, heading; Texture2D panel;
    float clock, speed = 1, yaw = -9, pitch = 12, distance = 7.6f; bool paused, section = true, showGas = true, equalized, closeup;
    public static float BloodOxygen(float t, bool equilibrium) => equilibrium ? 100 : Mathf.Lerp(40, 100, 1 - Mathf.Exp(-5 * Mathf.Clamp01(t)));
    public static float BloodCarbon(float t, bool equilibrium) => equilibrium ? 40 : Mathf.Lerp(46, 40, 1 - Mathf.Exp(-5 * Mathf.Clamp01(t)));
    public static float NetGradient(bool co2, bool equilibrium) => equilibrium ? 0 : co2 ? 6 : 60;

    void Start()
    {
        Application.targetFrameRate = 60;
        font = Resources.Load<Font>("Fonts/NotoSansKR");
        panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.035f, .065f, .085f, .95f)); panel.Apply();
        anatomy = new GameObject("허파꽈리와 모세혈관").transform;
        view = Camera.main;
        if (view == null) { view = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); view.tag = "MainCamera"; }
        view.backgroundColor = new Color(.017f, .032f, .046f); view.clearFlags = CameraClearFlags.SolidColor; view.fieldOfView = 39; view.nearClipPlane = .03f;
        RenderSettings.ambientLight = new Color(.32f, .26f, .27f);
        var light = new GameObject("Warm tissue light", typeof(Light)).GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f; light.color = new Color(1, .86f, .78f); light.transform.rotation = Quaternion.Euler(38, -32, 0);
        wall = Tissue("폐포벽", new Color(.71f, .36f, .32f), true);
        vessel = new Material(Shader.Find("Heart/AnatomyGhost")); vessel.SetColor("_BaseColor", new Color(.68f, .20f, .24f, .22f));
        oMat = Tissue("산소 · 청록색 학습 표시", oxygen); cMat = Tissue("이산화 탄소 · 금색 학습 표시", carbon);
        sphere = AlveolarGeometry.Sac(8, 12);
        oxygenAtoms = AlveolarGeometry.ReadAtoms("Molecules/Oxygen");
        carbonAtoms = AlveolarGeometry.ReadAtoms("Molecules/CarbonDioxide");
        redCell = AlveolarGeometry.RedCell(); BuildAnatomy(); BuildDetail(); ResetSimulation();
        Shader.SetGlobalFloat("_SectionEnabled", 1); Shader.SetGlobalFloat("_SectionDepth", -.16f);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-verify") >= 0) StartCoroutine(VerifyAndCapture());
    }

    Material Tissue(string name, Color color, bool cut = false)
    {
        var m = new Material(Shader.Find("Heart/LivingTissue")) { name = name };
        m.SetColor("_BaseColor", color); m.SetFloat("_Cuttable", cut ? 1 : 0); m.SetFloat("_Deform", 0); m.SetFloat("_Fiber", 1); m.SetFloat("_Roughness", .25f); m.SetFloat("_Subsurface", .65f); return m;
    }
    Transform MeshObject(string name, Mesh mesh, Material material, Transform parent, Vector3 position, Vector3 scale)
    {
        var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); g.transform.SetParent(parent, false); g.transform.localPosition = position; g.transform.localScale = scale;
        g.GetComponent<MeshFilter>().sharedMesh = mesh; g.GetComponent<MeshRenderer>().sharedMaterial = material; return g.transform;
    }
    static Vector3 Surface(float a, float b, float r = 1.47f) => new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b)) * r;
    Vector3 BloodPath(float t)
    {
        // A continuous front-facing vessel, with visible single-file red cells.
        float x = Mathf.Lerp(-1.85f, 1.85f, t);
        return new Vector3(x, -.57f + .20f * Mathf.Sin(t * Mathf.PI * 2), -Mathf.Sqrt(Mathf.Max(.03f, 2.1f - x * x)) - .08f);
    }
    void BuildAnatomy()
    {
        var sacMesh = AlveolarGeometry.Sac();
        sacs.Add(MeshObject("중앙 허파꽈리 · 절개", sacMesh, wall, anatomy, Vector3.zero, Vector3.one * 1.4f));
        cutRim = MeshObject("절개면의 얇은 조직 가장자리", AlveolarGeometry.Tube(t => new Vector3(Mathf.Cos(t * 2 * Mathf.PI) * 1.382f, Mathf.Sin(t * 2 * Mathf.PI) * 1.382f, -.224f), .018f, 160, 10), Tissue("절개 가장자리", new Color(.86f, .56f, .47f)), anatomy, Vector3.zero, Vector3.one);
        var backWall = Tissue("주변 허파꽈리", new Color(.52f, .23f, .23f));
        var backSacMesh = AlveolarGeometry.Sac(24, 36);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + .5f) * Mathf.PI * 2 / 8;
            Vector3 p = new Vector3(Mathf.Cos(a) * 1.8f, Mathf.Sin(a) * 1.55f, 1.05f + .3f * Mathf.Sin(i * 3));
            var sac = MeshObject("주변 허파꽈리 " + i, backSacMesh, backWall, anatomy, p, Vector3.one * (.65f + .12f * Mathf.Sin(i * 7)));
            sacs.Add(sac);
        }
        var airway = Tissue("폐포관", new Color(.58f, .31f, .27f));
        MeshObject("폐포관", AlveolarGeometry.Tube(t => new Vector3(.12f * Mathf.Sin(t * 4), 1.1f + t * 1.7f, 1.05f), .28f, 48, 28), airway, anatomy, Vector3.zero, Vector3.one);
        // The basket represents a continuous capillary bed on the wall; detail view shows its true thin barrier.
        var network = Tissue("모세혈관 그물", new Color(.43f, .055f, .085f));
        for (int i = 0; i < 9; i++)
        {
            float angle = i * Mathf.PI * 2 / 9;
            MeshObject("세로 모세혈관 " + i, AlveolarGeometry.Tube(t => Surface(angle + .12f * Mathf.Sin(t * 13 + angle), Mathf.Lerp(-1.33f, 1.33f, t)), .026f, 90, 9), network, anatomy, Vector3.zero, Vector3.one);
        }
        for (int i = 0; i < 6; i++)
        {
            float latitude = Mathf.Lerp(-1.1f, 1.1f, i / 5f);
            MeshObject("가로 모세혈관 " + i, AlveolarGeometry.Tube(t => Surface(t * Mathf.PI * 2, latitude + .035f * Mathf.Sin(t * 42)), .025f, 160, 9), network, anatomy, Vector3.zero, Vector3.one);
        }
        MeshObject("관찰용 혈관 · 투명 외벽", AlveolarGeometry.Tube(BloodPath, .105f, 160, 18), vessel, anatomy, Vector3.zero, Vector3.one);
        for (int i = 0; i < 26; i++)
        {
            var mat = Tissue("적혈구 " + i, venous); cellMaterials.Add(mat);
            cells.Add(MeshObject("양면 오목 적혈구 " + i, redCell, mat, anatomy, Vector3.zero, Vector3.one * .073f));
        }
        // Enlarged nuclei / type II cells emphasize the living wall rather than a plastic shell.
        var nuclei = Tissue("폐포벽 세포핵", new Color(.32f, .12f, .19f));
        var secretory = Tissue("제2형 폐포세포", new Color(.79f, .53f, .40f));
        for (int i = 0; i < 82; i++)
        {
            float a = i * 2.39996f, y = Mathf.Lerp(-.94f, .94f, i / 81f), s = Mathf.Sqrt(1 - y * y);
            Vector3 n = new Vector3(Mathf.Cos(a) * s, y, Mathf.Abs(Mathf.Sin(a) * s));
            var cell = MeshObject(i % 9 == 0 ? "제2형 세포" : "편평한 세포핵", sphere, i % 9 == 0 ? secretory : nuclei, anatomy, n * 1.37f, i % 9 == 0 ? new Vector3(.065f, .055f, .04f) : new Vector3(.052f, .025f, .012f));
            cell.localRotation = Quaternion.LookRotation(n);
        }
        for (int i = 0; i < 64; i++)
        {
            bool co2 = i % 3 == 0, transfer = i < 28;
            var g = new Gas { co2 = co2, transfer = transfer, phase = i * .618034f % 1, origin = UnityEngine.Random.insideUnitSphere * 1.08f, velocity = UnityEngine.Random.onUnitSphere * .18f };
            g.transform = Molecule(co2, anatomy); gases.Add(g);
        }
        arrows.Add(Arrow("산소의 순이동", oxygen, anatomy, new Vector3(-.53f, -.04f, -1.65f), new Vector3(-.53f, -.52f, -1.65f)));
        arrows.Add(Arrow("이산화 탄소의 순이동", carbon, anatomy, new Vector3(.52f, -.52f, -1.65f), new Vector3(.52f, -.04f, -1.65f)));
    }
    Transform Molecule(bool co2, Transform parent)
    {
        var root = new GameObject(co2 ? "CO2 · SDF structure" : "O2 · SDF structure").transform; root.SetParent(parent, false);
        Vector3[] atoms = co2 ? carbonAtoms : oxygenAtoms;
        foreach (Vector3 atom in atoms) MeshObject("원자", sphere, co2 ? cMat : oMat, root, atom * .025f, Vector3.one * .035f);
        return root;
    }
    Transform Arrow(string name, Color color, Transform parent, Vector3 start, Vector3 end)
    {
        var root = new GameObject(name).transform; root.SetParent(parent, false); var mat = Tissue(name, color);
        MeshObject("화살표 축", AlveolarGeometry.Tube(t => Vector3.Lerp(start, end, t), .013f, 2, 8), mat, root, Vector3.zero, Vector3.one);
        Vector3 direction = (end - start).normalized, side = Vector3.Cross(direction, Vector3.forward) * .075f;
        MeshObject("화살촉", AlveolarGeometry.Tube(t => t < .5f ? Vector3.Lerp(end - direction * .12f + side, end, t * 2) : Vector3.Lerp(end, end - direction * .12f - side, t * 2 - 1), .013f, 4, 8), mat, root, Vector3.zero, Vector3.one);
        return root;
    }
    void BuildDetail()
    {
        detail = new GameObject("폐포-모세혈관 장벽 확대").transform;
        var epithelial = Tissue("제1형 폐포상피", new Color(.83f, .53f, .43f));
        var basement = Tissue("융합 기저막", new Color(.93f, .78f, .59f));
        var endothelial = Tissue("모세혈관 내피", new Color(.65f, .22f, .24f));
        var surfactant = Tissue("액체층 · 계면활성 물질", new Color(.44f, .72f, .72f));
        var mats = new[] { endothelial, basement, epithelial, surfactant };
        for (int i = 0; i < 4; i++) MeshObject(mats[i].name, AlveolarGeometry.Tube(t => new Vector3(Mathf.Lerp(-2.2f, 2.2f, t), -.26f + i * .15f + .035f * Mathf.Sin(t * 12), .6f), .095f, 90, 22), mats[i], detail, Vector3.zero, new Vector3(1, 1, 6));
        for (int i = 0; i < 7; i++) MeshObject("확대 적혈구", redCell, Tissue("혈액", Color.Lerp(venous, arterial, i / 6f)), detail, new Vector3(-1.9f + i * .64f, -.85f, 0), new Vector3(.25f, .21f, .27f));
        arrows.Add(Arrow("O2 · 장벽을 통과", oxygen, detail, new Vector3(-.8f, 1.12f, -.8f), new Vector3(-.8f, -.63f, -.8f)));
        arrows.Add(Arrow("CO2 · 반대 방향", carbon, detail, new Vector3(.8f, -.63f, -.8f), new Vector3(.8f, 1.12f, -.8f)));
        for (int i = 0; i < 24; i++)
        {
            bool co2 = i % 2 == 0;
            var g = new Gas { detail = true, co2 = co2, transfer = true, phase = i * .618034f % 1, velocity = UnityEngine.Random.onUnitSphere * .18f };
            g.origin = new Vector3((g.phase - .5f) * 3.8f, co2 ? -.8f : .9f, -.7f);
            g.transform = Molecule(co2, detail); gases.Add(g);
        }
        detail.gameObject.SetActive(false);
    }
    void ResetSimulation() { clock = 0; paused = false; equalized = false; speed = 1; }
    void Update()
    {
        float dt = paused ? 0 : Mathf.Min(Time.deltaTime, .05f) * speed; clock += dt;
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.R)) { yaw = -9; pitch = 12; distance = 7.6f; }
        // Reserve the right-hand teaching panel and bottom controls for UI.
        if (Input.mousePosition.x < Screen.width * .72f && Input.mousePosition.y > Screen.height * .18f)
        {
            if (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0)) { Vector3 drag = Input.mousePosition - previousMouse; yaw += drag.x * .25f; pitch = Mathf.Clamp(pitch - drag.y * .20f, -65, 65); }
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * .4f, 4.8f, 12);
        }
        previousMouse = Input.mousePosition;
        view.transform.position = Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -distance) + new Vector3(.45f, .18f, 0);
        view.transform.LookAt(new Vector3(.45f, .18f, 0));
        view.rect = new Rect(0, .13f, .72f, .78f);
        Shader.SetGlobalFloat("_SectionEnabled", section ? 1 : 0);
        cutRim.gameObject.SetActive(section);
        // ponytail: pressure/flow teaching animation, not a numerical lung physiology solver.
        float breath = 1 + .018f * Mathf.Sin(clock * Mathf.PI / 2.5f); anatomy.localScale = Vector3.one * breath;
        for (int i = 0; i < cells.Count; i++)
        {
            float t = Mathf.Repeat(i / (float)cells.Count + clock * .052f, 1);
            cells[i].localPosition = BloodPath(t); Vector3 tangent = BloodPath(Mathf.Min(1, t + .003f)) - BloodPath(Mathf.Max(0, t - .003f));
            cells[i].localRotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(73, 0, i * 137 + clock * 8);
            cellMaterials[i].SetColor("_BaseColor", Color.Lerp(venous, arterial, equalized ? 1 : 1 - Mathf.Exp(-5 * t)));
        }
        foreach (Gas g in gases)
        {
            bool visible = showGas && g.detail == closeup;
            if (g.transform.gameObject.activeSelf != visible) g.transform.gameObject.SetActive(visible);
            if (!visible || dt == 0) continue;
            if (g.transfer && !equalized)
            {
                float t = Mathf.Repeat(clock * .27f + g.phase, 1); float x = (g.phase - .5f) * 1.65f;
                Vector3 a = g.detail ? new Vector3(x * 2.3f, 1.1f, -.7f) : new Vector3(x, .3f + .15f * Mathf.Sin(g.phase * 17), -.7f);
                Vector3 b = g.detail ? new Vector3(x * 2.3f, -.8f, -.7f) : BloodPath(Mathf.InverseLerp(-1.85f, 1.85f, x));
                g.transform.localPosition = Vector3.Lerp(g.co2 ? b : a, g.co2 ? a : b, t) + new Vector3(Mathf.Sin(clock * 10 + g.phase * 50), Mathf.Cos(clock * 8 + g.phase * 40), 0) * .025f;
            }
            else
            {
                if (g.detail) g.origin = new Vector3((g.phase - .5f) * 3.8f + .08f * Mathf.Sin(clock * 3 + g.phase * 17), (g.co2 ? -.8f : .9f) + .08f * Mathf.Cos(clock * 4 + g.phase * 13), -.7f);
                else { g.origin += g.velocity * dt; if (g.origin.magnitude > 1.15f) { g.velocity = Vector3.Reflect(g.velocity, g.origin.normalized); g.origin = g.origin.normalized * 1.149f; } }
                g.transform.localPosition = g.origin;
            }
            g.transform.Rotate(new Vector3(23, 37, 17) * dt, Space.Self);
        }
        foreach (var arrow in arrows) arrow.gameObject.SetActive(!equalized && showGas);
    }
    void Styles()
    {
        if (title != null) return;
        body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 17, wordWrap = true, richText = true }; body.normal.textColor = new Color(.85f, .91f, .92f);
        title = new GUIStyle(body) { fontSize = 30, fontStyle = FontStyle.Bold };
        heading = new GUIStyle(body) { fontSize = 21, fontStyle = FontStyle.Bold };
        small = new GUIStyle(body) { fontSize = 14 }; small.normal.textColor = new Color(.60f, .71f, .74f);
        button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, padding = new RectOffset(10, 10, 7, 7) };
    }
    void Label(float x, float y, float w, float h, string text, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, h), text, style ?? body);
    void OnGUI()
    {
        Styles(); float scale = Mathf.Min(Screen.width / 1440f, Screen.height / 900f); GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float w = Screen.width / scale, h = Screen.height / scale, x = w * .735f, pw = w - x - 24;
        GUI.DrawTexture(new Rect(0, 0, w, 98), panel); GUI.DrawTexture(new Rect(x - 18, 114, pw + 34, h - 254), panel);
        Label(30, 15, 850, 43, "허파꽈리에서 일어나는 기체 교환", title);
        Label(32, 62, 950, 28, "ALVEOLAR GAS EXCHANGE     /     3D 탐구 실험실", small);
        Label(x, 135, pw, 34, closeup ? "02   얇은 장벽을 통과하다" : "01   허파꽈리와 모세혈관", heading);
        Label(x, 185, pw, 74, closeup ? "폐포 공기와 혈액은 섞이지 않습니다. 기체가 얇은 세포층을 확산으로 통과합니다." : "공기가 들어 있는 주머니를 모세혈관이 그물처럼 감쌉니다. 적혈구가 혈관을 따라 흐릅니다.");
        Label(x, 280, pw, 30, "<color=#48DEEF>● 산소 O2</color>   폐포 → 혈액", heading);
        Label(x, 319, pw, 58, equalized ? "폐포 공기 100 = 혈액 100\n분압 차 0 mmHg" : "폐포 공기 100 → 유입 혈액 40\n분압 차 60 mmHg");
        Label(x, 398, pw, 30, "<color=#FFBB43>● 이산화 탄소 CO2</color>", heading);
        Label(x, 437, pw, 62, equalized ? "혈액 40 = 폐포 공기 40 mmHg\n순이동 없음" : "혈액 → 폐포\n유입 혈액 46 → 폐포 공기 40 mmHg");
        Label(x, 523, pw, 65, equalized ? "분압이 같으면 순이동은 0입니다. 분자의 무작위 운동은 계속됩니다." : "각 기체는 자신의 분압이 높은 쪽에서 낮은 쪽으로 순이동합니다.");
        Label(x, 610, pw, 87, "산소는 적혈구의 헤모글로빈과 결합합니다.\n혈액 색: 어두운 적색 → 선홍색\n산소가 적어도 혈액은 붉은색입니다.", small);
        if (closeup)
        {
            Label(36, 140, 650, 30, "폐포 공기", heading); Label(36, 600, 650, 30, "모세혈관 속 혈액", heading);
            Label(36, 650, 870, 54, "위에서부터: 액체층 → 폐포상피 → 기저막 → 모세혈관 내피\n층 두께와 분자 크기는 관찰을 위해 확대했습니다.", small);
        }
        else
        {
            Label(32, 130, 650, 30, section ? "절개 관찰  /  안쪽 공기 공간과 얇은 폐포벽" : "외형 관찰  /  포도송이 모양의 폐포낭", small);
            Label(32, h - 205, 900, 42, equalized ? "평형 비교: 폐포와 혈액의 O2 100 / CO2 40 mmHg · 무작위 운동은 계속" : "혈액 유입  O2 40 / CO2 46    →    혈액 유출  O2 ≈100 / CO2 ≈40 mmHg", small);
        }
        float y = h - 132; GUI.DrawTexture(new Rect(0, y - 12, w, 144), panel);
        if (GUI.Button(new Rect(28, y, 110, 40), paused ? "▶ 재생" : "Ⅱ 일시정지", button)) paused = !paused;
        if (GUI.Button(new Rect(148, y, 110, 40), "처음부터", button)) ResetSimulation();
        if (GUI.Button(new Rect(268, y, 145, 40), section ? "외형 보기" : "절개 보기", button)) section = !section;
        if (GUI.Button(new Rect(423, y, 145, 40), closeup ? "전체 보기" : "장벽 확대", button)) { closeup = !closeup; detail.gameObject.SetActive(closeup); anatomy.gameObject.SetActive(!closeup); yaw = 0; pitch = closeup ? 4 : 12; }
        if (GUI.Button(new Rect(578, y, 145, 40), showGas ? "기체 표시 끄기" : "기체 표시 켜기", button)) showGas = !showGas;
        if (GUI.Button(new Rect(733, y, 165, 40), equalized ? "정상 분압 차" : "분압 같게 비교", button)) equalized = !equalized;
        Label(920, y + 5, 130, 32, "속도 " + speed.ToString("0.0") + "×"); speed = GUI.HorizontalSlider(new Rect(1035, y + 18, Mathf.Max(100, w - 1070), 20), speed, .2f, 2);
        Label(30, y + 53, w - 60, 26, "마우스 드래그: 회전   ·   휠: 확대/축소   ·   Space: 일시정지   ·   R: 시점 복원", small);
        Label(30, y + 81, w - 60, 36, "교육용 구조 모형 · 크기·시간·기체 색·입자 수는 실제 비율이 아닙니다. 분압은 안정 시 대표값이며 정량 계산 결과가 아닙니다.", small);
    }

    IEnumerator VerifyAndCapture()
    {
        yield return null;
        if (AlveolarGeometry.ReadAtoms("Molecules/Oxygen").Length != 2 || AlveolarGeometry.ReadAtoms("Molecules/CarbonDioxide").Length != 3) throw new Exception("SDF atom count");
        if (!(BloodOxygen(1, false) > BloodOxygen(0, false) && BloodCarbon(1, false) < BloodCarbon(0, false))) throw new Exception("Diffusion direction");
        if (NetGradient(false, true) != 0 || NetGradient(true, true) != 0) throw new Exception("Equilibrium");
        float start = clock; yield return null; if (clock <= start) throw new Exception("Simulation clock");
        paused = true; start = clock; yield return null; if (clock != start) throw new Exception("Pause");
        paused = false;
        string folder = Path.Combine(Application.dataPath, "..", "Verification"); Directory.CreateDirectory(folder);
        yield return new WaitForSeconds(2); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "01-alveoli.png"));
        yield return new WaitForSeconds(1); closeup = true; anatomy.gameObject.SetActive(false); detail.gameObject.SetActive(true); yaw = 0; pitch = 4;
        yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(folder, "02-barrier.png"));
        yield return new WaitForSeconds(1); equalized = true; yield return null;
        foreach (Transform a in arrows) if (a.gameObject.activeSelf) throw new Exception("Equilibrium arrows");
        File.WriteAllText(Path.Combine(folder, "checks.txt"), "PASS: SDF atom counts; oxygen and carbon dioxide directions; zero net gradient at equilibrium; advancing clock; pause; hidden net-flow arrows at equilibrium.\n");
        Debug.Log("ALVEOLI_VERIFICATION_PASS"); yield return new WaitForSeconds(1); Application.Quit();
    }
}
