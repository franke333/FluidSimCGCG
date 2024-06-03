using System.Collections;
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

    private int closeSpheres = 0;
    private Vector4[] sphereData;
    

    // Buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }

    public ComputeBuffer predictedPositionsBuffer { get; private set; }

    public ComputeBuffer externalSpheres { get; private set; }


    // Kernels (methods in the pragmas at top of compute shader)
    const int updatePositionKernel = 0;
    const int calculateDensityKernel = 1;
    const int calculatePressureForceKernel = 2;
    const int externalForcesKernel = 3;

    private void Start()
    {
        Debug.Log("Simulation Start");

        //create buffers here
        positionBuffer = ComputeHelper.CreateBuffer<Vector3>(numParticles);
        velocityBuffer = ComputeHelper.CreateBuffer<Vector3>(numParticles);
        densityBuffer = ComputeHelper.CreateBuffer<float>(numParticles);
        predictedPositionsBuffer = ComputeHelper.CreateBuffer<float3>(numParticles);
        externalSpheres = ComputeHelper.CreateBuffer<float4>(sphereObstacles.Length);
        sphereData = new Vector4[sphereObstacles.Length];


        // tell each compute shader method (called kernel) which buffers will be used
        ComputeHelper.SetBuffer(compute, "Positions", positionBuffer, updatePositionKernel, calculateDensityKernel, calculatePressureForceKernel, externalForcesKernel);
        ComputeHelper.SetBuffer(compute, "Velocities", velocityBuffer, updatePositionKernel, calculatePressureForceKernel, externalForcesKernel);
        ComputeHelper.SetBuffer(compute, "Densities", densityBuffer, calculateDensityKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "PredictedPositions", predictedPositionsBuffer, externalForcesKernel, updatePositionKernel, calculateDensityKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "SphereObstacles", externalSpheres, externalForcesKernel);

        SpawnParticles(numParticles);
        //const deltatime
        Time.fixedDeltaTime = 1 / 60f;

        SetComputeVariables();
    }

    public void SetComputeVariables()
    {
        // set all the variables in the compute shader
        // this is done once at the start
        // to see changes, you need to restart the simulation
        compute.SetFloats("boundsSize", new float[]
        {
            display.UpperBoundary().x - display.scale,
            display.UpperBoundary().y - display.scale,
            display.UpperBoundary().z - display.scale
        });
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
        externalSpheres.SetData(sphereData);
        compute.SetInt("sphereCount", closeSpheres);
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

        float minDistance = Mathf.Min(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        closeSpheres = 0;
        foreach (var sphere in sphereObstacles)
        {
            
            float distance = Vector3.Distance(sphere.transform.position, transform.position) - sphere.transform.localScale.x - minDistance;
            if (distance > 0)
                break;
            sphereData[closeSpheres] = new Vector4(sphere.transform.position.x, sphere.transform.position.y, sphere.transform.position.z, sphere.transform.localScale.x/2);
            closeSpheres++;
        }
    }

    public void SpawnParticles(int numParticles)
    {
        this.numParticles = numParticles;

        Debug.Log("Spawning " + numParticles + " particles");

        Vector3 lower = display.LowerBoundary()/2;
        Vector3 upper = display.UpperBoundary()/2;

        float3[] positions = new float3[numParticles];
        float3[] velocities = new float3[numParticles];
        float[] density = new float[numParticles];

        int countPerSide = (int)Mathf.Pow(numParticles, 1f / 3f) + 1;
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

        positionBuffer.SetData(positions);
        velocityBuffer.SetData(velocities);
        predictedPositionsBuffer.SetData(positions);
        densityBuffer.SetData(density);

        display.positions = new Vector3[numParticles];
        display.density = new float[numParticles];
    }

    void OnDestroy()
    {
        //release all buffers
        positionBuffer.Release();
        velocityBuffer.Release();
        densityBuffer.Release();
        predictedPositionsBuffer.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smoothingRadius);
    }
}
