using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerController : MonoBehaviour {

    public Animator anim;

    protected FixedJoystick joystick;

    private Rigidbody rb;
    private Transform transform;
    private float lastAngle = 90f;

    // Start is called before the first frame update
    void Start() {
        anim = GetComponent<Animator>();

        joystick = FindObjectOfType<FixedJoystick>();

        rb = GetComponent<Rigidbody>();
        transform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update() {

        rb.velocity = new Vector3(joystick.Horizontal * 5f * 5, 0, joystick.Vertical * 5f  *5); //rb.velocity.y
        anim.SetFloat("movementSpeed", rb.velocity.magnitude);

        float angle = lastAngle;
        if (rb.velocity.magnitude > 0.1f) {
            Vector3 dir = GetComponent<Rigidbody>().velocity;
            angle = -1 * Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg + 90;
            lastAngle = angle;
        }

        transform.eulerAngles = new Vector3(0, angle, 0);




        if (Input.GetKey("space")) {
            //anim.SetBool("isRunning", true);
        }
        else {
            //anim.SetBool("isRunning", false);
        }
    }
}
