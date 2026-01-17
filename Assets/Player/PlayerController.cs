using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float jumpForce = 7f;
    public float gravity = -25f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float normalHeight = 2f;
    public float heightLerpSpeed = 10f;

    [Header("Mouse Look Settings")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Animation")]
    public Animator animator;
    public Animator gunAnimator;

    [Header("Jump & Fall Settings")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTimeMax = 0.12f;
    public float fallMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.2f;

    [Header("Camera Heights")]
    public float cameraStandHeight = 1.6f;
    public float cameraCrouchHeight = 1.0f;

    // Components (cached)
    private CharacterController controller;
    private Transform cachedTransform;

    // Input state (cached)
    private Vector2 input;
    private bool jumpPressed;
    private bool dashPressed;
    private float mouseX;
    private float mouseY;

    // Movement state
    private Vector3 velocity;
    private Vector3 dashDirection;
    private float rotationX;
    private float currentSpeed;
    private float targetHeight;
    private bool isCrouching;
    private bool heightTransitioning;
    private bool isDashing;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    
    // Timers
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    // Cached values
    private bool hasAnimator;
    private bool hasGunAnimator;
    private const float groundedCheckVelocity = -2f;
    private const float minHeightDifference = 0.01f;
    
    // Public state
    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsDashing { get; private set; }
    public float DashCooldownNormalized => Mathf.Clamp01(1f - (dashCooldownRemaining / dashCooldown));

    void Start()
    {
        // Cache components
        controller = GetComponent<CharacterController>();
        cachedTransform = transform;
        
        // Cache animator checks
        hasAnimator = animator != null;
        hasGunAnimator = gunAnimator != null;

        // Initialize controller
        controller.height = normalHeight;
        controller.center = new Vector3(0f, normalHeight * 0.5f, 0f);
        
        // НЕ поднимаем весь объект - CharacterController должен стоять на земле
        
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = new Vector3(0f, cameraStandHeight, 0f);
        }

        targetHeight = normalHeight;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        CacheInput();
        HandleMouseLook();
        HandleDash();
        DetermineSpeed();
        HandleCrouch();
        HandleJump();
        ApplyGravity();
        ApplyMovement();
        UpdateAnimators();
    }

    // ================= INPUT CACHING =================

    void CacheInput()
    {
        // Cache all input in one place
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        jumpPressed = Input.GetButtonDown("Jump");
        dashPressed = Input.GetKeyDown(KeyCode.LeftShift);
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        // Calculate derived state
        IsMoving = input.sqrMagnitude > 0.01f;
        IsDashing = isDashing;

        // Update jump buffer
        jumpBufferCounter = jumpPressed ? jumpBufferTime : Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        
        // Update dash cooldown
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }
    }

    // ================= DASH =================

    void HandleDash()
    {
        // Update dash timer
        if (isDashing)
        {
            dashTimeRemaining -= Time.deltaTime;
            if (dashTimeRemaining <= 0f)
            {
                isDashing = false;
            }
        }

        // Initiate dash
        if (dashPressed && !isDashing && dashCooldownRemaining <= 0f && !isCrouching)
        {
            // Determine dash direction based on input or forward if no input
            Vector3 forward = cachedTransform.forward;
            Vector3 right = cachedTransform.right;
            
            if (IsMoving)
            {
                dashDirection = (forward * input.y + right * input.x).normalized;
            }
            else
            {
                dashDirection = forward;
            }

            isDashing = true;
            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;
        }
    }

    // ================= MOUSE LOOK =================

    void HandleMouseLook()
    {
        if (Mathf.Approximately(mouseX, 0f) && Mathf.Approximately(mouseY, 0f))
            return;

        cachedTransform.Rotate(0f, mouseX * mouseSensitivity, 0f);

        rotationX = Mathf.Clamp(rotationX - mouseY * mouseSensitivity, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    // ================= MOVEMENT =================

    void DetermineSpeed()
    {
        if (isDashing)
        {
            currentSpeed = dashSpeed;
        }
        else
        {
            currentSpeed = isCrouching ? crouchSpeed : runSpeed;
        }
    }

    void ApplyMovement()
    {
        Vector3 moveDirection;

        if (isDashing)
        {
            // Use cached dash direction
            moveDirection = dashDirection;
        }
        else
        {
            // Calculate horizontal movement
            Vector3 forward = cachedTransform.forward;
            Vector3 right = cachedTransform.right;
            
            float moveX = input.x;
            float moveZ = input.y;
            
            moveDirection = (forward * moveZ + right * moveX).normalized;
        }
        
        // Combine horizontal and vertical movement
        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;
        
        controller.Move(velocity * Time.deltaTime);
    }

    // ================= JUMP & GRAVITY =================

    void HandleJump()
    {
        IsGrounded = controller.isGrounded;

        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTimeMax;

            // Reset downward velocity when grounded
            if (velocity.y < 0f)
            {
                velocity.y = groundedCheckVelocity;
            }
        }
        else
        {
            coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.deltaTime);
        }

        // Execute jump if buffered and coyote time active
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }
    }

    void ApplyGravity()
    {
        float gravityMultiplier;

        if (velocity.y < 0f)
        {
            gravityMultiplier = fallMultiplier;
        }
        else if (velocity.y > 0f && !Input.GetButton("Jump"))
        {
            gravityMultiplier = lowJumpMultiplier;
        }
        else
        {
            gravityMultiplier = 1f;
        }

        velocity.y += gravity * gravityMultiplier * Time.deltaTime;
    }

    // ================= CROUCH =================

    void HandleCrouch()
    {
        bool crouchKeyDown = Input.GetKeyDown(KeyCode.LeftControl);
        bool crouchKeyUp = Input.GetKeyUp(KeyCode.LeftControl);

        // State change
        if (crouchKeyDown)
        {
            isCrouching = true;
            targetHeight = crouchHeight;
            heightTransitioning = true;

            if (hasAnimator)
            {
                animator.SetBool("isCrouching", true);
            }
        }
        else if (crouchKeyUp)
        {
            isCrouching = false;
            targetHeight = normalHeight;
            heightTransitioning = true;

            if (hasAnimator)
            {
                animator.SetBool("isCrouching", false);
            }
        }

        // Smooth height transition (only when transitioning)
        if (heightTransitioning)
        {
            float newHeight = Mathf.Lerp(controller.height, targetHeight, heightLerpSpeed * Time.deltaTime);
            
            // Check if transition complete
            if (Mathf.Abs(newHeight - targetHeight) < minHeightDifference)
            {
                newHeight = targetHeight;
                heightTransitioning = false;
            }

            controller.height = newHeight;
            controller.center = new Vector3(0f, newHeight * 0.5f, 0f);

            // Update camera position
            float camTargetY = isCrouching ? cameraCrouchHeight : cameraStandHeight;
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                new Vector3(0f, camTargetY, 0f),
                heightLerpSpeed * Time.deltaTime
            );
        }
    }

    // ================= ANIMATION =================

    void UpdateAnimators()
    {
        if (!hasGunAnimator)
            return;

        float movementMagnitude = input.magnitude;
        gunAnimator.SetFloat("Speed", movementMagnitude);
        gunAnimator.SetBool("IsDashing", isDashing);
    }
}