using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float jumpForce = 7f;
    public float gravity = -25f;

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

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.12f;
    private float jumpBufferCounter;

    public float cameraStandHeight = 1.6f;
    public float cameraCrouchHeight = 1.0f;

    // Coyote Time
    public float coyoteTimeMax = 0.12f;
    private float coyoteTimeCounter;

    // Fall tuning
    public float fallMultiplier = 1.8f;
    public float lowJumpMultiplier = 2.2f;

    // Components
    private CharacterController controller;

    // State
    private Vector2 input;
    private Vector3 velocity;
    private float rotationX;
    private float currentSpeed;
    private float targetHeight;
    private bool isCrouching;

    // Public info
    public bool IsGrounded { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        controller.height = normalHeight;
        controller.center = new Vector3(0f, normalHeight / 2f, 0f);
        transform.position += Vector3.up * (controller.height / 2f);

        if (cameraHolder != null)
            cameraHolder.localPosition = new Vector3(0f, cameraStandHeight, 0f);

        targetHeight = normalHeight;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        ReadInput();
        HandleMouseLook();
        HandleSpeed();
        HandleCrouch();

        HandleJump();
        ApplyGravity();

        ApplyMovement();

        UpdateAnimator();
    }

    // ================= INPUT =================

    void ReadInput()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        IsMoving = input.sqrMagnitude > 0.01f;
        IsRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    // ================= MOUSE LOOK =================

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    // ================= MOVEMENT =================

    void HandleSpeed()
    {
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (IsRunning)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;
    }


    // ================= JUMP & GRAVITY =================

    void HandleJump()
    {
        IsGrounded = controller.isGrounded;

        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTimeMax;

            // ВАЖНО: не затираем импульс прыжка
            if (velocity.y < 0f)
                velocity.y = -2f;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            coyoteTimeCounter = 0f;
            jumpBufferCounter = 0f;
        }
    }



    void ApplyGravity()
    {
        if (velocity.y < 0f)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else if (velocity.y > 0f && !Input.GetButton("Jump"))
        {
            velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    void ApplyMovement()
    {
        Vector3 moveXZ =
            transform.forward * input.y +
            transform.right * input.x;

        Vector3 finalMove =
            moveXZ.normalized * currentSpeed +
            Vector3.up * velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }




    // ================= CROUCH =================

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            targetHeight = crouchHeight;

            if (animator != null)
                animator.SetBool("isCrouching", true);
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            targetHeight = normalHeight;

            if (animator != null)
                animator.SetBool("isCrouching", false);
        }

        // Плавное изменение высоты CharacterController
        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            heightLerpSpeed * Time.deltaTime
        );
        float camTargetY = isCrouching ? cameraCrouchHeight : cameraStandHeight;

        cameraHolder.localPosition = Vector3.Lerp(
        cameraHolder.localPosition,
        new Vector3(0f, camTargetY, 0f),
        heightLerpSpeed * Time.deltaTime
        );

        controller.center = new Vector3(
            0f,
            controller.height / 2f,
            0f
        );
    }

    // ================= ANIMATION =================

    void UpdateAnimator()
    {
        float movementAmount = new Vector2(input.x, input.y).magnitude;

        if (gunAnimator != null)
        {
            gunAnimator.SetFloat("Speed", movementAmount);
            gunAnimator.SetBool("IsRunning", IsRunning);
        }
    }
}
