using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DisplayParticles : MonoBehaviour
{
    public Vector3[] positions;

    //scale of the particles
    public float scale = 1f;

    const int BATCH_SIZE = 1023;

    public Vector3 LowerBoundary() => -transform.localScale;
    public Vector3 UpperBoundary() => transform.localScale;

    public Mesh mesh;
    public Material material;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //TODO dont use LINQ lol... too slow
        for (int i = 0; i <= positions.Length / BATCH_SIZE; i++)
            Graphics.DrawMeshInstanced(mesh, 0, material, positions.Skip(BATCH_SIZE * i).Take(BATCH_SIZE).Select(p => Matrix4x4.TRS(p + transform.position, Quaternion.identity, Vector3.one * scale)).ToArray());

    }

    //Boundary visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale * 2);
    }
}
