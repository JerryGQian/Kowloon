using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour { 

    public GameObject player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    private Transform transform;
    private Transform playerTransform;

    // Start is called before the first frame update
    void Start() {
        transform = GetComponent<Transform>();
        playerTransform = player.GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate() {
        Vector3 desiredPosition = playerTransform.position  + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

    }
}
