using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class HeartGeometry
{
    public static Vector3 Curve(Vector3[] points,float t)
    {
        float u=Mathf.Clamp01(t)*(points.Length-1);int i=Mathf.Min((int)u,points.Length-2);u-=i;
        Vector3 a=points[Mathf.Max(0,i-1)],b=points[i],c=points[i+1],d=points[Mathf.Min(points.Length-1,i+2)];
        return .5f*(2*b+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
    }
    public static Mesh MakeMesh(List<Vector3> vertices,List<int> triangles,string name)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
