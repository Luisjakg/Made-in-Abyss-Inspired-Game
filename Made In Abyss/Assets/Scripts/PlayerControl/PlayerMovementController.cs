using System;
using System.Collections;
using Obi;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

namespace MIA.PlayerControl
{
    public class PlayerMovementController : MonoBehaviour
    {
        public bool CanMove { get; private set; } = true;
        private bool shouldSprint => canSprint && Input.GetKey(sprintKey);
        private bool shouldJump => Input.GetKeyDown(jumpKey) && isGrounded;
        private bool shouldCrouch => Input.GetKeyDown(crouchKey) && isGrounded;
        private bool shouldSlide => Input.GetKeyDown(slideKey) && isGrounded && isSprinting;

        [Header("Functional Options")] [SerializeField]
        private bool canSprint = true;
        [SerializeField] private bool canJump = true;
        [SerializeField] private bool canSlide = true;
        [SerializeField] private bool canCrouch = true;
        [SerializeField] private bool canUseHeadBob = true;
        [SerializeField] private bool useFootsteps = true;
        
        [Header("Audio")]
        [SerializeField] private AudioSource playerAudioSource = default;

        [Header("States")] [SerializeField] private bool isCrouching;
        [SerializeField] private bool isSliding;
        [SerializeField] private bool isJumping;
        [SerializeField] private bool isSprinting;

        [Header("Controls")] [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode slideKey = KeyCode.C;

        [Header("Movement Parameters")] [SerializeField]
        private float walkSpeed = 3.0f;

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
        
        [Header("Fall Damage Parameters")]
        [SerializeField] float maxFallDamage = 999999f;
        [SerializeField] private float fallDamageSpeedThreshold = -10f;
        [SerializeField] private AudioClip fallDamageSound = default;
        [SerializeField] private AudioClip massiveFallDamageSound = default;
        [SerializeField] private float massiveFallDamageThreshold; 
        private float maxYVelocity;

        [Header("Crouch Parameters")] [SerializeField]
        private float crouchYScale = .4f;

        private float startYScale;
        private bool isStuck;

        [Header("Sliding Parameters")] [SerializeField]
        private float slideForce = 200f;

        [SerializeField] private float slideYScale = .3f;
        [SerializeField] private float slopeIncreaseMultiplier;
        [SerializeField] private float slideSpeedUpTime = 1f;
        [SerializeField] private float slideSpeedDownTime = 1f;
        private float lerpSpeed;

        [Header("GroundCheck")] [SerializeField]
        private LayerMask whatIsGround;
        [SerializeField] private bool isGrounded;

        [Header("Step Parameters")] 
        [SerializeField] private Transform stepRayUpper;
        [SerializeField] private Transform stepRayLower;
        [SerializeField] private float stepHeight = 0.3f;
        [SerializeField] private float stepSmooth = 0.1f;
        
        [Header("Slope Handling")] [SerializeField, Range(0f, 90f)]
        private float maxSlopeAngle = 40f;

        [SerializeField, Range(0f, 90f)] private float slideSpeedUpSlopeAngle = 20f;
        private RaycastHit slopeHit;
        private bool exitingSlope;
        private float slopeAngle;

        [Header("HeadBob Parameters")] [SerializeField]
        private float walkBobSpeed = 14f;

        [SerializeField] private float walkBobAmount = .05f;
        [SerializeField] private float sprintBobSpeed = 18f;
        [SerializeField] private float sprintBobAmount = .1f;
        [SerializeField] private float crouchBobSpeed = 8f;
        [SerializeField] private float crouchBobAmount = .025f;
        private float defaultYPos = 0;
        private float headBobTimer;

        [Header("Footstep Parameters")] [SerializeField]
        private float baseStepSpeed = .05f;

        [SerializeField] private float crouchStepMultiplier = 1.5f;
        [SerializeField] private float sprintStepMultiplier = 0.6f;
        [SerializeField] private AudioClip[] grassClips = default;
        [SerializeField] private AudioClip[] stoneClips = default;
        [SerializeField] private AudioClip[] woodClips = default;
        private float footstepTimer = 0;

        private float GetCurrentOffSet => isCrouching ? baseStepSpeed * crouchStepMultiplier :
            isSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

        [Header("Orientation")] [SerializeField]
        private Transform orientation;

        [SerializeField] private Camera playerCam;
        [SerializeField] private Collider playerCollider;
        private Vector3 playerLookDirection;
        private float horizontalInput;
        private float verticalInput;

        private Rigidbody rb;

        private void Awake()
        {
            maxYVelocity = 0;
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            readyToJump = true;
            startYScale = transform.localScale.y;
            
            /*stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x,
                stepRayUpper.transform.position.y + stepHeight, stepRayUpper.transform.position.z);*/
        }

        private void Update()
        {
            currentSpeedMagnitude = rb.velocity.magnitude;
            isSprinting = shouldSprint && canSprint && !isSliding && !isCrouching;
            DesiredMoveSpeed();

            //ground check
            GroundCheck();

            if (CanMove) HandleMovementInput();

            if (canSprint) HandleSprint();

            SpeedControl();

            if (canJump) HandleJump();

            if (canSlide) HandleSlide();

            if (canCrouch) HandleCrouch();

            if (canUseHeadBob) HandleHeadBob();

            if (useFootsteps) HandleFootsteps();

            //StepClimb(); TODO this part is extremely buggy for the moment
            
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

        private void GroundCheck()
        {
            Vector3 cast = transform.position;
            
            //if (Physics.Raycast(transform.position, Vector3.down, CurrentHeight() * 0.5f + 0.2f, whatIsGround)) (OLD RAYCAST)
            if (Physics.SphereCast(cast, .4f, Vector3.down,out RaycastHit hit ,.65f, whatIsGround))
            {
                isJumping = false;
                isGrounded = true;
                if (maxYVelocity <= fallDamageSpeedThreshold)
                {
                    TakeFallDamage();
                    maxYVelocity = 0;
                }
            }
            else
            {
                isGrounded = false;
                if (rb.velocity.y < maxYVelocity)
                    maxYVelocity = rb.velocity.y;
            }
            
        }

        private void HandleMovementInput()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");

            verticalInput = Input.GetAxisRaw("Vertical");

            if (shouldSlide)
                moveDirection = playerCam.transform.forward;
            else if (!isSliding)
                moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        }

        private void HandleSprint()
        {
            if (!shouldSprint) return;
            bool roofAbove = Physics.Raycast(transform.position, Vector3.up, 1.2f);
            if (isCrouching && !roofAbove) StopCrouch();
            if (Input.GetKeyDown(sprintKey) && isSliding && !roofAbove) StopSlide();
        }

        private void HandleJump()
        {
            if (!shouldJump || !readyToJump) return;
            bool roofAbove = Physics.Raycast(transform.position, Vector3.up, 1.2f);
            if (isSliding && !roofAbove) StopSlide();

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
            readyToJump = true;
            exitingSlope = false;
        }

        private void HandleSlide()
        {
            // We check the current speed to avoid overlap with crouch
            if (!shouldSlide) return;
            if (isCrouching || !isGrounded) return;
            if (!isSliding && isSprinting && currentSpeedMagnitude > 3f) StartSlide();
            else StopSlideWithCrouch();
        }

        private void StartSlide()
        {
            isSliding = true;

            //If the user is in the air then we dont add downwards force in order to avoid weird movement
            if (isGrounded)
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

            transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);

        }

        private void StopSlideWithCrouch()
        {
            StopAllCoroutines();
            isSliding = false;
            StartCrouch();
        }

        private void StopSlide()
        {
            StopAllCoroutines();
            isSliding = false;
            canSprint = true;

            transform.localScale = new(transform.localScale.x, startYScale, transform.localScale.z);
        }

        private void HandleCrouch()
        {
            if (!shouldCrouch) return;
            if (isSprinting || isSliding || desiredMoveSpeed > walkSpeed) return;
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

            isStuck = false;
            canSprint = true;
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            isCrouching = false;
        }

        private void HandleHeadBob()
        {
            if (!isGrounded || isSliding) return;

            if (Math.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
            {
                headBobTimer += Time.deltaTime *
                                (isCrouching ? crouchBobSpeed : shouldSprint ? sprintBobSpeed : walkBobSpeed);
                playerCam.transform.localPosition = new Vector3(playerCam.transform.localPosition.x,
                    defaultYPos + (isCrouching ? crouchBobAmount : shouldSprint ? sprintBobAmount : walkBobAmount) *
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
                            playerAudioSource.PlayOneShot(
                                grassClips[Random.Range(0, woodClips.Length - 1)]);
                            break;
                        case "Footsteps/STONE":
                            playerAudioSource.PlayOneShot(
                                stoneClips[Random.Range(0, stoneClips.Length - 1)]);
                            break;
                        case "Footsteps/WOOD":
                            playerAudioSource.PlayOneShot(
                                woodClips[Random.Range(0, woodClips.Length - 1)]);
                            break;
                        default:
                            playerAudioSource.PlayOneShot(
                                stoneClips[Random.Range(0, stoneClips.Length - 1)]);
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
                rb.AddForce(GetSlopeMoveDirection(moveDirection) * (currentMoveSpeed * 20f), ForceMode.Force);

                //We add force to avoid weird bouncing while going down slopes
                if (rb.velocity.y > 0)
                    rb.AddForce(Vector3.down * (isCrouching ? 40f : 80f), ForceMode.Force);
            }

            //on ground
            else if (isGrounded)
                rb.AddForce(moveDirection.normalized * (currentMoveSpeed * 10f), ForceMode.Force);

            // in air
            else if (!isGrounded)
                rb.AddForce(moveDirection.normalized * (currentMoveSpeed * 10f * airMultiplier), ForceMode.Force);
        }


        private void SlidingMovement()
        {
            Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

            // normal sliding
            if (!OnSlope() || rb.velocity.y > -0.1f)
            {
                rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            }
            else
                rb.AddForce(GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);


            if (currentSpeedMagnitude <= crouchSpeed && isGrounded)
                StopSlideWithCrouch();
        }

        private void SpeedControl()
        {
            // limiting speed on slope
            if (OnSlope() && !exitingSlope && isGrounded)
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

            // check if desiredMoveSpeed has changed drastically
            if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && currentMoveSpeed != 0)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed(lerpSpeed));
            }
            else
            {
                currentMoveSpeed = desiredMoveSpeed;
            }

            lastDesiredMoveSpeed = desiredMoveSpeed;
        }
        
        private void StepClimb()
        {
            if (moveDirection == Vector3.zero) return;

            Vector3 stepDirection;

            if (OnSlope() && !exitingSlope) 
                stepDirection = GetSlopeMoveDirection(moveDirection);
            else
                stepDirection = moveDirection;

                //climb at an angle

            // if the bottom raycast collides but the top doesn't then bounce us up over the step
            Debug.DrawRay(stepRayLower.position, transform.TransformDirection(stepDirection), Color.green);
            Debug.DrawRay(stepRayUpper.position, transform.TransformDirection(stepDirection), Color.red);
            if (Physics.Raycast(stepRayLower.position, transform.TransformDirection(stepDirection), 1f))
            {
                Debug.Log("Lower Collision");
                if (!Physics.Raycast(stepRayUpper.position, transform.TransformDirection(stepDirection), 1f))
                {
                    Debug.Log("No upper collision");
                    rb.position -= new Vector3(0f, -stepSmooth, 0f);
                }
            }
        }

        private IEnumerator SmoothlyLerpMoveSpeed(float duration)
        {
            // TODO keep momentum when jumping while fast & moving forward
            bool decreasing;
            float elapsed = 0.0f;
            float startSpeed = currentMoveSpeed;
            float endSpeed = desiredMoveSpeed;

            if (startSpeed > endSpeed) decreasing = true;
            else decreasing = false;

            while (elapsed < duration)
            {
                currentMoveSpeed = Mathf.Lerp(startSpeed, endSpeed, elapsed / duration);
                if (OnSlope())
                {
                    float slopeAngleIncrease = 1 + (slopeAngle / 90f);
                    elapsed += Time.deltaTime * slopeIncreaseMultiplier * slopeAngleIncrease;
                }
                else
                    elapsed += Time.deltaTime;



                yield return null;
            }

            currentMoveSpeed = endSpeed;
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
            if (isSliding && OnSlope() && rb.velocity.y < -3f && slopeAngle > slideSpeedUpSlopeAngle)
            {
                desiredMoveSpeed = downSlopeSlideSpeed;
                lerpSpeed = slideSpeedUpTime;
            }
            else if (isSliding && OnSlope() && slopeAngle < slideSpeedUpSlopeAngle)
            {
                lerpSpeed = slideSpeedDownTime;
                desiredMoveSpeed = .0f;
            }
            else if (isSliding && OnSlope() && rb.velocity.y > 0f)
            {
                lerpSpeed = slideSpeedDownTime;
                desiredMoveSpeed = .0f;
            }
            else if (isSliding && isGrounded)
            {
                lerpSpeed = slideSpeedDownTime;
                desiredMoveSpeed = .0f;
            }
            else
                desiredMoveSpeed = ((!isStuck && isSprinting) || isSliding ? sprintSpeed :
                    isCrouching ? crouchSpeed : walkSpeed);
        }

        private float CurrentHeight()
        {
            float currentHeight = (isCrouching ? crouchYScale + 1 : isSliding ? slideYScale + 1 : standingHeight);
            return currentHeight;
        }
        
        private void TakeFallDamage()
        {
            if (maxYVelocity <= massiveFallDamageThreshold)
                playerAudioSource.PlayOneShot(massiveFallDamageSound);
            else
                playerAudioSource.PlayOneShot(fallDamageSound);
                

            //TODO call to the health script to reduce health or something -\_(;-;)_/-
        }

        public bool GetIsGrounded()
        {
            return isGrounded;
        }

        public Vector3 GetVelocity()
        {
            return rb.velocity;
        }

        
        //Ground check visualization
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position - new Vector3(0f, .65f, 0f), .4f);
        }
    }
}
