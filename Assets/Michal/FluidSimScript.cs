using System.Linq;
using UnityEngine;

public class FluidSimScript : MonoBehaviour
{
    [SerializeField]
    int count = 1000;

    //scale of the particles
    public float scale = 1f;

    public Vector3 boundsSize;
    public float gravity;
    public float smoothingRadius;
    public float targetDensity;
    public float pressureMultiplier;
    public float mass;

    float[] particleProperties;
    float[] densities;
    Vector3[] positions;
    Vector3[] velocities;

    const int BATCH_SIZE = 1023;

    public Mesh mesh;
    public Material material;

    private void Simulate(float deltaTime, Vector3 getRandomDir)
    {
        // Apply gravity and calculate densities
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            velocities[i] += Vector3.down * gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        });

        // Calculate and apply pressure forces
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            Vector3 pressureForce = CalculatePressureForce(i, getRandomDir);
            // F = m * a, so: a = F / m
            Vector3 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * deltaTime;
        });

        // Update positions and resolve collisions
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            positions[i] += velocities[i] * deltaTime;
            ResolveCollisions(ref positions[i], ref velocities[i]);
        });
    }

    //Spawn particles in the box
    void SpawnParticles(int count)
    {
        this.count = count;
        positions = new Vector3[count];
        velocities = new Vector3[count];
        densities = new float[count];
        particleProperties = new float[count];

        Debug.Log("Spawning " + count + " particles");

        for (int i = 0; i < count; i++)
        {
            float x = (float)(Random.value - 0.5) * boundsSize.x;
            float y = (float)(Random.value - 0.5) * boundsSize.y;
            float z = (float)(Random.value - 0.5) * boundsSize.z;
            positions[i] = new Vector3(x, y, z);
            particleProperties[i] = CalculateProperty(positions[i]);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SpawnParticles(count);
    }

    // Update is called once per frame
    void Update()
    {
        Simulate(Time.deltaTime, Random.insideUnitSphere.normalized);

        //TODO dont use LINQ lol... too slow
        for (int i = 0; i <= count/BATCH_SIZE; i++)
            Graphics.DrawMeshInstanced(mesh, 0, material, positions.Skip(BATCH_SIZE*i).Take(BATCH_SIZE).Select(p => Matrix4x4.TRS(p + transform.position,Quaternion.identity,Vector3.one * scale)).ToArray());
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    Vector3 CalculatePressureForce(int particleIndex, Vector3 getRandomDir)
    {
        Vector3 pressureForce = Vector3.zero;

        for (int otherParticleIndex = 0; otherParticleIndex < count; otherParticleIndex++)
        {
            if (particleIndex == otherParticleIndex) continue;

            Vector3 offset = positions[otherParticleIndex] - positions[particleIndex];
            float dst = offset.magnitude;
            Vector3 dir = dst == 0 ? getRandomDir: offset / dst;

            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[otherParticleIndex];
            pressureForce += -ConvertDensityToPressure(density) * dir * slope * mass / density;
        }

        return pressureForce;
    }

    float CalculateProperty(Vector3 samplePoint)
    {
        float property = 0;

        for (int i = 0; i < count; i++)
        {
            float dst = (positions[i] - samplePoint).magnitude;
            float influence = SmoothingKernel(smoothingRadius, dst);
            float density = CalculateDensity(positions[i]);
            property += particleProperties[i] * mass / density * influence;
        }

        return property;
    }

    static float SmoothingKernel(float radius, float dst)
    {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4;
        float value = Mathf.Max(0, radius * radius - dst * dst);
        return value * value * value / volume;
    }

    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;
        float f = radius * radius - dst * dst;
        float scale = -24 / (Mathf.PI * Mathf.Pow(radius, 8));
        return scale * dst * f * f;
    }

    float CalculateDensity(Vector3 samplePoint)
    {
        float density = 0;

        foreach (Vector3 position in positions)
        {
            float dst = (position - samplePoint).magnitude;
            float influence = SmoothingKernel(smoothingRadius, dst);
            density += mass * influence;
        }

        return density;
    }


    // Bounce from walls, the walls can be changed by resizing them in the editor
    void ResolveCollisions(ref Vector3 position, ref Vector3 velocity)
    {
        Vector3 halfBoundSize = boundsSize / 2 - Vector3.one * scale;

        if (Mathf.Abs(position.x) > halfBoundSize.x)
        {
            position.x = halfBoundSize.x * Mathf.Sign(position.x);
            velocity.x *= -1;
        }
        if (Mathf.Abs(position.y) > halfBoundSize.y)
        {
            position.y = halfBoundSize.y * Mathf.Sign(position.y);
            velocity.y *= -1;
        }
        if (Mathf.Abs(position.z) > halfBoundSize.z)
        {
            position.z = halfBoundSize.z * Mathf.Sign(position.z);
            velocity.z *= -1;
        }
    }

    //Boundary visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, boundsSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smoothingRadius);
    }
}
