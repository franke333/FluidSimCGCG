using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FluidScript : MonoBehaviour
{
    Vector3[] positions;
    Vector3[] velocities;
    [SerializeField]
    int count = 1000;

    //scale of the particles
    public float scale = 1f;

    const int BATCH_SIZE = 1023;

    public Mesh mesh;
    public Material material;


    //Boundary of the box set by the scale of transform
    private Vector3 LowerBoundary() => -transform.localScale;
    private Vector3 UpperBoundary() => transform.localScale;

    //Bounce particles off the walls
    private void BounceParticles()
    {
        Vector3 lower = LowerBoundary();
        Vector3 upper = UpperBoundary();
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = positions[i];
            Vector3 vel = velocities[i];
            for (int j = 0; j < 3; j++)
            {
                if (pos[j] < lower[j] || pos[j] > upper[j])
                {
                    pos[j] = Mathf.Clamp(pos[j], lower[j], upper[j]);
                    vel[j] *= -1;
                }
            }
            velocities[i] = vel;
            positions[i] = pos;
        }
    }


    private void Simulate()
    {
        for (int i = 0; i < count; i++)
        {
            positions[i] += velocities[i] * Time.deltaTime;
        }
    }

    //Spawn particles in the box
    public void SpawnParticles(int count)
    {
        this.count = count;
        positions = new Vector3[count];
        velocities = new Vector3[count];

        Debug.Log("Spawning " + count + " particles");

        Vector3 lower = LowerBoundary();
        Vector3 upper = UpperBoundary();
        for (int i = 0; i < count; i++)
        {

            positions[i] = new Vector3(Random.Range(lower.x, upper.x), Random.Range(lower.y, upper.y), Random.Range(lower.z, upper.z));
            //random unit vector
            velocities[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
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
        // react to change in editor
        if (count != positions.Length)
            SpawnParticles(count);

        //some random physics
        Simulate();
        BounceParticles();

        //TODO dont use LINQ lol... too slow
        for (int i = 0; i <= count/BATCH_SIZE; i++)
            Graphics.DrawMeshInstanced(mesh, 0, material, positions.Skip(BATCH_SIZE*i).Take(BATCH_SIZE).Select(p => Matrix4x4.TRS(p + transform.position,Quaternion.identity,Vector3.one * scale)).ToArray());
        
    }

    //Boundary visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale * 2);
    }
}
