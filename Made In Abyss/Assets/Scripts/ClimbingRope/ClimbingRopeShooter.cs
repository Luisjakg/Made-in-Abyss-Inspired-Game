using System;
using System.Collections;
using System.Collections.Generic;
using MIA.ClimbingRope;
using MIA.PlayerControl;
using Obi;
using UnityEngine;

namespace MIA.ClimbingRope
{
    public class ClimbingRopeShooter : MonoBehaviour
    {
        private bool ShouldReadyThrow => Input.GetKeyDown(throwKey);
        private bool shouldThrow => Input.GetKeyUp(throwKey);

        [Header("Functional Options")] 
        [SerializeField] private bool canThrow = true;
        
        [Header("Controls")]
        [SerializeField] private KeyCode throwKey = KeyCode.R;
        
        [Header("References")] 
        [SerializeField] private Transform playerCamera;
        [SerializeField] private Transform hookSpawnPosition;
        [SerializeField]private PlayerController playerMovementController;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private GameObject climbingRopePrefab;
        [SerializeField] private LayerMask playerLayer;
        private GameObject climbingRope;
        private GameObject hook;
        private ClimbingRopeHandler climbingRopeHandler;
        private bool hookReady;
        private GameObject core;

        [Header("Throwing")]
        [SerializeField] private float throwForce;
        [SerializeField] private float throwUpwardsForce;
        [SerializeField] private float throwCooldown;
        private bool readyToThrow;
        
        private void Awake()
        {
            readyToThrow = true;
        }

        private void Start()
        {
            core = GameObject.FindGameObjectWithTag("Core");
        }

        private void Update()
        {
            if (canThrow) HandleThrow();
        }

        private void HandleThrow()
        {
            
            if (!readyToThrow) return;

            if (ShouldReadyThrow) ReadyThrow();

            if (shouldThrow && hookReady) ThrowHook();
        }

        private void ReadyThrow()
        {
            if (!readyToThrow) return;
            if (climbingRope != null)
            {
                Destroy(climbingRope);
                Destroy(hook);
                hookReady = false;
                return;
            }

            climbingRope = Instantiate(climbingRopePrefab, attackPoint.position, Quaternion.identity);
            climbingRopeHandler = climbingRope.GetComponent<ClimbingRopeHandler>();
            climbingRopeHandler.Visible(false);

            // get hook of rope
            hook = climbingRopeHandler.GetHook();

            hook.transform.parent = hookSpawnPosition;
            hook.transform.position = hookSpawnPosition.position;

            hookReady = true;
        }

        private void ThrowHook()
        {
            climbingRopeHandler.Visible(true);
            hook.transform.parent = core.transform;
            
            Rigidbody hookRb = hook.GetComponent<Rigidbody>();
            Vector3 forceDirection;
            
            // Calculate direction
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit,
                500f, playerLayer))
                forceDirection = (hit.point - attackPoint.position).normalized;
            else forceDirection = playerCamera.transform.forward;


            hookRb.isKinematic = false;
            //Add force to projectile
            Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardsForce;
            //Add player current velocity to projectile
            forceToAdd += playerMovementController.GetVelocity();
            hookRb.AddForce(forceToAdd, ForceMode.Impulse);

            Invoke(nameof(ResetThrow), throwCooldown);
        }

        private void ResetThrow()
        {
            readyToThrow = true;
        }
    }
}

