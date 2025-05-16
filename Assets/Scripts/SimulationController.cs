using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 16)]
public class SimulationController : MonoBehaviour
{
    struct Particle
    {
        public Vector2 position;       // 8 bytes
        public Vector2 velocity;       // 16 bytes
    }

    public int particleCount;
    public float radius;
    public float penetrationFactor;
    public ComputeShader physicsShader;
    public Material material;
    public Vector2 containerBounds;
    private Mesh circleMesh;
    private ComputeBuffer particlesBufferRead;
    private ComputeBuffer particlesBufferWrite;
    private int solveCollisionsKernel;
    private int gravityKernel;
    private Vector2 gravity;
    private float dampingFactor;

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

        Particle[] particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = new Particle
            {
                position = new Vector2(UnityEngine.Random.Range(-containerBounds.x + radius, containerBounds.x - radius),
                UnityEngine.Random.Range(-containerBounds.y + radius, containerBounds.y - radius)),
                velocity = Vector2.zero,
            };
        }


        particlesBufferRead.SetData(particles);
        particlesBufferWrite.SetData(particles);

        material.SetBuffer("_Particles", particlesBufferRead);
    }

    void FixedUpdate()
    {
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
        int iterations = 30;
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
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0.6f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(Vector2.zero, containerBounds * 2);
    }
}
