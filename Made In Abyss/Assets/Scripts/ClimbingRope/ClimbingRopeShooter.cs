using System;
using System.Collections;
using System.Collections.Generic;
using MIA.ClimbingRope;
using UnityEngine;

namespace MIA.ClimbingRope
{
    public class ClimbingRopeShooter : MonoBehaviour
    {
        private bool shouldThrow => Input.GetKeyDown(throwKey);

        [Header("References")] [SerializeField]
        private Transform playerCamera;

        [SerializeField] private Transform attackPoint;
        [SerializeField] private GameObject climbingRopePrefab;
        [SerializeField] private LayerMask playerLayer;
        private GameObject climbingRope;
        private GameObject hook;
        private ClimbingRopeHandler climbingRopeHandler;

        [Header("Throwing")] [SerializeField] private bool canThrow = true;
        [SerializeField] private float throwForce;
        [SerializeField] private float throwUpwardsForce;
        [SerializeField] private KeyCode throwKey = KeyCode.R;
        private bool readyToThrow;

        [Header("Settings")] [SerializeField] private float throwCooldown;

        private void Awake()
        {
            readyToThrow = true;
        }

        private void Update()
        {
            if (canThrow) HandleThrow();
        }

        private void HandleThrow()
        {
            if (!shouldThrow) return;
            if (!readyToThrow) return;
            if (climbingRope != null)
            {
                Destroy(climbingRope);
                Destroy(hook);
                return;
            }

            climbingRope = Instantiate(climbingRopePrefab, attackPoint.position, Quaternion.identity);
            climbingRopeHandler = climbingRope.GetComponent<ClimbingRopeHandler>();
            Vector3 forceDirection;

            // get hook of rope
            hook = climbingRopeHandler.GetHook();
            Rigidbody hookRb = hook.GetComponent<Rigidbody>();

            // Calculate direction
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit,
                500f, playerLayer))
            {
                Debug.Log(hit.point);
                forceDirection = (hit.point - attackPoint.position).normalized;
            }
            else forceDirection = playerCamera.transform.forward;

            //Add force to projectile
            Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardsForce;
            hookRb.AddForce(forceToAdd, ForceMode.Impulse);

            Invoke(nameof(ResetThrow), throwCooldown);
        }

        private void ResetThrow()
        {
            readyToThrow = true;
        }
    }
}

