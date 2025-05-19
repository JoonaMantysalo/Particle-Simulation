using UnityEngine;

[CreateAssetMenu(fileName = "ParticleSpawnSettings", menuName = "Simulation/ParticleSpawnSettings")]
public class ParticleSpawnSettings : ScriptableObject
{
    public int particleCount = 1000;
    public Vector2 spawnAreaCenter = Vector2.zero;
    public Vector2 spawnAreaSize = new Vector2(5f, 5f);
    public SpawnType spawnType = SpawnType.Rectangle;
    public float spawnInterval = 0.05f;
}

public enum SpawnType
{
    Rectangle,
    Circle,
    OneByOne,
}
