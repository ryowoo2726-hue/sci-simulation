using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

// Z-Anatomy / BodyParts3D derivative. See ModelSources/ATTRIBUTION.md (CC BY-SA 4.0).
public static class AtlasHeart
{
    public static readonly Vector3 RA=new Vector3(-.92f,.10f,.15f),RV=new Vector3(.05f,-.78f,-.64f),LA=new Vector3(.25f,.42f,.86f),LV=new Vector3(.80f,-.80f,.20f);
    public static readonly Vector3 Tricuspid=new Vector3(-.58f,-.37f,-.12f),Mitral=new Vector3(.45f,-.14f,.59f),Pulmonary=new Vector3(.22f,.50f,-.43f),Aortic=new Vector3(-.16f,.40f,.18f);
    public static readonly Vector3[] ValvePosition={Tricuspid,Mitral,Pulmonary,Aortic};
    public static readonly Vector3[] ValveFlow={new Vector3(.55f,-.50f,-.40f),new Vector3(.32f,-.64f,-.23f),new Vector3(.05f,.97f,.20f),new Vector3(-.08f,.97f,-.14f)};
    public static readonly string[] ChamberNames={"Right atrium","Right ventricle","Left atrium","Left ventricle"};
    public static Dictionary<string,Mesh> Load()
    {
        var file=Resources.Load<TextAsset>("Atlas/Heart");if(file==null)throw new InvalidOperationException("Atlas heart data is missing.");
        var result=new Dictionary<string,Mesh>();
        using(var reader=new BinaryReader(new MemoryStream(file.bytes),Encoding.UTF8)){
            int count=reader.ReadInt32();if(count<20||count>100)throw new InvalidDataException("Atlas part count");
            for(int part=0;part<count;part++){
                int length=reader.ReadInt32();if(length<1||length>256)throw new InvalidDataException("Atlas name");
                string name=Encoding.UTF8.GetString(reader.ReadBytes(length)).Replace(".001","");
                int n=reader.ReadInt32();if(n<1||n>2000000)throw new InvalidDataException("Atlas vertices");
                var vertices=new Vector3[n];for(int i=0;i<n;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                int m=reader.ReadInt32();if(m<3||m>12000000||m%3!=0)throw new InvalidDataException("Atlas triangles");
                var triangles=new int[m];for(int i=0;i<m;i++){triangles[i]=reader.ReadInt32();if(triangles[i]<0||triangles[i]>=n)throw new InvalidDataException("Atlas index");}
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();result.Add(name,mesh);
            }
        }
        return result;
    }
    public static Mesh Combine(IEnumerable<Mesh> meshes,string name)
    {
        var entries=new List<CombineInstance>();foreach(Mesh m in meshes)entries.Add(new CombineInstance{mesh=m,transform=Matrix4x4.identity});
        var combined=new Mesh{name=name,indexFormat=IndexFormat.UInt32};combined.CombineMeshes(entries.ToArray(),true,true);combined.RecalculateBounds();return combined;
    }
    public static Mesh Cap(Mesh body,float z)
    {
        var segments=new List<Vector4>();var windingDirection=new List<int>();Vector3[] v=body.vertices;int[] indices=body.triangles;
        for(int i=0;i<indices.Length;i+=3){
            Vector3[] p={v[indices[i]],v[indices[i+1]],v[indices[i+2]]};var points=new List<Vector3>(2);
            for(int j=0;j<3;j++){Vector3 a=p[j],b=p[(j+1)%3];if((a.z<z)!=(b.z<z))points.Add(Vector3.Lerp(a,b,(z-a.z)/(b.z-a.z)));}
            if(points.Count==2){segments.Add(new Vector4(points[0].x,points[0].y,points[1].x,points[1].y));windingDirection.Add(Vector3.Cross(p[1]-p[0],p[2]-p[0]).x<0?1:-1);}
        }
        var verts=new List<Vector3>();var triangles=new List<int>();var hits=new List<Vector2>();var levels=new SortedSet<float>();
        foreach(Vector4 s in segments){levels.Add(s.y);levels.Add(s.w);}var bands=new List<float>(levels);
        for(int band=0;band+1<bands.Count;band++){
            float y=bands[band],top=bands[band+1];if(top-y<.00001f)continue;
            hits.Clear();float mid=(y+top)*.5f;
            for(int i=0;i<segments.Count;i++){Vector4 s=segments[i];if((s.y<=mid&&mid<s.w)||(s.w<=mid&&mid<s.y))hits.Add(new Vector2(Mathf.Lerp(s.x,s.z,(mid-s.y)/(s.w-s.y)),i));}
            hits.Sort((a,b)=>a.x.CompareTo(b.x));
            int winding=0,start=-1;
            for(int i=0;i<hits.Count;i++){
                int before=winding;winding+=windingDirection[(int)hits[i].y];
                if(before==0&&winding!=0){start=(int)hits[i].y;continue;}
                if(before==0||winding!=0||start<0)continue;
                Vector4 a=segments[start],b=segments[(int)hits[i].y];
                float X(Vector4 s,float yy)=>Mathf.Lerp(s.x,s.z,Mathf.Clamp01((yy-s.y)/(s.w-s.y)));
                int k=verts.Count;verts.Add(new Vector3(X(a,y),y,z));verts.Add(new Vector3(X(b,y),y,z));verts.Add(new Vector3(X(b,top),top,z));verts.Add(new Vector3(X(a,top),top,z));triangles.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});
                start=-1;
            }
        }
        return HeartGeometry.MakeMesh(verts,triangles,"Atlas myocardial section");
    }
}
