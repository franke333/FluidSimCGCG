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

    public int numParticles;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Matrix4x4[] matrices = new Matrix4x4[BATCH_SIZE];
        if (numParticles > 1)
            for (int i = 0; i <= numParticles / BATCH_SIZE; i++)
            {
                for(int j = 0; j < BATCH_SIZE; j++)
                {
                    if (j + BATCH_SIZE * i >= numParticles)
                        break;
                    //look at the camera
                    Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.position - (positions[j] + transform.position), Vector3.up);
                    //rotate so up is forward
                    //rotation *= Quaternion.Euler(90, 0, 0);
                    matrices[j] = Matrix4x4.TRS(positions[j + BATCH_SIZE * i] + transform.position, rotation, Vector3.one * scale);
                }
                Graphics.DrawMeshInstanced(mesh, 0, material, matrices, Mathf.Min(BATCH_SIZE, numParticles - BATCH_SIZE*i));
            }
        else
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            for (int i = 0; i < numParticles; i++)
            {
                float v = (density[i] - targetDensity) / 5 + 0.5f;
                Color color = gradient.Evaluate(v);
                color.a = 0.5f;
                props.SetColor("_BaseColor", color);
                Matrix4x4 matrix = Matrix4x4.TRS(positions[i] + transform.position, Quaternion.identity, Vector3.one * scale);

                Graphics.DrawMesh(mesh, matrix, material, 0, null, 0, props);
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
