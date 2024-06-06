using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour
{


    public ComputeShader compute;

    public DisplayParticles display;

    //settings
    public int numParticles = 1000;
    public Vector3 gravity;
    public float collisionDamping = 0.8f;
    public float smoothingRadius = 1f;
    public float targetDensity = 2f;
    public float pressureMultiplier = 0.5f;

    public GameObject[] sphereObstacles;
    public GameObject[] boxObstacles;

    private int closeSpheres = 0;
    private Vector4[] sphereData;

    private int closeBoxes = 0;
    private float2x3[] boxData;

    [SerializeField]
    private int maxParticles;
    private int lastFrameParticles;
    private int oldestPatrticle = 0;

    [Header("Spawn New Particles")]
    [SerializeField]
    Transform spawnNewParticlesTransform;
    [SerializeField]
    float spawnRadius = 1;
    [SerializeField]
    Vector3 spawnVelocity = new Vector3(0, 0, 0);
    [SerializeField]
    int spawnparticlesPerFXDUpdate = 1;

    // Buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }

    public ComputeBuffer predictedPositionsBuffer { get; private set; }

    public ComputeBuffer externalSpheres { get; private set; }
    public ComputeBuffer externalBoxes { get; private set; }

    // Kernels (methods in the pragmas at top of compute shader)
    const int updatePositionKernel = 0;
    const int calculateDensityKernel = 1;
    const int calculatePressureForceKernel = 2;
    const int externalForcesKernel = 3;

    private void Start()
    {
        Debug.Log("Simulation Start");
        compute = Instantiate(compute);

        lastFrameParticles = numParticles;

        //create buffers here
        positionBuffer = ComputeHelper.CreateBuffer<Vector3>(maxParticles);
        velocityBuffer = ComputeHelper.CreateBuffer<Vector3>(maxParticles);
        densityBuffer = ComputeHelper.CreateBuffer<float>(maxParticles);
        predictedPositionsBuffer = ComputeHelper.CreateBuffer<float3>(maxParticles);
        externalSpheres = ComputeHelper.CreateBuffer<float4>(sphereObstacles.Length + 1);
        sphereData = new Vector4[sphereObstacles.Length];
        externalBoxes = ComputeHelper.CreateBuffer<float2x3>(boxObstacles.Length + 1);
        boxData = new float2x3[boxObstacles.Length];
        // tell each compute shader method (called kernel) which buffers will be used
        ComputeHelper.SetBuffer(compute, "Positions", positionBuffer, updatePositionKernel, calculateDensityKernel, calculatePressureForceKernel, externalForcesKernel);
        ComputeHelper.SetBuffer(compute, "Velocities", velocityBuffer, updatePositionKernel, calculatePressureForceKernel, externalForcesKernel);
        ComputeHelper.SetBuffer(compute, "Densities", densityBuffer, calculateDensityKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "PredictedPositions", predictedPositionsBuffer, externalForcesKernel, updatePositionKernel, calculateDensityKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "SphereObstacles", externalSpheres, externalForcesKernel);
        ComputeHelper.SetBuffer(compute, "BoxObstacles", externalBoxes, externalForcesKernel);

        SpawnParticles(numParticles);
        //const deltatime
        Time.fixedDeltaTime = 1 / 60f;

        SetComputeVariables();
    }

    public void SetComputeVariables()
    {
        if(numParticles != maxParticles)
            numParticles += spawnparticlesPerFXDUpdate;
        else
            numParticles -= spawnparticlesPerFXDUpdate;


        compute.SetFloats("boundsSize", new float[]
        {
            display.UpperBoundary().x - display.scale,
            display.UpperBoundary().y - display.scale,
            display.UpperBoundary().z - display.scale
        });

        if(numParticles > maxParticles)
            numParticles = maxParticles;

        if (lastFrameParticles < numParticles){
            int particlesToSpawn = numParticles - lastFrameParticles;
            float3[] newPositions = new float3[particlesToSpawn];
            float3[] newVelocities = new float3[particlesToSpawn];

            for(int i = 0; i < particlesToSpawn; i++)
            {
                newPositions[i] = (spawnNewParticlesTransform.position - transform.position) + Random.insideUnitSphere * spawnRadius;
                newVelocities[i] = spawnVelocity;
            }

            LoadNewParticle(particlesToSpawn, newPositions, newVelocities);
        }

        display.numParticles = numParticles;

        compute.SetFloat("deltaTime", Time.fixedDeltaTime);
        compute.SetInt("numParticles", numParticles);
        compute.SetFloats("gravity", new float[] { gravity.x, gravity.y, gravity.z });
        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetFloat("smoothingRadius", smoothingRadius);
        compute.SetFloat("targetDensity", targetDensity);
        compute.SetFloat("pressureMultiplier", pressureMultiplier);

        compute.SetMatrix("localToWorld", transform.localToWorldMatrix);
        compute.SetMatrix("worldToLocal", transform.worldToLocalMatrix);

        CheckClosestSphere();
        if(closeSpheres > 0)
            externalSpheres.SetData(sphereData);
        compute.SetInt("sphereCount", closeSpheres);

        CheckCLosestBoxes();
        if(closeBoxes > 0)
            externalBoxes.SetData(boxData);
        compute.SetInt("boxCount", closeBoxes);


        lastFrameParticles = numParticles;
    }

    //do sim here
    private void FixedUpdate()
    {
        SetComputeVariables();
        //PrintDataAboutParticle(0, "before");
        ComputeHelper.Dispatch(compute, numParticles, externalForcesKernel);
        //PrintDataAboutParticle(0, "external");
        ComputeHelper.Dispatch(compute, numParticles, calculateDensityKernel);
        //PrintDataAboutParticle(0, "density");
        ComputeHelper.Dispatch(compute, numParticles, calculatePressureForceKernel);
        //PrintDataAboutParticle(0, "pressure");
        ComputeHelper.Dispatch(compute, numParticles, updatePositionKernel);
        //PrintDataAboutParticle(0, "positions");

        //get positions from buffer
        positionBuffer.GetData(display.positions);
        //densityBuffer.GetData(display.density);

        display.targetDensity = targetDensity;
    }

    private void CheckClosestSphere()
    {
        sphereObstacles = sphereObstacles.OrderBy(x => Vector3.Distance(x.transform.position, transform.position)).ToArray();

        float maxDistance = transform.localScale.magnitude * 2;
        closeSpheres = 0;
        foreach (var sphere in sphereObstacles)
        {
            
            float distance = Vector3.Distance(sphere.transform.position, transform.position) - sphere.transform.localScale.x - maxDistance;
            if (distance > 0)
                break;
            Vector3 spherePos = sphere.transform.position - transform.position;
            sphereData[closeSpheres] = new Vector4(spherePos.x, spherePos.y, spherePos.z, sphere.transform.localScale.x/2);
            closeSpheres++;
        }
    }

    private void CheckCLosestBoxes()
    {
        List<GameObject> boxesInRange = new List<GameObject>();

        float BBSimRadius = transform.localScale.magnitude * 2;

        foreach (var box in boxObstacles)
        {
            if(Vector3.Distance(box.transform.position, transform.position) - BBSimRadius - box.transform.localScale.magnitude <= 0)
            {
                boxesInRange.Add(box);
            }
        }

        closeBoxes = boxesInRange.Count;

        for (int i = 0; i < closeBoxes; i++)
        {
            boxData[i].c0 = new float2(boxesInRange[i].transform.position.x - transform.position.x, boxesInRange[i].transform.localScale.x);
            boxData[i].c1 = new float2(boxesInRange[i].transform.position.y - transform.position.y, boxesInRange[i].transform.localScale.y);
            boxData[i].c2 = new float2(boxesInRange[i].transform.position.z - transform.position.z, boxesInRange[i].transform.localScale.z);
        }

        Debug.Log("Close boxes: " + closeBoxes);
        if(closeBoxes == 1)
        {
            Debug.Log("Box 0: " + boxData[0].c0 + " " + boxData[0].c1 + " " + boxData[0].c2);
        }
    }

    public void LoadNewParticle(int count, float3[] newPositions, float3[] newVelocities)
    {
        float3[] positions = new float3[maxParticles];
        float3[] velocities = new float3[maxParticles];
        float[] density = new float[maxParticles];
        float3[] predictedPositions = new float3[maxParticles];

        positionBuffer.GetData(positions);
        velocityBuffer.GetData(velocities);
        densityBuffer.GetData(density);

        for (int i = 0; i < count; i++)
        {
            positions[oldestPatrticle] = newPositions[i];
            velocities[oldestPatrticle] = newVelocities[i];
            predictedPositions[oldestPatrticle] = newPositions[i] + newVelocities[i];
            density[oldestPatrticle] = 1;

            oldestPatrticle++;
            if (oldestPatrticle >= numParticles)
                oldestPatrticle = 0;
        }

        positionBuffer.SetData(positions);
        velocityBuffer.SetData(velocities);
        densityBuffer.SetData(density);
        predictedPositionsBuffer.SetData(predictedPositions);
    }

    public void SpawnParticles(int numParticles)
    {
        this.numParticles = numParticles;

        Debug.Log("Spawning " + numParticles + " particles");

        Vector3 lower = display.LowerBoundary()/2;
        Vector3 upper = display.UpperBoundary()/2;

        float3[] positions = new float3[maxParticles];
        float3[] velocities = new float3[maxParticles];
        float[] density = new float[maxParticles];

        int countPerSide = (int)Mathf.Pow(maxParticles, 1f / 3f) + 1;
        float stepX = (upper.x - lower.x) / countPerSide;
        float stepY = (upper.y - lower.y) / countPerSide;
        float stepZ = (upper.z - lower.z) / countPerSide;


        for (int i = 0; i < numParticles; i++)
        {
            positions[i] = new Vector3(
                lower.x + stepX * (i % countPerSide),
                lower.y + stepY * ((i / countPerSide) % countPerSide),
                lower.z + stepZ * (i / (countPerSide * countPerSide))
                );
            velocities[i] = 0;
            density[i] = 1;
        }
        for (int i = numParticles; i < maxParticles; i++)
        {
            positions[i] = spawnNewParticlesTransform.position - transform.position;
            velocities[i] = 0;
            density[i] = 0;
        }

        positionBuffer.SetData(positions);
        velocityBuffer.SetData(velocities);
        predictedPositionsBuffer.SetData(positions);
        densityBuffer.SetData(density);

        display.positions = new Vector3[maxParticles];
        display.density = new float[maxParticles];
    }

    void OnDestroy()
    {
        //release all buffers
        positionBuffer.Release();
        velocityBuffer.Release();
        densityBuffer.Release();
        predictedPositionsBuffer.Release();
        externalSpheres.Release();
        externalBoxes.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smoothingRadius);
        if(spawnNewParticlesTransform == null)
                        return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnNewParticlesTransform.position, spawnRadius);
    }
}
