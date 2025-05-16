using UnityEngine;

public class CircleMeshGenerator
{
    public static Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        // Center point
        vertices[0] = Vector3.zero;

        // Outer vertices in XY plane
        float angleStep = 360f / segments;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i - 1) * angleStep * Mathf.Deg2Rad;
            vertices[i] = new Vector3(Mathf.Cos(angle), -Mathf.Sin(angle), 0) * radius;
        }

        // Triangles
        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0; // Center
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}