using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingRopeHandler : MonoBehaviour
{
    private bool shouldThrow => Input.GetKeyDown(throwKey);

    [Header("References")] 
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject climbingRopePrefab;
    [SerializeField] private LayerMask playerLayer;

    [Header("Throwing")] 
    [SerializeField] private bool canThrow = true;
    [SerializeField] private float throwForce;
    [SerializeField] private float throwUpwardsForce;
    [SerializeField] private KeyCode throwKey = KeyCode.Mouse0;
    private bool readyToThrow;
    
    [Header("Settings")] 
    [SerializeField] private float throwCooldown;

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

        GameObject projectile = Instantiate(climbingRopePrefab, attackPoint.position, playerCamera.rotation);
        Vector3 forceDirection;

        // get rigidbody of projectile
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        // Calculate direction
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 500f, playerLayer))
        {
            Debug.Log(hit.point);
            forceDirection = (hit.point - attackPoint.position).normalized;
        }
        else forceDirection = playerCamera.transform.forward;

        //Add force to projectile
        Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardsForce;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);
        
        Invoke(nameof(ResetThrow), throwCooldown);
    }
    
    private void ResetThrow()
    {
        readyToThrow = true;
    }
}






