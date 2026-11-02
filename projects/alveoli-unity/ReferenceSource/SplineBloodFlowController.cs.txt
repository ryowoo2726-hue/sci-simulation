using System.Collections.Generic;
using UnityEngine;

public class SplineBloodFlowController:MonoBehaviour
{
    // ponytail: paths and valve gates teach flow sequence; use a fluid solver for quantitative hemodynamics.
    public CardiacCycleController cycle;
    public Circulation simulation;
    public bool visible;
    public int GateCrossings{get;private set;}
    public static readonly Color Venous=new Color(74/255f,14/255f,23/255f),Arterial=new Color(230/255f,25/255f,25/255f);
    public static readonly Vector3[] RightRoute={new Vector3(-.90f,1.72f,.27f),new Vector3(-.92f,.90f,.27f),AtlasHeart.RA,AtlasHeart.Tricuspid,new Vector3(-.30f,-.80f,-.50f),new Vector3(.18f,-1.0f,-.65f),new Vector3(.22f,-.10f,-.50f),AtlasHeart.Pulmonary,new Vector3(.20f,1.08f,-.30f),new Vector3(.12f,1.30f,.35f),new Vector3(.90f,1.25f,1.10f)};
    public static readonly Vector3[] LeftRoute={new Vector3(.85f,.42f,1.0f),new Vector3(.60f,.42f,.95f),AtlasHeart.LA,AtlasHeart.Mitral,AtlasHeart.LV,new Vector3(.95f,-1.15f,.0f),new Vector3(.32f,-.30f,.12f),AtlasHeart.Aortic,new Vector3(-.20f,1.15f,.13f),new Vector3(-.10f,1.75f,.45f),new Vector3(.0f,1.75f,1.30f),new Vector3(.0f,1.45f,1.60f)};
    class Stream
    {
        public readonly List<int> order=new List<int>();
        public System.Comparison<int> compare;
        public Vector3[] points;public float[] arc;public float length,avGate,outGate;
        public readonly List<Transform> cells=new List<Transform>();public readonly List<float> distance=new List<float>();
    }
    readonly List<Stream> streams=new List<Stream>();
    readonly List<LineRenderer> arrows=new List<LineRenderer>();
    Transform packet;LineRenderer pathLine,halo;Material packetMaterial;Mesh cellMesh;
    public static float GateAdvance(float current,float proposed,float gate,bool canPass)=>current<gate&&proposed>=gate&&!canPass?gate-.0001f:proposed;
    public void Initialize(Circulation owner,Material venous,Material arterial)
    {
        simulation=owner;cycle=owner.cycle;cellMesh=RedCellMesh();Add(RightRoute,venous);Add(LeftRoute,arterial);
        var material=new Material(Shader.Find("Heart/GuideOverlay"));material.SetColor("_Color",new Color(.25f,.95f,.82f,.8f));
        LineRenderer Line(string name,int count,float width,Material m){var g=new GameObject(name);g.transform.SetParent(transform,false);var l=g.AddComponent<LineRenderer>();l.positionCount=count;l.startWidth=width;l.endWidth=width;l.sharedMaterial=m;l.numCapVertices=4;return l;}
        pathLine=Line("학습 경로 / 이동 방향",72,.015f,material);
        for(int i=0;i<7;i++)arrows.Add(Line("방향 화살표",3,.023f,material));
        packetMaterial=new Material(Shader.Find("Heart/GuideOverlay"));packetMaterial.SetColor("_Color",Venous);
        var p=new GameObject("추적하는 혈액 한 무리");p.transform.SetParent(transform,false);p.AddComponent<MeshFilter>().sharedMesh=cellMesh;p.AddComponent<MeshRenderer>().sharedMaterial=packetMaterial;packet=p.transform;packet.localScale=Vector3.one*.095f;
        var gold=new Material(Shader.Find("Heart/GuideOverlay"));gold.SetColor("_Color",new Color(1,.80f,.28f,1));halo=Line("추적 표시",49,.012f,gold);
    }
    void Add(Vector3[] path,Material mat)
    {
        var s=new Stream{points=path,arc=new float[601]};Vector3 prev=HeartGeometry.Curve(path,0);
        for(int i=1;i<=600;i++){Vector3 p=HeartGeometry.Curve(path,i/600f);s.arc[i]=s.arc[i-1]+Vector3.Distance(prev,p);prev=p;}
        s.length=s.arc[600];s.avGate=s.arc[Mathf.RoundToInt(3f/(path.Length-1)*600)];s.outGate=s.arc[Mathf.RoundToInt(7f/(path.Length-1)*600)];
        for(int i=0;i<16;i++){var g=new GameObject("적혈구 · "+mat.name);g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=cellMesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.transform.localScale=Vector3.one*.065f;s.cells.Add(g.transform);s.distance.Add(s.length*i/16);}
        for(int i=0;i<s.cells.Count;i++)s.order.Add(i);
        s.compare=(a,b)=>s.distance[b].CompareTo(s.distance[a]);
        streams.Add(s);
    }
    void LateUpdate()
    {
        foreach(var s in streams){
            s.order.Sort(s.compare);float ahead=float.PositiveInfinity;
            foreach(int i in s.order){float d=s.distance[i],velocity=d>s.avGate&&d<s.outGate?cycle.FlowRate(false):cycle.FlowRate(true);if(d>s.outGate)velocity=1.2f+cycle.FlowRate(false);
                float next=d+cycle.Delta*velocity*1.1f;
                next=GateAdvance(d,next,s.avGate,cycle.CanPass(true)&&cycle.AVOpen>.45f);next=GateAdvance(d,next,s.outGate,cycle.CanPass(false)&&cycle.SemilunarOpen>.8f);
                next=Mathf.Max(d,Mathf.Min(next,ahead-.14f));ahead=next;if(d<s.avGate&&next>=s.avGate||d<s.outGate&&next>=s.outGate)GateCrossings++;
                if(next>s.length)next=0;s.distance[i]=next;int k=System.Array.BinarySearch(s.arc,next);if(k<0)k=~k;k=Mathf.Clamp(k,1,600);
                float t=(k-1+Mathf.InverseLerp(s.arc[k-1],s.arc[k],next))/600;Vector3 pos=HeartGeometry.Curve(s.points,t),direction=HeartGeometry.Curve(s.points,Mathf.Min(1,t+.005f))-pos;
                s.cells[i].localPosition=pos;s.cells[i].localRotation=Quaternion.LookRotation(direction.sqrMagnitude>.0001f?direction:Vector3.up)*Quaternion.Euler(65,0,i*137);s.cells[i].gameObject.SetActive(visible);
            }
        }
        var learning=simulation.learning;bool guided=learning!=null&&learning.ShowPacket;
        packet.gameObject.SetActive(guided);halo.gameObject.SetActive(guided);pathLine.gameObject.SetActive(guided&&learning.showRoute);
        foreach(var arrow in arrows)arrow.gameObject.SetActive(guided&&learning.showRoute&&learning.step!=5&&learning.step!=10);
        if(!guided)return;
        Vector3[] path=learning.CurrentPath();Vector3 point=HeartGeometry.Curve(path,learning.Progress);packet.localPosition=point;packetMaterial.SetColor("_Color",learning.PacketColor);
        Vector3 tangent=HeartGeometry.Curve(path,Mathf.Min(1,learning.Progress+.01f))-HeartGeometry.Curve(path,Mathf.Max(0,learning.Progress-.01f));
        packet.rotation=Quaternion.LookRotation(tangent.sqrMagnitude>.001f?tangent:Vector3.up)*Quaternion.Euler(70,0,30);
        var camera=simulation.viewCamera;for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;halo.SetPosition(i,point+(camera.transform.right*Mathf.Cos(a)+camera.transform.up*Mathf.Sin(a))*.15f);}
        for(int i=0;i<72;i++)pathLine.SetPosition(i,HeartGeometry.Curve(path,i/71f));
        for(int i=0;i<arrows.Count;i++){
            float t=(i+.5f)/arrows.Count;Vector3 center=HeartGeometry.Curve(path,t),direction=(HeartGeometry.Curve(path,Mathf.Min(1,t+.015f))-HeartGeometry.Curve(path,Mathf.Max(0,t-.015f))).normalized;
            Vector3 side=Vector3.Cross(direction,camera.transform.forward).normalized*.055f;arrows[i].SetPosition(0,center-direction*.085f+side);arrows[i].SetPosition(1,center);arrows[i].SetPosition(2,center-direction*.085f-side);
        }
    }
    public static Mesh RedCellMesh()
    {
        var v=new List<Vector3>();var tri=new List<int>();const int rings=16,sides=32;
        for(int i=0;i<=rings;i++){
            float theta=i*Mathf.PI/rings,r=Mathf.Sin(theta),z=Mathf.Cos(theta)*(.16f+.45f*r*r);
            for(int j=0;j<=sides;j++){float a=j*Mathf.PI*2/sides;v.Add(new Vector3(r*Mathf.Cos(a),z,r*Mathf.Sin(a)));
                if(i<rings&&j<sides){int k=i*(sides+1)+j;tri.AddRange(new[]{k,k+1,k+sides+1,k+1,k+sides+2,k+sides+1});}}
        }
        return HeartGeometry.MakeMesh(v,tri,"Biconcave erythrocyte");
    }
}


