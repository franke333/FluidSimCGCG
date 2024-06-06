using UnityEngine;

//The class OrbitingParticle is derived from MonoBehaviour and controls the behavior of the orbiting particle in the scene.
public class OrbitingStars : MonoBehaviour
{
    public int particleCount = 17; //number of particles to be generated
    public ComputeShader shader; //holds a reference to the compute shader used to calculate the positions of the particles.

    public GameObject prefab; //refers to the prefab GameObject that will be instantiated for each particle.


    //are used to manage the compute shader execution, including identifying
    ///the kernel function and calculating the number of thread groups needed based on particleCount.
    int kernelHandle;
    uint threadGroupSizeX;
    int groupSizeX;

    Transform[] particles; //the transforms of the instantiated star prefabs
    ComputeBuffer resultBuffer; //is a buffer to communicate between the compute shader and the C# script, specifically for receiving the calculated positions
    Vector3[] output; //an array used to store the positions fetched from resultBuffer and apply them to the star prefabs


    //This method is called on script initialization.
    //It sets up the compute shader (finding the kernel,
    //setting up the thread group sizes, and initializing
    //the compute buffer). It then instantiates the particle and stores their transforms.
    void Start()
    {
        kernelHandle = shader.FindKernel("OrbitingStars");
        shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((particleCount + threadGroupSizeX - 1) / threadGroupSizeX);

        //we need to define some GPU memory to store the data
        resultBuffer = new ComputeBuffer(particleCount, sizeof(float) * 3);
        shader.SetBuffer(kernelHandle, "Result", resultBuffer);
        output = new Vector3[particleCount];

        particles = new Transform[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = Instantiate(prefab, transform).transform;
        }
    }


    // Called every frame, it updates the time variable
    // in the shader, dispatches the compute shader to
    // calculate new positions for the particles, retrieves the
    // calculated positions from the GPU, and applies these positions to the particle prefabs.
    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        //give data to gpu for positioning particles
        resultBuffer.GetData(output);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].localPosition = output[i];
        }
    }

    //to avoid leak of memory and to free up GPU resources , just dispose buffer from not needed information in the end
    private void OnDestroy()
    {
        resultBuffer.Dispose();
    }
}


/*
How It Works
Initialization (Start Method):
Finds the compute shader kernel named OrbitingParticle.
Calculates the size of the thread group and the number of groups needed to process all particles, 
ensuring that every particle is accounted for even if particleCount isn't a perfect multiple of threadGroupSizeX.
Allocates a ComputeBuffer (resultBuffer) to store the output positions of the particles 
calculated by the compute shader. The buffer size is based on the number of particles 
and the size of a Vector3 (since each position is a 3D vector).
Instantiates the particle prefabs and stores their transforms for later use.

Per-Frame Update (Update Method):
Sets the current time in the shader, allowing for dynamic animation based on the elapsed time (Time.time).
Dispatches the compute shader with the calculated number of thread groups. This operation executes the shader, calculating the new positions for all particles.
Retrieves the calculated positions from the resultBuffer and applies these positions to the particle prefabs, effectively moving them in the scene.
*/