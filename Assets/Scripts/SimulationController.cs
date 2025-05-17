using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 16)]
public class SimulationController : MonoBehaviour
{
    public int particleCount;
    public float radius;
    [Range(0, 1)]
    public float penetrationFactor;
    [Range(1, 100)]
    public int iterations;
    public ComputeShader physicsShader;
    public Material material;
    public Vector2 containerBounds;

    private Mesh circleMesh;
    private ComputeBuffer particlesBufferRead;
    private ComputeBuffer particlesBufferWrite;
    private ComputeBuffer obstacleBuffer;
    private int solveCollisionsKernel;
    private int gravityKernel;
    private Obstacle[] obstacles;
    private Vector2 gravity;
    private float dampingFactor;
    private float delayTimer;
    private bool isSimulating = false;

    void Start()
    {
        circleMesh = CircleMeshGenerator.CreateCircleMesh(radius, 32);

        int stride = Marshal.SizeOf(typeof(Particle));
        particlesBufferRead = new ComputeBuffer(particleCount, stride);
        particlesBufferWrite = new ComputeBuffer(particleCount, stride);

        solveCollisionsKernel = physicsShader.FindKernel("SolveCollisions");
        gravityKernel = physicsShader.FindKernel("ApplyGravity");

        gravity = new float2(0f, -9.81f);
        dampingFactor = 0.98f;
        delayTimer = 1f;

        Particle[] particles = new Particle[particleCount];
        SpawnParticles(particles);

        particlesBufferRead.SetData(particles);
        particlesBufferWrite.SetData(particles);

        SetObstacles();
        obstacleBuffer = new ComputeBuffer(obstacles.Length, Marshal.SizeOf(typeof(Obstacle)));
        obstacleBuffer.SetData(obstacles);

        physicsShader.SetBuffer(solveCollisionsKernel, "_Obstacles", obstacleBuffer);
        physicsShader.SetInt("obstacleCount", obstacles.Length);

        material.SetBuffer("_Particles", particlesBufferRead);
    }

    void SpawnParticles(Particle[] particles)
    {
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = new Particle
            {
                position = new Vector2(UnityEngine.Random.Range(-containerBounds.x + radius, containerBounds.x - radius),
                UnityEngine.Random.Range(-containerBounds.y + radius, containerBounds.y - radius)),
                velocity = Vector2.zero,
            };
        }
    }

    void SetObstacles()
    {
        var sceneObstacles = FindObjectsOfType<ParticleObstacle>();
        obstacles = new Obstacle[sceneObstacles.Length];

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            obstacles[i] = sceneObstacles[i].ToObstacle();
        }
    }

    void FixedUpdate()
    {
        // Pause simulation for a bit before starting
        if (!isSimulating)
        {
            delayTimer -= Time.fixedDeltaTime;
            if (delayTimer <= 0f)
            {
                isSimulating = true;
            }
            return;
        }

        physicsShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        physicsShader.SetFloat("radius", radius);
        physicsShader.SetVector("gravity", gravity);
        physicsShader.SetFloat("dampingFactor", dampingFactor);
        physicsShader.SetFloat("penetrationFactor", penetrationFactor);
        physicsShader.SetVector("bounds", containerBounds);

        // Calculate thread groups
        int threadGroups = Mathf.CeilToInt(particleCount / 64f);

        // Gravity
        physicsShader.SetBuffer(gravityKernel, "_ReadParticles", particlesBufferRead);
        physicsShader.SetBuffer(gravityKernel, "_WriteParticles", particlesBufferWrite);
        physicsShader.Dispatch(gravityKernel, threadGroups, 1, 1);

        var tempFinal = particlesBufferRead;
        particlesBufferRead = particlesBufferWrite;
        particlesBufferWrite = tempFinal;

        // Solve collisions
        for (int i = 0; i < iterations; i++)
        {
            physicsShader.SetBuffer(solveCollisionsKernel, "_ReadParticles", particlesBufferRead);
            physicsShader.SetBuffer(solveCollisionsKernel, "_WriteParticles", particlesBufferWrite);

            physicsShader.Dispatch(solveCollisionsKernel, threadGroups, 1, 1);

            var temp = particlesBufferRead;
            particlesBufferRead = particlesBufferWrite;
            particlesBufferWrite = temp;
        }
    }

    void Update()
    {
        Graphics.DrawMeshInstancedProcedural(
            circleMesh,
            0,
            material,
            circleMesh.bounds,
            particleCount
        );
    }

    void OnDestroy()
    {
        particlesBufferRead.Release();
        particlesBufferWrite.Release();
        obstacleBuffer.Release();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.6f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(Vector2.zero, containerBounds * 2);
    }
}
