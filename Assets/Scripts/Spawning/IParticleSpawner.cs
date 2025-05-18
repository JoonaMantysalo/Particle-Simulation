using System.Collections;
using UnityEngine;

public interface IParticleSpawner
{
    void Initialize(int count, float radius, Vector2 bounds, Vector2 center);
}

public interface IBatchParticleSpawner : IParticleSpawner
{
    Particle[] GenerateParticles();
}

public interface IIncrementalParticleSpawner : IParticleSpawner
{
    IEnumerator SpawnParticlesOverTime(System.Action<Particle, int> onSpawn);
}
