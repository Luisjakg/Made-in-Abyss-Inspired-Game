using System;
using System.Collections;
using System.Collections.Generic;
using MIA.ClimbingRope;
using MIA.PlayerControl;
using UnityEngine;

namespace MIA.ClimbingRope
{
    public class ClimbingRopeShooter : MonoBehaviour
    {
        private bool readyRope => Input.GetKeyDown(throwKey);
        private bool throwHook => Input.GetKeyUp(throwKey);

        [Header("References")] 
        [SerializeField] private Transform playerCamera;
        [SerializeField] private PlayerMovementController playerMovementController;
        [SerializeField] private Transform attackPoint;
        [SerializeField] private GameObject climbingRopePrefab;
        [SerializeField] private GameObject hookPrefab;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private Transform ropeSpawnPosition;
        [SerializeField] private Transform hookSpawnPosition;
        private GameObject climbingRope;
        private GameObject hook;
        private bool hookReady;

        private GameObject player;

        [Header("Throwing")] 
        [SerializeField] private bool canThrow = true;
        [SerializeField] private float throwForce;
        [SerializeField] private float throwUpwardsForce;
        [SerializeField] private KeyCode throwKey = KeyCode.R;
        private bool readyToThrow;

        [Header("Settings")] 
        [SerializeField] private float throwCooldown;

        private void Awake()
        {
            readyToThrow = true;
        }

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        private void Update()
        {
            if (canThrow) HandleThrow();
        }

        private void HandleThrow()
        {
            if (!readyToThrow) return;

            if (readyRope) InstantiateRope();

            if (throwHook && hookReady) ThrowHook();
        }

        private void ThrowHook()
        {
            hook.transform.parent = null;
            
            Vector3 forceDirection;
            Rigidbody hookRb = hook.GetComponent<Rigidbody>();
            // Calculate direction
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit,
                500f, playerLayer))
                forceDirection = (hit.point - attackPoint.position).normalized;
            else forceDirection = playerCamera.transform.forward;

            hookRb.isKinematic = false;
            //Add force to projectile
            Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardsForce;
            //Add player current velocity to projectile
            forceToAdd += playerMovementController.getVelocity();
            hookRb.AddForce(forceToAdd, ForceMode.Impulse);
            
            Invoke(nameof(ResetThrow), throwCooldown);
        }

        private void InstantiateRope()
        {
            if (hookReady)
            {
                Destroy(hook);
                Destroy(climbingRope);
                hookReady = false;
                return;
            }
            
            //TODO THERE MIGHT BE A RACE CONDITION HERE
            
            hook = Instantiate(hookPrefab, hookSpawnPosition.position, Quaternion.identity);
            //climbingRope = Instantiate(climbingRopePrefab, ropeSpawnPosition.position, Quaternion.identity);


            hook.transform.parent = hookSpawnPosition;

            hookReady = true;
        }

        private void ResetThrow()
        {
            readyToThrow = true;
        }

        public GameObject GetHook()
        {
            return hook;
        }
    }
}

