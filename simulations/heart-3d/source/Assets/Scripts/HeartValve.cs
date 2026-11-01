using System.Collections.Generic;
using UnityEngine;

public class HeartValve:MonoBehaviour
{
    public bool atrioventricular;
    public int leafletCount;
    public float radius;
    public CardiacCycleController cycle;
    public float Opening=>atrioventricular?cycle.AVOpen:cycle.SemilunarOpen;
    Mesh mesh;Vector3[] vertices;int[] indices;
    float lastOpening=float.NaN;
    Vector3[] atlasRest;
    Vector3[] cuspDirection;
    readonly List<LineRenderer> chords=new List<LineRenderer>();
    readonly List<Vector3> anchors=new List<Vector3>();
    const int Radial=10, Angular=18;
    public void Initialize(bool av,int count,float r,Material material,Material cord,CardiacCycleController clock)
    {
        atrioventricular=av;leafletCount=count;radius=r;cycle=clock;
        mesh=new Mesh{name=name+" deforming leaflets"};mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        vertices=new Vector3[count*(Radial+1)*(Angular+1)];var tri=new List<int>();
        for(int leaf=0;leaf<count;leaf++)for(int r0=0;r0<Radial;r0++)for(int a=0;a<Angular;a++){
            int k=leaf*(Radial+1)*(Angular+1)+r0*(Angular+1)+a;tri.AddRange(new[]{k,k+1,k+Angular+1,k+1,k+Angular+2,k+Angular+1});}
        indices=tri.ToArray();mesh.vertices=vertices;mesh.triangles=indices;
        if(av)for(int leaf=0;leaf<count;leaf++)for(int j=0;j<4;j++){
            var g=new GameObject("건삭 · chordae tendineae");g.transform.SetParent(transform,false);var line=g.AddComponent<LineRenderer>();
            line.useWorldSpace=false;line.positionCount=8;line.startWidth=.006f;line.endWidth=.0035f;line.numCapVertices=3;line.sharedMaterial=cord;chords.Add(line);
            float angle=(leaf+.5f)*Mathf.PI*2/count;
            anchors.Add(new Vector3(Mathf.Cos(angle)*radius*.68f,-.65f,Mathf.Sin(angle)*radius*.65f+.12f));
        }
        UpdateMesh();
    }
    public void InitializeAtlas(Mesh source,float r,Material material,CardiacCycleController clock)
    {
        atrioventricular=false;leafletCount=3;radius=r;cycle=clock;
        mesh=Instantiate(source);mesh.name=name+" · atlas cusp deformation";
        vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=transform.InverseTransformPoint(vertices[i]);
        atlasRest=(Vector3[])vertices.Clone();mesh.vertices=vertices;
        int[] parents=new int[vertices.Length];for(int i=0;i<parents.Length;i++)parents[i]=i;
        int Root(int i){while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;}
        int[] triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3){parents[Root(triangles[i+1])]=Root(triangles[i]);parents[Root(triangles[i+2])]=Root(triangles[i]);}
        var centers=new Dictionary<int,Vector3>();var counts=new Dictionary<int,int>();
        for(int i=0;i<vertices.Length;i++){int root=Root(i);if(!centers.ContainsKey(root)){centers[root]=Vector3.zero;counts[root]=0;}centers[root]+=vertices[i];counts[root]++;}
        cuspDirection=new Vector3[vertices.Length];for(int i=0;i<vertices.Length;i++){Vector3 center=centers[Root(i)]/counts[Root(i)];cuspDirection[i]=new Vector3(center.x,0,center.z).normalized;}
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;mesh.MarkDynamic();UpdateMesh();
    }
    Vector3 Point(int leaf,float along,float across)
    {
        float a=(leaf+across)*Mathf.PI*2/leafletCount;
        float open=Opening;
        // Free margin moves down into ventricle for AV valves, out to the wall for pockets.
        float inner=atrioventricular?Mathf.Lerp(.015f,.78f,open):Mathf.Lerp(.015f,.89f,open);
        float scallop=Mathf.Sin(across*Mathf.PI);
        float r=radius*Mathf.Lerp(1,inner+open*.045f*scallop,along);
        float y=atrioventricular?-radius*(.10f+open*1.12f)*Mathf.Sin(along*Mathf.PI*.5f):radius*(.13f+.30f*scallop)*Mathf.Sin(along*Mathf.PI*.7f)*(1-open*.65f);
        y+=.012f*Mathf.Sin(along*9+across*6)*Mathf.Sin(along*Mathf.PI)*open;
        if(atrioventricular)y-=open*radius*.10f*scallop*along;
        return new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);
    }
    void LateUpdate(){if(mesh!=null)UpdateMesh();}
    void UpdateMesh()
    {
        if(Opening==lastOpening)return;
        lastOpening=Opening;
        if(atlasRest!=null){
            for(int i=0;i<vertices.Length;i++){
                Vector3 p=atlasRest[i],outward=cuspDirection[i];float radial=new Vector2(p.x,p.z).magnitude;
                float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius*.35f,radius*.98f,radial));
                Vector3 hinge=outward*radius,axis=Vector3.Cross(Vector3.up,outward);
                vertices[i]=hinge+Quaternion.AngleAxis(Opening*82*weight,axis)*(p-hinge);
            }
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();return;
        }
        int k=0;for(int l=0;l<leafletCount;l++)for(int r=0;r<=Radial;r++)for(int a=0;a<=Angular;a++)vertices[k++]=Point(l,(float)r/Radial,(float)a/Angular);
        mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();
        for(int i=0;i<chords.Count;i++){
            Vector3 a=Point(i/4,1,(i%4+.5f)/4),b=anchors[i];
            for(int j=0;j<8;j++){float u=j/7f;Vector3 p=Vector3.Lerp(a,b,u);p.x+=Mathf.Sin(u*Mathf.PI)*.055f*Opening;chords[i].SetPosition(j,p);}
        }
    }
    public IEnumerable<Vector3> PapillaryTips()=>anchors;
    public void AttachToPapillaryTips(IList<Vector3> worldTips)
    {
        if(worldTips.Count==0)return;
        lastOpening=float.NaN;
        for(int i=0;i<anchors.Count;i++)anchors[i]=transform.InverseTransformPoint(worldTips[(i/4)%worldTips.Count]);
    }
}
