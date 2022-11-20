using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mover))]
public class HostileCreatureController : MonoBehaviour
{
    [Header("FOV Settings")]   
    [SerializeField] private float radius = 10f;
    [SerializeField, Range(0, 360)] private float angle = 90f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private bool canSeePlayer;
    public Action<bool> OnCanSeePlayerChanged;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float huntMoveSpeed = 10f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private GameObject player;
    private Mover mover;

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
        if (canSeePlayer) HuntPlayer();
    }

    private void HuntPlayer()
    {
        mover.MoveTo(player.transform.position, huntMoveSpeed);
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
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                    canSeePlayer = true;
                else
                    canSeePlayer = false;
            }
            else 
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }
    
    private void ApplyDamage(float damage)
    {
        Debug.Log("Creature took " + damage + " damage");
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        Vector3 fovLine1 = Quaternion.AngleAxis(angle / 2, transform.up) * transform.forward * radius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-angle / 2, transform.up) * transform.forward * radius;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        if (canSeePlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, (player.transform.position - transform.position).normalized * radius);
        }
    }
}


