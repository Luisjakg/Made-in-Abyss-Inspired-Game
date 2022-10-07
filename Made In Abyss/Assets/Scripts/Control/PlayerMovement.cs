using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Keybinds")] 
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Action controllers")] 
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canSprint = true;
    
    
    [Header("Movement")]
    [SerializeField] private float walkspeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float groundDrag;
    private float moveSpeed;

    [Header("Crouching")] 
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float StartYScale;

    [Header("Jumping")] 
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump;
    
    [Header("Ground Check")] 
    [SerializeField] private float playerHeight;
    [SerializeField] private float sphereRadius;
    [SerializeField] public LayerMask whatIsGround;
    
    private bool grounded;

    [Header("Slope Handling")] 
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    
    
    [Header("Other")]
    [SerializeField] private Transform orientation;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private Rigidbody rb;

    [SerializeField] private MovementState state;
    [SerializeField] public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        readyToJump = true;
        rb.freezeRotation = true;

        StartYScale = transform.localScale.y;
    }
    
    private void Update()
    {
        grounded = Physics.SphereCast(transform.position, sphereRadius - .01f, Vector3.down, out var hit, 
            .85f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore);
        
        HandleInput();
        SpeedControl();
        StateHandler();

        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // start jump
        if (Input.GetKeyDown(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            
            Jump();
            
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        
        // start crouch
        if (Input.GetKeyDown(crouchKey))
        {
            Crouch();
        }
        
        // stop crouch
        if (Input.GetKeyUp(crouchKey))
        {
            StopCrouch();
        }
    }

    private void StateHandler()
    {
        //Mode - Crouching
        if (grounded && Input.GetKey(crouchKey) && canCrouch && state != MovementState.sprinting)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        
        //Mode - Sprinting
        else if (grounded && Input.GetKey(sprintKey) && canSprint)
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        
        //Mode - Walking
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkspeed;
        }

        else
        {
            state = MovementState.air;
        }
        
    }

    private void MovePlayer()
    {
        //Calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        
        //on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized* moveSpeed * 10, ForceMode.Force);
        //in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        
        // turn gravity off while on slope
        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        //limiting speed on ground or in air
        else
        {
            var flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                var limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }

    }

    private void Crouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void StopCrouch()
    {
        //Checks if there is an obstacle obstructing the Stop Crouch and if there is then we try again
        if (Physics.Raycast(transform.position, Vector3.up, playerHeight * 0.5f + 0.3f))
            return;
        transform.localScale = new Vector3(transform.localScale.x, StartYScale, transform.localScale.z);
    }

    private void Jump()
    {
        if (!canJump) return;
        
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    
    private void ResetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            Debug.Log("Handling slope");
            Debug.Log("Handling Steep Slope: " + (angle < maxSlopeAngle && angle != 0));

            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    public MovementState GetState()
    {
        return state;
    }
    
}





//Sphere raycast visualizer
/*private void OnDrawGizmosSelected()
{
    var castOrigin = transform.position;
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(castOrigin - new Vector3(0,.7f,0), sphereRadius);
}*/