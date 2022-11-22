using System;
using System.Collections;
using System.Collections.Generic;
using MIA.CREATURES;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GetRoamPoint))]
[RequireComponent(typeof(Mover))]
public class RoamingBehavior : MonoBehaviour
{
    [SerializeField] private bool pointBasedRoaming = true; 
    [SerializeField] float radius = 5f;
    [SerializeField] GetRoamPoint roamPoint;
    [SerializeField] float roamSpeed = 1f;
    [SerializeField] float roamWaitTime = 1f;
    private Mover mover;
    private bool lookingForRoamPoint = false;

    private void Awake()
    {
        mover = GetComponent<Mover>();
    }

    private void Update()
    {
        HandleRoam();
    }

    private void HandleRoam()
    {
        if (mover.HasPath() || lookingForRoamPoint) return;

        StartCoroutine(Roam());
    }

    private IEnumerator Roam()
    {
        lookingForRoamPoint = true;
        
        yield return new WaitForSeconds(roamWaitTime);
        if (mover.HasPath())
        {
            lookingForRoamPoint = false;
            yield break; // if we have a path, we don't need to find a new one
        }
        
        Vector3 destination = transform.position;
        
        print("Looking for destination");
        while (Vector3.Distance(destination, transform.position) < .5f || destination == roamPoint.transform.position)
        {
            if (pointBasedRoaming)
                destination = roamPoint.GetRandomPoint(null);
            else
                destination = roamPoint.GetRandomPoint(transform, radius);
        }
        
        print("Found destination");
        
        mover.MoveTo(destination, roamSpeed);

        lookingForRoamPoint = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
