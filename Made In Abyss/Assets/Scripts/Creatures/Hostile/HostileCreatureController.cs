using System;
using System.Collections;
using System.Collections.Generic;
using MIA.PlayerControl;
using UnityEngine;
using UnityEngine.Rendering.UI;

[RequireComponent(typeof(Mover))]
public class HostileCreatureController : MonoBehaviour
{
    [Header("FOV Settings")]   
    [SerializeField] private float maxRadius = 10f;
    [SerializeField, Range(0, 360)] private float angle = 90f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private bool canSeeTarget;

    [Header("Player State Range Settings")]
    [SerializeField, Range(0,100)] private float walkingRange = 80f;
    [SerializeField, Range(0,100)] private float sprintingRange = 100f;
    [SerializeField, Range(0,100)] private float crouchingRange = 50f;
    [SerializeField, Range(0,100)] private float slidingRange = 50f;
    [SerializeField, Range(0,100)] private float idleRange = 80f; 

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float huntMoveSpeed = 10f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private GameObject player;
    private Mover mover;
    private Transform target;

    private void Awake()
    {
        currentHealth = maxHealth;
        mover = GetComponent<Mover>();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVroutine());
    }

    private void Update()
    {
        if (canSeeTarget) HuntTarget();
    }

    private void HuntTarget()
    {
        if (target.CompareTag("Player"))
            mover.MoveTo(player.transform.position, huntMoveSpeed);
        else
            mover.MoveTo(target.position, huntMoveSpeed);
    }


    private IEnumerator FOVroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        float range = maxRadius;
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, maxRadius, targetMask);

        if (rangeChecks.Length != 0)
        {
            target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (target.CompareTag("Player"))
                    range = CalculateToPlayerStates(range);

                if (distanceToTarget <= range)
                {
                    if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                        canSeeTarget = true;
                    else
                        canSeeTarget = false;
                }
                else
                    canSeeTarget = false;
            }
            else 
                canSeeTarget = false;
        }
        else if (canSeeTarget)
            canSeeTarget = false;
    }

    private float CalculateToPlayerStates(float range)
    {
        PlayerController.PlayerState playerState = player.GetComponent<PlayerController>().GetPlayerState();

        switch (playerState)
        {
            case PlayerController.PlayerState.Walking:
                return range * walkingRange / 100;
            case PlayerController.PlayerState.Sprinting:
                return range * sprintingRange / 100;
            case PlayerController.PlayerState.Crouching:
                return range * crouchingRange / 100;
            case PlayerController.PlayerState.Sliding:
                return range * slidingRange / 100;
            case PlayerController.PlayerState.Idle:
                return range * idleRange / 100;
            default:
                Debug.Log("Player State not found");
                return range;
        }
    }
    
    private void ApplyDamage(float damage)
    {
        if (currentHealth - damage <= 0)
            currentHealth = 0;
        else 
            currentHealth -= damage;

        if (currentHealth <= 0)
            KillCreature();
    }

    private void KillCreature()
    {
        throw new NotImplementedException();
    }
    
    public bool HasTarget()
    {
        return canSeeTarget;
    }

    // Fov Gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
        
        Vector3 fovLine1 = Quaternion.AngleAxis(angle / 2, transform.up) * transform.forward * maxRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-angle / 2, transform.up) * transform.forward * maxRadius;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (canSeeTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, (player.transform.position - transform.position).normalized * maxRadius);
        }
    }
}


