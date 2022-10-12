using System;
using System.Collections;
using Obi;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    private bool shouldSprint => canSprint && Input.GetKey(sprintKey);
    private bool shouldJump => Input.GetKeyDown(jumpKey) && isGrounded;
    private bool shouldCrouch => Input.GetKeyDown(crouchKey) && isGrounded;
    private bool shouldSlide => Input.GetKeyDown(slideKey) && isGrounded;

    [Header("Functional Options")] 
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canSlide = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadBob = true;
    [SerializeField] private bool canInputHorizontal = true;
    [SerializeField] private bool canInputVertical = true;
    [SerializeField] private bool useFootsteps = true;
    
    [Header("States")]
    [SerializeField] private bool isCrouching;
    [SerializeField] private bool isSliding;
    [SerializeField] private bool isJumping;
    [SerializeField] private bool isSprinting;

    [Header("Controls")] 
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode slideKey = KeyCode.C;

    [Header("Movement Parameters")] 
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float downSlopeSlideSpeed = 15f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float currentMoveSpeed = 0f;
    [SerializeField] private float desiredMoveSpeed; 
    private float currentSpeedMagnitude;
    private float lastDesiredMoveSpeed;
    private Vector3 moveDirection;

    [Header("Jumping Parameters")] 
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float jumpCooldown = 0f;
    [SerializeField] private float airMultiplier = 0.5f;
    private bool readyToJump;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchYScale = .4f;
    private float startYScale;
    private bool isStuck;

    [Header("Sliding Parameters")] 
    [SerializeField] private float maxSlideTime = 1.5f;
    [SerializeField] private float slideForce = 200f;
    [SerializeField] private float slideYScale = .3f;
    [SerializeField] private float slopeIncreaseMultiplier;
    [SerializeField] private float speedIncreaseMultiplier;
    private float slideTimer;

    [Header("GroundCheck")] 
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool isGrounded;

    [Header("Slope Handling")] 
    [SerializeField, Range(0f, 90f)] private float maxSlopeAngle = 40f;
    [SerializeField, Range(0f, 90f)] private float slideSpeedUpSlopeAngle = 20f;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    private float slopeAngle;
    
    [Header("HeadBob Parameters")] 
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = .05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = .1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = .025f;
    private float defaultYpos = 0;
    private float headBobTimer;

    [Header("Footstep Parameters")] 
    [SerializeField] private float baseStepSpeed = .05f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] stoneClips = default;
    [SerializeField] private AudioClip[] woodClips = default;
    private float footstepTimer = 0;
    private float GetCurrentOffSet => isCrouching ? baseStepSpeed * crouchStepMultiplier :
        isSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Orientation")] 
    [SerializeField] private Transform orientation;
    [SerializeField] private Camera playerCam;
    private Vector3 playerLookDirection;

    private float horizontalInput;
    private float verticalInput;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        currentSpeedMagnitude = rb.velocity.magnitude;
        isSprinting = shouldSprint;
        DesiredMoveSpeed();
        
        //ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, CurrentHeight() * 0.5f + 0.2f, whatIsGround);

        if (CanMove) HandleMovementInput();

        if (canSprint) HandleSprint();
        
        SpeedControl();

        if (canJump) HandleJump();

        if (canSlide) HandleSlide();

        if (canCrouch) HandleCrouch();

        if (canUseHeadBob) HandleHeadBob();

        if (useFootsteps) HandleFootsteps();
        
        //handle drag

        if (isGrounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

    }

    private void FixedUpdate()
    {
        if (isSliding)
            SlidingMovement(); 
        
        ApplyFinalMovements();
    }

    private void HandleMovementInput()
    {
        if (canInputHorizontal) horizontalInput = Input.GetAxisRaw("Horizontal");
        else horizontalInput = 0;

        if (canInputVertical) verticalInput = Input.GetAxisRaw("Vertical");
        else verticalInput = 0;

        if (shouldSlide)
            moveDirection = new Vector3(playerCam.GetComponent<PlayerCam>().getCameraLookDirection().x, 0, playerCam.GetComponent<PlayerCam>().getCameraLookDirection().z);
        else if (!isSliding)
            moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
    }
    
    private void HandleSprint()
    {
        if (!shouldSprint) return;
        if (isCrouching) StopCrouch();
    }
    
    private void HandleJump()
    {
        if (!shouldJump) return;
        
        if(isSliding) StopSlide();

        isJumping = true;
        readyToJump = false;
        exitingSlope = true;

        //reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump()
    {
        isJumping = false;
        readyToJump = true;
        exitingSlope = false;
    }
    
    private void HandleSlide()
    {
        // We check the current speed to avoid overlap with crouch
        if(currentSpeedMagnitude < sprintSpeed - .5f && !isSliding) return;
        if (!shouldSlide || isCrouching || !isGrounded) return;
        if (!isSliding) StartSlide();
        else StopSlide();
    }

    private void StartSlide()
    {
        isSliding = true;
        canInputVertical = false;

        //If the user is in the air then we dont add downwards force in order to avoid weird movement
        if (isGrounded)
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        
        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        slideTimer = maxSlideTime;
    }

    private void StopSlide()
    {
        isSliding = false;
        canInputVertical = true;
        StartCrouch();
        /*if (Physics.Raycast(transform.position, Vector3.up, 1.2f))
        {
            canSprint = false;
            isCrouching = true;
            return;
        }
        canSprint = true;
        isCrouching = false;

        transform.localScale = new(transform.localScale.x, startYScale, transform.localScale.z);*/
    }
    
    private void HandleCrouch()
    {
        if (currentMoveSpeed > walkSpeed + .5f) return;
        if (!shouldCrouch || isSliding) return;
        if (!isCrouching) StartCrouch();
        else StopCrouch();
    }

    private void StartCrouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        isCrouching = true;
    }

    private void StopCrouch()
    {
        if (Physics.Raycast(transform.position, Vector3.up, 1.2f))
        {
            isStuck = true;
            return;
        }
        else isStuck = false;
        canSprint = true;
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        isCrouching = false;
    }

    private void HandleHeadBob()
    {
        if (!isGrounded || isSliding) return;

        if (Math.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            headBobTimer += Time.deltaTime * (isCrouching ? crouchBobSpeed : shouldSprint ? sprintBobSpeed : walkBobSpeed);
            playerCam.transform.localPosition = new Vector3(playerCam.transform.localPosition.x,
                defaultYpos + (isCrouching ? crouchBobAmount : shouldSprint ? sprintBobAmount : walkBobAmount) *
                Mathf.Sin(headBobTimer),
                playerCam.transform.localPosition.z);
        }
    }
    
    private void HandleFootsteps()
    {
        if (!isGrounded || isSliding) return;
        if (currentSpeedMagnitude < 1f) return;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(playerCam.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                switch (hit.collider.tag)
                {
                    case "Footsteps/GRASS":
                        footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, woodClips.Length - 1)]);
                        break;
                    case "Footsteps/STONE":
                        footstepAudioSource.PlayOneShot(stoneClips[UnityEngine.Random.Range(0, stoneClips.Length - 1)]);
                        break;
                    case "Footsteps/WOOD":
                        footstepAudioSource.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length - 1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(stoneClips[UnityEngine.Random.Range(0, stoneClips.Length - 1)]);
                        break;
                }
            }
            footstepTimer = GetCurrentOffSet;
        }
    }
    
    private void ApplyFinalMovements()
    {
        //Turn off gravity while in slope
        rb.useGravity = !OnSlope();
        // on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * currentMoveSpeed * 20f, ForceMode.Force);
            
            //We add force to avoid weird bouncing while going down slopes
            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        //on ground
        else if (isGrounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f, ForceMode.Force);
        
        // in air
        else if (!isGrounded)
            rb.AddForce(moveDirection.normalized * currentMoveSpeed * 10f * airMultiplier, ForceMode.Force);
    }


    private void SlidingMovement()
    {
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // normal sliding
        if (!OnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }
        else
            rb.AddForce(GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);


        if (currentSpeedMagnitude <= 2f && isGrounded)
            StopSlide();
    }

    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (currentSpeedMagnitude > currentMoveSpeed)
                rb.velocity = rb.velocity.normalized * currentMoveSpeed;
        }

        // limiting speed on ground or in air
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > currentMoveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * currentMoveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }

        //Check if player stopped to remove all momentum
        if (currentSpeedMagnitude < desiredMoveSpeed)
        {
            StopAllCoroutines();
            currentMoveSpeed = desiredMoveSpeed;
        }
        // check if desiredMoveSpeed has changed drastically
        else if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && currentMoveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            currentMoveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }
    
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementSpeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - currentMoveSpeed);
        float startValue = currentMoveSpeed;

        while (time < difference)
        {
            currentMoveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        currentMoveSpeed = desiredMoveSpeed;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, standingHeight * 0.5f + 0.3f))
        {
            slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return slopeAngle < maxSlopeAngle && slopeAngle != 0;
        }

        return false;
    }
    
    private Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void DesiredMoveSpeed()
    {
        if (isSliding && OnSlope() && rb.velocity.y < -7f && slopeAngle > slideSpeedUpSlopeAngle)
        {
            Debug.Log(rb.velocity.y);
            Debug.Log("Down slope detected");
            desiredMoveSpeed = downSlopeSlideSpeed;
        }
        else if (isSliding && OnSlope() && slopeAngle < slideSpeedUpSlopeAngle)
            desiredMoveSpeed = .0f;
        else if (isSliding && OnSlope() && rb.velocity.y > 0f)
            desiredMoveSpeed = .0f;
        else if (isSliding && isGrounded)
            desiredMoveSpeed = .0f;
        else
            desiredMoveSpeed = (!isStuck && shouldSprint || isSliding ? sprintSpeed : isCrouching ? crouchSpeed : walkSpeed);
    }

    private float CurrentHeight()
    {
        float currentHeight = (isCrouching ? crouchYScale + 1 : isSliding ? slideYScale + 1 : standingHeight);
        return currentHeight; 
    }
}
