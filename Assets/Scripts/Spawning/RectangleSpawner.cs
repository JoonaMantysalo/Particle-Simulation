using UnityEngine;

public class RectangleSpawner : IBatchParticleSpawner
{
    private int count;
    private float radius;
    private Vector2 area;
    private Vector2 center;

    public void Initialize(int count, float radius, Vector2 area, Vector2 center)
    {
        this.center = center;
        this.area = area;
        this.radius = radius;
        this.count = count;
        this.center = center;
    }

    public Particle[] GenerateParticles()
    {
        Particle[] particles = new Particle[count];
        for (int i = 0; i < count; i++)
        {
            particles[i] = new Particle
            {
                position = new Vector2(
                    Random.Range(center.x - area.x, center.x + area.x),
                    Random.Range(center.y - area.y, center.y + area.y)
                ),
                velocity = Vector2.zero
            };
        }
        return particles;
    }
}
