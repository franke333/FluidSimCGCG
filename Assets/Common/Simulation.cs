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

    // Buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }

    //public ComputeBuffer predictedPositionsBuffer;


    // Kernels (methods in the pragmas at top of compute shader)
    const int updatePositionKernel = 0;
    const int resolveCollisionsKernel = 1;
    const int calculateDensityKernel = 2;
    const int calculatePressureForceKernel = 3;
    //const int predictPositionKernel = 4;

    private void Start()
    {
        Debug.Log("Simulation Start");

        //create buffers here
        positionBuffer = ComputeHelper.CreateBuffer<Vector3>(numParticles);
        velocityBuffer = ComputeHelper.CreateBuffer<Vector3>(numParticles);
        densityBuffer = ComputeHelper.CreateBuffer<float>(numParticles);
        //predictedPositionsBuffer = ComputeHelper.CreateBuffer<float3>(numParticles);


        // tell each compute shader method (called kernel) which buffers will be used
        ComputeHelper.SetBuffer(compute, "Positions", positionBuffer, updatePositionKernel, resolveCollisionsKernel, calculateDensityKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "Velocities", velocityBuffer, updatePositionKernel, resolveCollisionsKernel, calculatePressureForceKernel);
        ComputeHelper.SetBuffer(compute, "Densities", densityBuffer, calculateDensityKernel, calculatePressureForceKernel);
        //ComputeHelper.SetBuffer(compute, "PredictedPositions", predictedPositionsBuffer, calculateDensityKernel, calculatePressureForceKernel, updatePositionKernel);

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
            display.UpperBoundary().y- display.scale,
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
    }

    //do sim here
    private void FixedUpdate()
    {
        ComputeHelper.Dispatch(compute, numParticles, updatePositionKernel);
        ComputeHelper.Dispatch(compute, numParticles, resolveCollisionsKernel);
        ComputeHelper.Dispatch(compute, numParticles, calculateDensityKernel);
        ComputeHelper.Dispatch(compute, numParticles, calculatePressureForceKernel);
        //get positions from buffer
        positionBuffer.GetData(display.positions);
    }

    public void SpawnParticles(int numParticles)
    {
        this.numParticles = numParticles;

        Debug.Log("Spawning " + numParticles + " particles");

        Vector3 lower = display.LowerBoundary();
        Vector3 upper = display.UpperBoundary();

        float3[] positions = new float3[numParticles];
        float3[] velocities = new float3[numParticles];

        for (int i = 0; i < numParticles; i++)
        {
            positions[i] = new Vector3(Random.Range(lower.x, upper.x), Random.Range(lower.y, upper.y), Random.Range(lower.z, upper.z));
            //velocities[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 0.25f;
        }

        positionBuffer.SetData(positions);
        velocityBuffer.SetData(velocities);

        display.positions = new Vector3[numParticles];
    }

    void OnDestroy()
    {
        //release all buffers
        positionBuffer.Release();
        velocityBuffer.Release();
        densityBuffer.Release();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smoothingRadius);
    }
}
