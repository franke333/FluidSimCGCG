using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSphere : MonoBehaviour
{
    public float yTop = 5.0f;
    public float yBottom = 0.0f;
    public float speed = 1.0f;
    
    bool movingTop = true;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.L))
        {
            movingTop = false;
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            movingTop = true;
        }

        transform.position += new Vector3(0, movingTop ? speed : -speed, 0) * Time.deltaTime;


        transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, yBottom, yTop), transform.position.z);
    }
}
