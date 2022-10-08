using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;
    private bool isSprinting => canSprint && Input.GetKey(sprintKey);
    private bool shouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool shouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;

    [Header("Functional Options")] 
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadBob = true;
    [SerializeField] private bool WillSlideOnSlopes = true;

    [Header("Controls")] 
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Movement Parameters")] 
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 2.0f;


    [Header("Look Parameters")] 
    [SerializeField, Range(1, 10)] private float lookSpeed = 1.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Jumping Parameters")] 
    [SerializeField] private float jumpForce = 8.0f;

    [SerializeField] private float gravity = 3.0f;

    [Header("Crouch Parameters")] 
    [SerializeField] private float crouchHeight = .5f;
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float timeToCrouch = .025f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("HeadBob Parameters")] 
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = .05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = .1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = .025f;
    private float defaultYpos = 0;
    private float timer;
    
    [Header("Slope Sliding Parameters")]
    [SerializeField] private float slopeSpeed = 8.0f;
    [SerializeField] private float steepSlopeSpeed = 20f;
    [SerializeField, Range(0, 90)] private float steepSlopeStartingAngle = 70f;
    private Vector3 hitPointNormal;
    private float slideAngle;
    private bool IsSliding
    {
        get
        {
            float sphereCastVerticalOffset = characterController.height / 2 - characterController.radius;
            var castOrigin = transform.position - new Vector3(0, sphereCastVerticalOffset, 0);
            
            //If we are standing on a steep slope then we disable jump
            if(Vector3.Angle(hitPointNormal, Vector3.up) > steepSlopeStartingAngle)
            {
                canJump = false;
            } 
            else
            {
                canJump = true;
            }
            
            if (characterController.isGrounded && Physics.SphereCast(castOrigin, characterController.radius - 0.1f, 
                Vector3.down, out var slopeHit, 1f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
            {
                hitPointNormal = slopeHit.normal;
                slideAngle = Vector3.Angle(Vector3.up, hitPointNormal);
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }

    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYpos = playerCamera.transform.localPosition.y;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void Update()
    {

        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();
            
            if (canJump) HandleJump();
            
            if (canCrouch) HandleCrouch();

            if (canUseHeadBob) HandleHeadBob();

            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"),
            (isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) +
                        (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {

        rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

    }

    private void HandleJump()
    {
        if (shouldJump)
        {
            moveDirection.y = jumpForce;
        }
    }

    private void HandleCrouch()
    {
        if (shouldCrouch) StartCoroutine(CrouchStand());
    }
    
    
    private void HandleHeadBob()
    {
        if (!characterController.isGrounded) return;

        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : isSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x,
                defaultYpos + (isCrouching ? crouchBobAmount : isSprinting ? sprintBobAmount : walkBobAmount) * Mathf.Sin(timer),
                playerCamera.transform.localPosition.z);
        }
    }

    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded) moveDirection.y -= gravity * Time.deltaTime;

        if (WillSlideOnSlopes && IsSliding) HandleSlide();

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleSlide()
    {
        //Slide speed is calculated using the slope angle
        if (!WillSlideOnSlopes) return;
        var yInverse = 1f - hitPointNormal.y;

        if (slideAngle >= steepSlopeStartingAngle)
        {
            moveDirection.x += yInverse * hitPointNormal.x * steepSlopeSpeed;
            moveDirection.z += yInverse * hitPointNormal.z * steepSlopeSpeed;
        }
        else
        {
            moveDirection.x += yInverse * hitPointNormal.x * slopeSpeed;
            moveDirection.z += yInverse * hitPointNormal.z * slopeSpeed;
        }
    }

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f)) 
            yield break;
        
        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }
    
}
