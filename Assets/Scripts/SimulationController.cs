using System.Runtime.InteropServices;
using UnityEngine;

[System.Serializable]
[StructLayout(LayoutKind.Sequential, Size = 16)]
public class SimulationController : MonoBehaviour
{
    public ParticleSpawnSettings spawnSettings;
    [Header("Simulation Parameters")]
    public float radius = 0.1f;
    public int substeps = 4;
    public int iterations = 30;
    public float startPauseTime = 1f;
    public Vector2 containerBounds = new Vector2(5, 5);

    [Header("Physics Settings")]
    public ComputeShader physicsShader;
    public Material material;
    [Range(0f, 1f)] public float penetrationFactor = 0.3f;
    public float dampingFactor = 0.98f;
    public float gravity = -9.81f;

    private Mesh circleMesh;
    private ComputeBuffer particlesBufferRead;
    private ComputeBuffer particlesBufferWrite;
    private ComputeBuffer obstacleBuffer;

    private int gravityKernel;
    private int solveCollisionsKernel;

    private Obstacle[] obstacles;
    private bool isSimulating = false;

    void Start()
    {
        InitializeKernels();
        InitializeMesh();
        InitializeBuffers();
        InitializeParticles();
        InitializeObstacles();
        UploadConstantsToShader();
    }

    void InitializeKernels()
    {
        solveCollisionsKernel = physicsShader.FindKernel("SolveCollisions");
        gravityKernel = physicsShader.FindKernel("ApplyGravity");
    }

    void InitializeMesh()
    {
        circleMesh = CircleMeshGenerator.CreateCircleMesh(radius, 32);
    }

    void InitializeBuffers()
    {
        int stride = Marshal.SizeOf(typeof(Particle));
        particlesBufferRead = new ComputeBuffer(spawnSettings.particleCount, stride);
        particlesBufferWrite = new ComputeBuffer(spawnSettings.particleCount, stride);
        material.SetBuffer("_Particles", particlesBufferRead);
    }

    void InitializeParticles()
    {
        Particle[] particles = new Particle[spawnSettings.particleCount];

        IParticleSpawner spawner = spawnSettings.spawnType switch
        {
            SpawnType.Rectangle => new RectangleSpawner(),
            // SpawnType.Circle => new CircleSpawner(),
            // SpawnType.OneByOne => new OneByOneSpawner(),
            // SpawnType.Batch => new BatchSpawner(),
            _ => throw new System.NotImplementedException()
        };

        spawner.Initialize(spawnSettings.particleCount, radius, spawnSettings.spawnAreaSize, spawnSettings.spawnAreaCenter);

        if (spawner is IBatchParticleSpawner batch)
        {
            particles = batch.GenerateParticles();
        }
        else if (spawner is IIncrementalParticleSpawner incremental)
        {
            // StartCoroutine(incremental.SpawnParticlesOverTime((p, i) =>
            // {
            //     particles[i] = p;
            // }));
        }
        else
        {
            Debug.LogError("Invalid spawner type");
            return;
        }

        particlesBufferRead.SetData(particles);
        particlesBufferWrite.SetData(particles);
    }

    void InitializeObstacles()
    {
        var sceneObstacles = FindObjectsOfType<ParticleObstacle>();
        Obstacle[] obstacles = new Obstacle[sceneObstacles.Length];

        for (int i = 0; i < sceneObstacles.Length; i++)
        {
            obstacles[i] = sceneObstacles[i].ToObstacle();
        }

        obstacleBuffer = new ComputeBuffer(obstacles.Length, Marshal.SizeOf(typeof(Obstacle)));
        obstacleBuffer.SetData(obstacles);

        physicsShader.SetBuffer(solveCollisionsKernel, "_Obstacles", obstacleBuffer);
        physicsShader.SetInt("obstacleCount", obstacles.Length);
    }

    void UploadConstantsToShader()
    {
        physicsShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        physicsShader.SetFloat("radius", radius);
        physicsShader.SetFloat("penetrationFactor", penetrationFactor);
        physicsShader.SetFloat("dampingFactor", dampingFactor);
        physicsShader.SetFloat("gravity", gravity);
        physicsShader.SetVector("bounds", containerBounds);
    }



    void FixedUpdate()
    {
        // Pause simulation for a bit before starting
        if (!isSimulating)
        {
            startPauseTime -= Time.fixedDeltaTime;
            if (startPauseTime <= 0f)
            {
                isSimulating = true;
            }
            return;
        }

        // Calculate thread groups
        int threadGroups = Mathf.CeilToInt(spawnSettings.particleCount / 64f);

        float subDelta = Time.fixedDeltaTime / substeps;

        physicsShader.SetFloat("deltaTime", subDelta);

        for (int s = 0; s < substeps; s++)
        {
            // Apply gravity
            physicsShader.SetBuffer(gravityKernel, "_ReadParticles", particlesBufferRead);
            physicsShader.SetBuffer(gravityKernel, "_WriteParticles", particlesBufferWrite);
            physicsShader.Dispatch(gravityKernel, threadGroups, 1, 1);
            SwapBuffers();

            // Solve collisions
            for (int i = 0; i < iterations; i++)
            {
                physicsShader.SetBuffer(solveCollisionsKernel, "_ReadParticles", particlesBufferRead);
                physicsShader.SetBuffer(solveCollisionsKernel, "_WriteParticles", particlesBufferWrite);
                physicsShader.Dispatch(solveCollisionsKernel, threadGroups, 1, 1);
                SwapBuffers();
            }
        }
    }

    void SwapBuffers()
    {
        var temp = particlesBufferRead;
        particlesBufferRead = particlesBufferWrite;
        particlesBufferWrite = temp;
    }

    void Update()
    {
        Graphics.DrawMeshInstancedProcedural(
            circleMesh,
            0,
            material,
            circleMesh.bounds,
            spawnSettings.particleCount
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
