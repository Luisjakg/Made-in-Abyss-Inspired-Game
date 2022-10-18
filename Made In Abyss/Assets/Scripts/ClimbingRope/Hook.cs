using System;
using System.Collections;
using System.Collections.Generic;
using Obi;
using Unity.VisualScripting;
using UnityEngine;

public class Hook : MonoBehaviour
{
    private Rigidbody rb;
    private bool targetHit = false;
    private Quaternion desiredRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (targetHit) return;
        
        targetHit = true;

        rb.isKinematic = true;

        transform.SetParent(other.transform);
    }

    public bool GetTargetHit()
    {
        return targetHit;
    }

    public float GetMoveSpeed()
    {
        return rb.velocity.magnitude;
    }
}