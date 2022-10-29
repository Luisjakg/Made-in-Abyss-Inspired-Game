using System;
using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;
using UnityEngine.Android;

public class ClimbingRopeHandler2 : MonoBehaviour
{
    private bool shouldRetract => Input.GetKey(retractRope);
    private bool shouldExtend => Input.GetKey(extendRope);
    
    [Header("Functional Options")]
    [SerializeField] private bool canChangeSize = true;

    [Header("Controls")] 
    [SerializeField] private KeyCode retractRope = KeyCode.Mouse0;
    [SerializeField] private KeyCode extendRope = KeyCode.Mouse1;
    
    [Header("Obi References")]
    [SerializeField] private ObiSolver obiSolver;
    [SerializeField] private ObiCollider playerObiCollider;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private ObiRopeSection obiRopeSection;

    [Header("Rope Settings")] 
    [SerializeField] private float hookResolution;
    [SerializeField] private float extendSpeed = 3f;
    [SerializeField] private float retractSpeed = 3f;
    [SerializeField] private float minimumRopeSize = 1f;
    [SerializeField] private int particlePoolSize = 150;

    private ObiRope rope;
    private ObiRopeBlueprint blueprint;
    private ObiRopeExtrudedRenderer ropeRenderer;

    private ObiRopeCursor ropeCursor;

    private Transform hookTransform;


    private void Awake()
    {
        //Create rope and solver
        rope = gameObject.AddComponent<ObiRope>();
        ropeRenderer = gameObject.AddComponent<ObiRopeExtrudedRenderer>();
        ropeRenderer.section = obiRopeSection;
        ropeRenderer.uvScale = new Vector2(1, 4);
        ropeRenderer.normalizeV = false;
        ropeRenderer.uvAnchor = 1;
        rope.GetComponent<MeshRenderer>().material = ropeMaterial;
        
        //Setup rope blueprint
        blueprint = ScriptableObject.CreateInstance<ObiRopeBlueprint>();
        blueprint.resolution = 0.5f;
        blueprint.pooledParticles = particlePoolSize;
        
        //rope parameters tweaks
        rope.maxBending = 0.02f;
        
        //Add a cursor to enable changing rope length
        ropeCursor = rope.gameObject.AddComponent<ObiRopeCursor>();
        ropeCursor.cursorMu = 0.15f;
        ropeCursor.direction = true;
    }

    private void Update()
    {
        if (canChangeSize) HandleRopeLength();
    }

    private void HandleRopeLength()
    {
        if (shouldExtend && shouldRetract) return;
        if (shouldRetract && !(rope.restLength <= minimumRopeSize))
            RetractRope(retractSpeed);
        else if (shouldExtend) ExtendRope(extendSpeed);
    }
    
    private void RetractRope(float velocity)
    {
        ropeCursor.ChangeLength(rope.restLength - velocity * Time.deltaTime);
    }
        
    private void ExtendRope(float velocity)
    {
        ropeCursor.ChangeLength(rope.restLength + velocity * Time.deltaTime);
    }



    private void OnDestroy()
    {
        DestroyImmediate(blueprint);
    }

    
}
