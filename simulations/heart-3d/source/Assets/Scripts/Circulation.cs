using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Circulation:MonoBehaviour
{
    public CardiacCycleController cycle;
    public CrossSectionPlaneController section;
    public SplineBloodFlowController flow;
    public HeartLearningController learning;
    public Camera viewCamera;
    public readonly List<HeartValve> valves=new List<HeartValve>();
    public readonly Dictionary<string,Mesh> atlas=new Dictionary<string,Mesh>();
    public readonly List<Renderer> vesselRenderers=new List<Renderer>();
    public readonly List<Renderer> anatomyRenderers=new List<Renderer>();
    public readonly List<Renderer> chambers=new List<Renderer>();
    public readonly List<Material> chamberMaterials=new List<Material>();
    public Mesh body;
    public int view,selectedChamber=-1,valveSelection=-1;
    public bool labels=true,transparentVessels=true;
    public float yaw=-14,pitch=9,distance=7.6f;
    Vector3 mouse;Material ghost;Transform cap;
    public Material TissueMaterial(string name,Color color,bool cut=false,float fiber=0,float deform=0)
    {
        var m=new Material(Shader.Find("Heart/LivingTissue")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Cuttable",cut?1:0);m.SetFloat("_Fiber",fiber);m.SetFloat("_Deform",deform);return m;
    }
    public Transform Part(string name,Mesh mesh,Material material,Transform parent=null)
    {
        var g=new GameObject(name);g.transform.SetParent(parent==null?transform:parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;return g.transform;
    }
    void Awake()
    {
        Application.targetFrameRate=60;QualitySettings.vSyncCount=1;
        var preview=GameObject.Find("Editor anatomy preview");if(preview!=null)preview.SetActive(false);
        cycle=gameObject.AddComponent<CardiacCycleController>();cycle.speed=.45f;cycle.SetPhase(.70f);
        foreach(var pair in AtlasHeart.Load())atlas.Add(pair.Key,pair.Value);
        ghost=new Material(Shader.Find("Heart/AnatomyGhost"));ghost.SetColor("_BaseColor",new Color(.56f,.76f,.82f,.065f));
        var wallMeshes=new List<Mesh>();
        for(int i=0;i<4;i++){
            string name=AtlasHeart.ChamberNames[i];Mesh mesh=atlas[name];wallMeshes.Add(mesh);
            Material material=TissueMaterial(name,new Color(.29f+i*.012f,.075f,.080f),true,.65f,1);
            Transform part=Part(name,mesh,material);Renderer renderer=part.GetComponent<Renderer>();chambers.Add(renderer);chamberMaterials.Add(material);anatomyRenderers.Add(renderer);
            part.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        body=AtlasHeart.Combine(wallMeshes,"Atlas myocardium · four anatomical chamber walls");
        Material cut=TissueMaterial("단면 · 심근 조직",new Color(.58f,.25f,.22f),false,.7f,1);
        cap=Part("실시간 절개면",null,cut);section=gameObject.AddComponent<CrossSectionPlaneController>();section.body=body;section.tissue=chambers[0];section.cap=cap.GetComponent<MeshFilter>();
        Material vessels=TissueMaterial("주요 혈관",new Color(.32f,.12f,.13f),false,.1f);
        Material coronary=TissueMaterial("관상혈관",new Color(.25f,.055f,.065f),true,.1f,1);
        Material muscle=TissueMaterial("유두근",new Color(.33f,.085f,.09f),true,.5f);
        foreach(var pair in atlas){
            string n=pair.Key;if(System.Array.IndexOf(AtlasHeart.ChamberNames,n)>=0||n.Contains("leaflet"))continue;
            bool papillary=n.Contains("papillary"),cor=n.Contains("coronary")||n.Contains("Coronary");
            Transform p=Part(n,pair.Value,papillary?muscle:cor?coronary:vessels);Renderer r=p.GetComponent<Renderer>();anatomyRenderers.Add(r);if(!papillary&&!cor)vesselRenderers.Add(r);
        }
        Material leaf=TissueMaterial("판막 · 얇은 결합 조직",new Color(.74f,.69f,.59f),false,.08f);leaf.SetFloat("_Subsurface",.8f);
        Material cord=TissueMaterial("건삭",new Color(.80f,.76f,.66f));
        CreateValve("삼첨판",0,true,3,.35f,leaf,cord,null);
        CreateValve("이첨판 (승모판)",1,true,2,.36f,leaf,cord,null);
        var pulmonary=new List<Mesh>();var aortic=new List<Mesh>();
        foreach(var pair in atlas)if(pair.Key.Contains("leaflet")){
            if(pair.Key.Contains("pulmonary valve"))pulmonary.Add(pair.Value);
            if(pair.Key=="Left coronary leaflet"||pair.Key=="Right coronary leaflet"||pair.Key=="Non-coronary leaflet")aortic.Add(pair.Value);
        }
        CreateValve("폐동맥판",2,false,3,.39f,leaf,cord,AtlasHeart.Combine(pulmonary,"Atlas pulmonary cusps"));
        CreateValve("대동맥판",3,false,3,.31f,leaf,cord,AtlasHeart.Combine(aortic,"Atlas aortic cusps"));
        for(int i=0;i<2;i++){
            var tips=new List<Vector3>();Vector3 root=AtlasHeart.ValvePosition[i];
            foreach(var pair in atlas)if(pair.Key.Contains("papillary")&&pair.Key.Contains(i==0?"right ventricle":"left ventricle")){
                Vector3 closest=pair.Value.vertices[0];float distance=(closest-root).sqrMagnitude;
                foreach(Vector3 p in pair.Value.vertices)if((p-root).sqrMagnitude<distance){closest=p;distance=(p-root).sqrMagnitude;}tips.Add(closest);
            }
            valves[i].AttachToPapillaryTips(tips);
        }
        viewCamera=new GameObject("Main Camera").AddComponent<Camera>();viewCamera.tag="MainCamera";viewCamera.clearFlags=CameraClearFlags.SolidColor;viewCamera.backgroundColor=new Color(.035f,.055f,.075f);viewCamera.fieldOfView=37;viewCamera.nearClipPlane=.03f;viewCamera.farClipPlane=40;viewCamera.allowHDR=true;
        var background=new GameObject("Full-screen background").AddComponent<Camera>();background.depth=-10;background.cullingMask=0;background.clearFlags=CameraClearFlags.SolidColor;background.backgroundColor=viewCamera.backgroundColor;
        var light=new GameObject("Soft anatomical key").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(32,-35,0);light.color=new Color(1,.90f,.85f);light.intensity=1.2f;
        RenderSettings.ambientLight=new Color(.18f,.22f,.27f);
        flow=gameObject.AddComponent<SplineBloodFlowController>();flow.Initialize(this,TissueMaterial("산소 적음",SplineBloodFlowController.Venous),TissueMaterial("산소 많음",SplineBloodFlowController.Arterial));
        learning=gameObject.AddComponent<HeartLearningController>();learning.simulation=this;gameObject.AddComponent<SimulationHUDController>().simulation=this;
        SetView(0);learning.SetMode(HeartLearningController.LearningMode.Guided);
        string[] args=System.Environment.GetCommandLineArgs();int capture=System.Array.IndexOf(args,"-capture");if(capture>=0&&capture+1<args.Length)StartCoroutine(Capture(args[capture+1]));
    }
    void CreateValve(string name,int index,bool av,int count,float radius,Material leaf,Material cord,Mesh source)
    {
        var g=new GameObject(name);g.transform.SetParent(transform,false);g.transform.localPosition=AtlasHeart.ValvePosition[index];
        g.transform.localRotation=Quaternion.FromToRotation(av?Vector3.down:Vector3.up,AtlasHeart.ValveFlow[index].normalized);
        var valve=g.AddComponent<HeartValve>();if(source!=null)valve.InitializeAtlas(source,radius,leaf,cycle);else valve.Initialize(av,count,radius,leaf,cord,cycle);valves.Add(valve);
    }
    public void FocusChamber(int index)
    {
        selectedChamber=index;
        for(int i=0;i<chambers.Count;i++)chambers[i].sharedMaterial=index>=0&&i==index?chamberMaterials[i]:ghost;
        section.body=body;section.capVisible=index>=0;
        if(index>=0){section.depth=new[]{-.10f,-.63f,.73f,.04f}[index];section.Invalidate();}
    }
    public void ClearFocus()
    {
        selectedChamber=-1;for(int i=0;i<chambers.Count;i++)chambers[i].sharedMaterial=chamberMaterials[i];section.body=body;section.capVisible=true;section.depth=-.35f;section.Invalidate();
    }
    public void SetView(int value)
    {
        view=value;valveSelection=-1;section.section=value==0;
        if(flow!=null&&learning!=null)flow.visible=value!=2&&learning.mode==HeartLearningController.LearningMode.Explore;
        foreach(var r in anatomyRenderers)r.enabled=value!=2||r.name.Contains("papillary");
        foreach(var valve in valves)valve.gameObject.SetActive(true);
        if(value==1)ClearFocus();
        foreach(var r in vesselRenderers)r.sharedMaterial=value==0&&transparentVessels?ghost:TissueMaterial("혈관벽",new Color(.32f,.12f,.13f),false,.1f);
        yaw=value==2?-20:-14;pitch=value==2?48:9;distance=value==2?5.0f:7.6f;
        if(value==0){if(learning!=null&&learning.mode!=HeartLearningController.LearningMode.Explore)FocusChamber(HeartLearningController.FocusChamber[learning.step]);else ClearFocus();}
    }
    public void SelectValve(int index)
    {
        valveSelection=index;
        for(int i=0;i<valves.Count;i++)valves[i].gameObject.SetActive(index<0||i==index);
        foreach(var r in anatomyRenderers)r.enabled=r.name.Contains("papillary")&&(index<0||index<2&&r.name.Contains(index==0?"right ventricle":"left ventricle"));
        if(index>=0)distance=2.5f;else distance=5;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)){
            if(learning.mode==HeartLearningController.LearningMode.Explore)cycle.playing=!cycle.playing;else if(learning.mode==HeartLearningController.LearningMode.Guided)learning.packetPaused=!learning.packetPaused;
        }
        if(Input.GetKeyDown(KeyCode.RightArrow)&&learning.mode==HeartLearningController.LearningMode.Guided)learning.Advance(1);
        if(Input.GetKeyDown(KeyCode.LeftArrow)&&learning.mode==HeartLearningController.LearningMode.Guided)learning.Advance(-1);
        if(Input.GetKeyDown(KeyCode.R)){learning.Go(0);SetView(0);}
        if(Input.GetMouseButtonDown(1))mouse=Input.mousePosition;
        if(Input.GetMouseButton(1)){Vector3 d=Input.mousePosition-mouse;yaw+=d.x*.22f;pitch=Mathf.Clamp(pitch-d.y*.22f,-75,75);mouse=Input.mousePosition;}
        if(Input.mousePosition.x>Screen.width*.22f&&Input.mousePosition.x<Screen.width*.76f)distance=Mathf.Clamp(distance-Input.mouseScrollDelta.y*.30f,view==2&&valveSelection>=0?1.2f:3.5f,11);
        if(Input.GetMouseButtonDown(0)&&learning.mode==HeartLearningController.LearningMode.Explore&&Input.mousePosition.x>Screen.width*.22f&&Input.mousePosition.x<Screen.width*.76f){
            Ray original=viewCamera.ScreenPointToRay(Input.mousePosition),ray=original;
            if(section.section&&ray.direction.z>.0001f){float cut=(section.depth-ray.origin.z)/ray.direction.z;if(cut>0)ray.origin=ray.GetPoint(cut+.001f);}
            var hits=Physics.RaycastAll(ray);if(hits.Length==0)hits=Physics.RaycastAll(original);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){int i=System.Array.IndexOf(AtlasHeart.ChamberNames,hit.collider.name);if(i>=0){FocusChamber(i);break;}}
        }
    }
    void LateUpdate()
    {
        viewCamera.rect=new Rect(.21f,.17f,.55f,.75f);Vector3 target=new Vector3(0,.02f,.15f);
        if(view==2&&valveSelection>=0){
            target=AtlasHeart.ValvePosition[valveSelection];Vector3 direction=valves[valveSelection].transform.up;
            viewCamera.transform.position=target+Quaternion.Euler(pitch-48,yaw+14,0)*direction*distance;viewCamera.transform.LookAt(target,Vector3.forward);return;
        }
        viewCamera.transform.position=target+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);viewCamera.transform.LookAt(target);
    }
    IEnumerator Capture(string folder)
    {
        Directory.CreateDirectory(folder);learning.SetMode(HeartLearningController.LearningMode.Explore);cycle.speed=2;
        for(int f=0;f<180;f++)yield return null;
        if(flow.GateCrossings==0)throw new System.Exception("No blood passed a valve during runtime test.");
        cycle.speed=.45f;
        string[] names={"01-guided-venous","02-right-ventricle","03-left-ventricle","04-lung-exchange","05-exterior-atlas","06-valves-atlas","07-quiz-feedback","08-free-exploration"};
        for(int i=0;i<names.Length;i++){
            if(i<4){learning.SetMode(HeartLearningController.LearningMode.Guided);learning.Go(new[]{0,1,7,5}[i]);learning.elapsed=1.8f;}
            if(i==4){learning.SetMode(HeartLearningController.LearningMode.Explore);SetView(1);}if(i==5){learning.SetMode(HeartLearningController.LearningMode.Explore);SetView(2);SelectValve(3);cycle.SetPhase(.3f);cycle.playing=false;}
            if(i==6){learning.SetMode(HeartLearningController.LearningMode.Quiz);learning.Choose(0);}
            if(i==7){learning.SetMode(HeartLearningController.LearningMode.Explore);SetView(0);cycle.SetPhase(.3f);cycle.playing=false;}
            for(int f=0;f<8;f++)yield return null;yield return new WaitForEndOfFrame();string file=Path.Combine(folder,names[i]+".png");if(File.Exists(file))File.Delete(file);ScreenCapture.CaptureScreenshot(file);
            for(int f=0;f<60&&!File.Exists(file);f++)yield return null;if(!File.Exists(file))throw new System.Exception("Capture failed: "+file);
        }
        learning.SetMode(HeartLearningController.LearningMode.Quiz);
        if(learning.ShowPacket)throw new System.Exception("Quiz revealed route before an answer.");
        for(int q=0;q<HeartLearningController.Questions.Length;q++){
            learning.Choose(HeartLearningController.Questions[q].correct);int score=learning.correct;learning.Choose(0);
            if(!learning.ShowPacket)throw new System.Exception("Quiz feedback did not reveal the route.");
            if(learning.correct!=score)throw new System.Exception("Quiz duplicate answer changed score.");learning.NextQuestion();
        }
        if(learning.correct!=6||learning.question!=6)throw new System.Exception("Quiz completion failed.");
        learning.SetMode(HeartLearningController.LearningMode.Guided);
        for(int step=0;step<12;step++){learning.Go(step);if(learning.CurrentPath().Length<2)throw new System.Exception("Incomplete learning route "+step);}
        learning.Advance(1);if(learning.mode!=HeartLearningController.LearningMode.Quiz)throw new System.Exception("Guided-to-quiz transition failed.");
        Debug.Log("HEART_V2_RUNTIME_PASS · atlas parts="+atlas.Count+"; valve passages="+flow.GateCrossings+"; 12 routes; 6 quiz answers; duplicate-score guard; completion transition; 8 screenshots");Application.Quit();
    }
}
