using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float angle=0f;
    [Range(1.5f,6.5f)]
    public float zoom = 4f;

    // Update is called once per frame
    void Update()
    {
        float radius = zoom * 2f;
        transform.position = target.position + new Vector3(radius * Mathf.Cos(angle), 1 / 0.577350269f * radius, radius * Mathf.Sin(angle));  
        transform.LookAt(target.position);
        if (Camera.main.orthographic)
        {
            Camera.main.orthographicSize = zoom;
        }
    }
}
