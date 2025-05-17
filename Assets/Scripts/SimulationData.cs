using UnityEngine;
using System.Runtime.InteropServices;

struct Particle
{
    public Vector2 position;       // 8 bytes
    public Vector2 velocity;       // 16 bytes
}

[StructLayout(LayoutKind.Sequential)]
public struct Matrix2x2
{
    public Vector2 col1;
    public Vector2 col2;

    public Matrix2x2(float angleRadians)
    {
        float c = Mathf.Cos(angleRadians);
        float s = Mathf.Sin(angleRadians);
        col1 = new Vector2(c, s);
        col2 = new Vector2(-s, c);
    }

    public Vector2 Multiply(Vector2 v)
    {
        return new Vector2(
            v.x * col1.x + v.y * col2.x,
            v.x * col1.y + v.y * col2.y
        );
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Obstacle
{
    public int type;             // 0 = circle, 1 = rectangle
    public Vector2 position;
    public Vector2 size;         // radius or half extents
    public Matrix2x2 rotation;
}