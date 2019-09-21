using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JitterCube : MonoBehaviour {

    public float energy = 0;
    private Rigidbody rb;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        rb.position =  new Vector3(
            transform.position.x, 
            Perlin.getPerlin(Time.time, transform.position.x, transform.position.z), 
            transform.position.z);
    }
}
