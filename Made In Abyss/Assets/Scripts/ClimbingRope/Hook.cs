using System;
using System.Collections;
using System.Collections.Generic;
using Obi;
using Unity.VisualScripting;
using UnityEngine;

namespace MIA.ClimbingRope
{
    public class Hook : MonoBehaviour
    {
        private Rigidbody rb;
        private bool isTargetHit;
        private Quaternion desiredRotation;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            isTargetHit = false;
        }

        private void OnCollisionEnter(Collision other)
        {
            if (isTargetHit) return;
        
            isTargetHit = true;

            rb.isKinematic = true;

            transform.SetParent(other.transform);
        }

        public bool GetIsTargetHit()
        {
            return isTargetHit;
        }

        public float GetMoveSpeed()
        {
            return rb.velocity.magnitude;
        }
    }
}