using UnityEngine;

public enum ObstacleType { Circle, Rectangle }

public class ParticleObstacle : MonoBehaviour
{
    public ObstacleType type = ObstacleType.Circle;

    public Obstacle ToObstacle()
    {
        var obs = new Obstacle
        {
            type = (int)type,
            position = transform.position,
            size = transform.localScale / 2,
        };

        if (type == ObstacleType.Rectangle)
        {
            float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
            obs.rotation = new Matrix2x2(angle);
        }

        return obs;
    }
}
