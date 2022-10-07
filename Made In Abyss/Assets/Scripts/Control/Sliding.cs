using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")] 
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float slideTimer;
    private bool sliding;
    public float slideYScale;
    private float startYScale;

    [Header("Input")] [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;



    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = playerObj.localScale.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && pm.GetState() == PlayerMovement.MovementState.sprinting) 
            StartSlide();
        

        if (Input.GetKeyUp(slideKey) && sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (sliding)
            SlidingMovement();
    }

    private void StartSlide()
    {
        sliding = true;

        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

        slideTimer -= Time.deltaTime;

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        /*if (Physics.Raycast(transform.position, Vector3.up, playerHeight * 0.5f + 0.3f))
            return;*/
        sliding = false;
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
    
    
}
