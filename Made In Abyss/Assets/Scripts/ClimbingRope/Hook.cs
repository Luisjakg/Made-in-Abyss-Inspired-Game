using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hook : MonoBehaviour
{
    private Rigidbody rb;
    private bool targetHit;
    private Quaternion desiredRotation;
    private Vector3 desiredScale;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (targetHit)
            return;
        else
            targetHit = true;

        // make sure projectile stick to surface
        rb.isKinematic = true;
        
        //make sure projectile moves with target

        transform.SetParent(other.transform);
    }
}
