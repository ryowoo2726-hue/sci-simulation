using System;
using System.Collections.Generic;
using UnityEngine;

// Meshes are generated once; animation only moves their transforms.
public static class AlveolarGeometry
{
    public static Mesh Sac(int rings = 64, int sides = 96)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i <= rings; i++)
        for (int j = 0; j <= sides; j++)
        {
            float a = i * Mathf.PI / rings, b = j * 2 * Mathf.PI / sides;
            var n = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b));
            float r = 1 + .025f * Mathf.Sin(7 * n.x + 3 * n.y) * Mathf.Sin(8 * n.z - n.y) + .012f * Mathf.Sin(19 * n.y + 7 * n.x);
            vertices.Add(n * r);
            if (i < rings && j < sides) { int k = i * (sides + 1) + j; triangles.AddRange(new[] { k, k + 1, k + sides + 1, k + 1, k + sides + 2, k + sides + 1 }); }
        }
        return HeartGeometry.MakeMesh(vertices, triangles, "Irregular alveolar wall");
    }

    public static Mesh Tube(Func<float, Vector3> curve, float radius, int segments = 100, int sides = 12)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments; Vector3 p = curve(t);
            Vector3 tangent = (curve(Mathf.Min(1, t + .001f)) - curve(Mathf.Max(0, t - .001f))).normalized;
            Vector3 reference = Mathf.Abs(tangent.y) > .9f ? Vector3.right : Vector3.up;
            Vector3 u = Vector3.Cross(tangent, reference).normalized, v = Vector3.Cross(tangent, u);
            for (int j = 0; j <= sides; j++)
            {
                float a = j * 2 * Mathf.PI / sides; vertices.Add(p + radius * (u * Mathf.Cos(a) + v * Mathf.Sin(a)));
                if (i < segments && j < sides) { int k = i * (sides + 1) + j; triangles.AddRange(new[] { k, k + 1, k + sides + 1, k + 1, k + sides + 2, k + sides + 1 }); }
            }
        }
        return HeartGeometry.MakeMesh(vertices, triangles, "Capillary lumen");
    }

    // Reused biconcave profile from ReferenceSource/SplineBloodFlowController.cs.txt.
    public static Mesh RedCell()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); const int rings = 24, sides = 40;
        for (int i = 0; i <= rings; i++)
        for (int j = 0; j <= sides; j++)
        {
            float theta = i * Mathf.PI / rings, r = Mathf.Sin(theta), a = j * 2 * Mathf.PI / sides;
            vertices.Add(new Vector3(r * Mathf.Cos(a), Mathf.Cos(theta) * (.16f + .45f * r * r), r * Mathf.Sin(a)));
            if (i < rings && j < sides) { int k = i * (sides + 1) + j; triangles.AddRange(new[] { k, k + 1, k + sides + 1, k + 1, k + sides + 2, k + sides + 1 }); }
        }
        return HeartGeometry.MakeMesh(vertices, triangles, "Biconcave erythrocyte");
    }

    public static Vector3[] ReadAtoms(string resource)
    {
        var lines = Resources.Load<TextAsset>(resource).text.Split('\n');
        int count = int.Parse(lines[3].Substring(0, 3)); var atoms = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            string line = lines[4 + i];
            float Number(int start) => float.Parse(line.Substring(start, 10), System.Globalization.CultureInfo.InvariantCulture);
            atoms[i] = new Vector3(Number(0), Number(10), Number(20));
        }
        return atoms;
    }
}
