using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 7f;
    public float jumpForce = 7f;
    public float gravity = -25f;
    public float acceleration = 12f;
    public float deceleration = 10f;
    public float airControl = 0.4f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashEndDeceleration = 15f;
    public bool canJumpDuringDash = false;

    [Header("Mouse Look Settings")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public float aimMouseSensitivityMultiplier = 0.6f;

    [Header("Camera Effects")]
    public float dashCameraTilt = 5f;
    public float cameraTiltSpeed = 8f;
    public float landingCameraShake = 0.15f;
    public float landingMinVelocity = -10f;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobFrequency = 10f;
    public float bobHorizontalAmplitude = 0.05f;
    public float bobVerticalAmplitude = 0.08f;

    [Header("Animation")]
    public Animator animator;
    public Animator gunAnimator;

    [Header("Jump & Fall Settings")]
    public float jumpBufferTime = 0.12f;
    public float coyoteTimeMax = 0.12f;
    public float fallMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.2f;
    public float maxFallSpeed = 50f;

    [Header("Camera Heights")]
    public float cameraStandHeight = 1.6f;

    // Components (cached)
    private CharacterController controller;
    private Transform cachedTransform;

    // Input state (cached)
    private Vector2 input;
    private bool jumpPressed;
    private bool dashPressed;
    private bool isAiming;
    private float mouseX;
    private float mouseY;

    // Movement state
    private Vector3 velocity;
    private Vector3 currentVelocity;
    private Vector3 dashDirection;
    private float rotationX;
    private bool isDashing;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private bool wasGroundedLastFrame;
    private float lastYVelocity;
    
    // Camera effects
    private float cameraTiltTarget;
    private float currentCameraTilt;
    private Vector3 cameraShakeOffset;
    private float bobTimer;
    private Vector3 originalCameraPos;
    
    // Timers
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    // Cached values
    private bool hasAnimator;
    private bool hasGunAnimator;
    private const float groundedCheckVelocity = -2f;
    
    // Public state
    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsDashing { get; private set; }
    public float DashCooldownNormalized => Mathf.Clamp01(1f - (dashCooldownRemaining / dashCooldown));

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cachedTransform = transform;
        hasAnimator = animator != null;
        hasGunAnimator = gunAnimator != null;
        
        if (cameraHolder != null)
        {
            cameraHolder.localPosition = new Vector3(0f, cameraStandHeight, 0f);
            originalCameraPos = cameraHolder.localPosition;
        }

        wasGroundedLastFrame = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        CacheInput();
        HandleMouseLook();
        HandleDash();
        HandleJump();
        ApplyGravity();
        ApplyMovement();
        HandleLanding();
        UpdateCameraEffects();
        UpdateHeadBob();
        UpdateAnimators();
    }

    // ================= INPUT CACHING =================

    void CacheInput()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        jumpPressed = Input.GetButtonDown("Jump");
        dashPressed = Input.GetKeyDown(KeyCode.LeftShift);
        isAiming = Input.GetMouseButton(1);
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        IsMoving = input.sqrMagnitude > 0.01f;
        IsDashing = isDashing;

        jumpBufferCounter = jumpPressed ? jumpBufferTime : Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining -= Time.deltaTime;
        }
    }

    // ================= DASH =================

    void HandleDash()
    {
        if (isDashing)
        {
            dashTimeRemaining -= Time.deltaTime;
            if (dashTimeRemaining <= 0f)
            {
                isDashing = false;
                currentVelocity *= 0.5f;
            }
        }

        if (dashPressed && !isDashing && dashCooldownRemaining <= 0f && IsGrounded)
        {
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
            currentVelocity = dashDirection * dashSpeed;
            
            velocity.y = Mathf.Max(velocity.y, 2f);
            
            cameraTiltTarget = dashCameraTilt;
        }
    }

    // ================= MOUSE LOOK =================

    void HandleMouseLook()
    {
        if (Mathf.Approximately(mouseX, 0f) && Mathf.Approximately(mouseY, 0f))
            return;

        float sensitivity = isAiming ? mouseSensitivity * aimMouseSensitivityMultiplier : mouseSensitivity;

        cachedTransform.Rotate(0f, mouseX * sensitivity, 0f);

        rotationX = Mathf.Clamp(rotationX - mouseY * sensitivity, -maxLookAngle, maxLookAngle);
    }

    // ================= MOVEMENT =================

    void ApplyMovement()
    {
        Vector3 targetVelocity;

        if (isDashing)
        {
            targetVelocity = dashDirection * dashSpeed;
        }
        else
        {
            Vector3 forward = cachedTransform.forward;
            Vector3 right = cachedTransform.right;
            
            Vector3 moveDirection = (forward * input.y + right * input.x).normalized;
            targetVelocity = moveDirection * walkSpeed;
        }
        
        float accelRate;
        
        if (isDashing)
        {
            accelRate = 1000f;
        }
        else if (IsGrounded)
        {
            accelRate = IsMoving ? acceleration : deceleration;
        }
        else
        {
            accelRate = acceleration * airControl;
        }
        
        currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetVelocity.x, accelRate * Time.deltaTime);
        currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetVelocity.z, accelRate * Time.deltaTime);
        
        // Замедление после дэша
        if (!isDashing && dashTimeRemaining < 0f && dashTimeRemaining > -0.5f)
        {
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetVelocity.x, dashEndDeceleration * Time.deltaTime);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, targetVelocity.z, dashEndDeceleration * Time.deltaTime);
            dashTimeRemaining -= Time.deltaTime;
        }
        
        velocity.x = currentVelocity.x;
        velocity.z = currentVelocity.z;
        
        controller.Move(velocity * Time.deltaTime);
    }

    // ================= JUMP & GRAVITY =================

    void HandleJump()
    {
        IsGrounded = controller.isGrounded;

        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTimeMax;

            if (velocity.y < 0f)
            {
                velocity.y = groundedCheckVelocity;
            }
        }
        else
        {
            coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.deltaTime);
        }

        bool canJump = canJumpDuringDash || !isDashing;

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && canJump)
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
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
    }

    // ================= LANDING =================

    void HandleLanding()
    {
        if (IsGrounded && !wasGroundedLastFrame)
        {
            if (lastYVelocity < landingMinVelocity)
            {
                float impactStrength = Mathf.InverseLerp(0f, landingMinVelocity, lastYVelocity);
                StartCameraShake(landingCameraShake * impactStrength);
            }
        }
        
        wasGroundedLastFrame = IsGrounded;
        lastYVelocity = velocity.y;
    }

    // ================= HEAD BOB =================

    void UpdateHeadBob()
    {
        if (!enableHeadBob || cameraHolder == null || !IsGrounded) return;

        if (IsMoving && !isDashing)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            
            float horizontalBob = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
            float verticalBob = Mathf.Sin(bobTimer * 2f) * bobVerticalAmplitude;
            
            Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            Vector3 targetPos = originalCameraPos + bobOffset + cameraShakeOffset;
            
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                targetPos,
                10f * Time.deltaTime
            );
        }
        else
        {
            bobTimer = 0f;
            
            Vector3 targetPos = originalCameraPos + cameraShakeOffset;
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                targetPos,
                10f * Time.deltaTime
            );
        }
    }

    // ================= CAMERA EFFECTS =================

    void UpdateCameraEffects()
    {
        if (cameraHolder == null) return;

        // Наклон только при дэше
        if (!isDashing)
        {
            cameraTiltTarget = 0f;
        }
        
        currentCameraTilt = Mathf.Lerp(currentCameraTilt, cameraTiltTarget, cameraTiltSpeed * Time.deltaTime);
        
        // Тряска камеры
        if (cameraShakeOffset.sqrMagnitude > 0.001f)
        {
            cameraShakeOffset = Vector3.Lerp(cameraShakeOffset, Vector3.zero, 10f * Time.deltaTime);
        }
        else
        {
            cameraShakeOffset = Vector3.zero;
        }
        
        // Применяем наклон
        Quaternion tiltRotation = Quaternion.Euler(rotationX, 0f, currentCameraTilt);
        cameraHolder.localRotation = tiltRotation;
    }

    void StartCameraShake(float intensity)
    {
        cameraShakeOffset = new Vector3(
            Random.Range(-intensity, intensity),
            Random.Range(-intensity, intensity) * 0.5f,
            0f
        );
    }

    // ================= ANIMATION =================

    void UpdateAnimators()
    {
        if (hasAnimator)
        {
            animator.SetFloat("Speed", currentVelocity.magnitude);
            animator.SetBool("IsGrounded", IsGrounded);
        }

        if (hasGunAnimator)
        {
            gunAnimator.SetFloat("Speed", input.magnitude);
            gunAnimator.SetBool("IsDashing", isDashing);
            gunAnimator.SetBool("IsAiming", isAiming);
        }
    }
}