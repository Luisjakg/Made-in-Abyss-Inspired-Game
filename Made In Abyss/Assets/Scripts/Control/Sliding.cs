using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm;
    private bool canSlide;
    private bool grounded; 
    private PlayerMovement.MovementState currentMovementState;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideYScale;
    private float startYScale;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
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
        grounded = pm.getIsGrounded();
        currentMovementState = pm.getMovementState();
        canSlide = pm.getCanSlide();
        if (!canSlide) return;
        verticalInput = Input.GetAxisRaw("Forward");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) 
                                       && currentMovementState != PlayerMovement.MovementState.crouching
                                       && currentMovementState == PlayerMovement.MovementState.sprinting)
            StartSlide();

        if (Input.GetKeyUp(slideKey) && pm.sliding)
            StopSlide();
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }

    private void StartSlide()
    {
        pm.sliding = true;

        //If the user is in the air then we dont add downwards force in order to avoid weird movement
        if (grounded)
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // sliding normal
        if(!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }

        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
            StopSlide();
    }

    private void StopSlide()
    {
        pm.sliding = false;
        if (Physics.Raycast(transform.position, Vector3.up, pm.getPlayerHeight() * 0.5f + 0.3f))
        {
            Debug.Log("obstruction detected queeen");
            return; 
        }
   

        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);
    }
}
