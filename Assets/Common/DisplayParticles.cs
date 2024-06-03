using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DisplayParticles : MonoBehaviour
{
    public Vector3[] positions;
    public float[] density;

    //scale of the particles
    public float scale = 1f;

    const int BATCH_SIZE = 1023;

    public Vector3 LowerBoundary() => -transform.localScale;
    public Vector3 UpperBoundary() => transform.localScale;

    public Mesh mesh;
    public Material material;

    public Gradient gradient;

    public float targetDensity = 1f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //TODO dont use LINQ lol... too slow
        if(positions.Length > 250)
            for (int i = 0; i <= positions.Length / BATCH_SIZE; i++)
                Graphics.DrawMeshInstanced(mesh, 0, material, positions.Skip(BATCH_SIZE * i).Take(BATCH_SIZE).Select(p => Matrix4x4.TRS(p + transform.position, Quaternion.identity, Vector3.one * scale)).ToArray());
        else
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            for (int i = 0; i < positions.Length; i++)
            {
                float v = (density[i] - targetDensity)/5 + 0.5f;
                Color color = gradient.Evaluate(v);
                color.a= 0.5f;
                props.SetColor("_BaseColor", color);
                Matrix4x4 matrix = Matrix4x4.TRS(positions[i] + transform.position, Quaternion.identity, Vector3.one * scale);
                
                Graphics.DrawMesh(mesh, matrix, material, 0,null,0,props);
            }
        }
    }

    //Boundary visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale * 2);
    }
}
