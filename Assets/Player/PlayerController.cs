using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2f;
    public float jumpForce = 7f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float normalHeight = 2f;
    public Transform playerBody;

    [Header("Mouse Look Settings")]
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    private Rigidbody rb;
    private Vector2 input;
    private float rotationX = 0f;
    public bool IsGrounded { get; private set; }
    private bool isCrouching = false;

    private float currentSpeed;
    public Animator animator;
    public Animator gunAnimator;
    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null)
            playerBody = this.transform;
    }

    void Update()
    { 
        HandleMouseLook();
        HandleJump();
        HandleSpeed();
        HandleCrouch();
        float movementAmount = new Vector2(input.x, input.y).magnitude;

        if (gunAnimator != null)
        {
            gunAnimator.SetFloat("Speed", movementAmount);
            gunAnimator.SetBool("IsRunning", Input.GetKey(KeyCode.LeftShift));
        }
        IsMoving = input.magnitude > 0.1f;
        IsRunning = Input.GetKey(KeyCode.LeftShift);
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f); // проверь длину в зависимости от размера игрока
    }
    void FixedUpdate()
    {
        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        Vector3 newPosition = rb.position + move * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            IsGrounded = false;
        }
    }

    void HandleSpeed()
    {
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;
    }

    void HandleCrouch()
    {   
    if (Input.GetKeyDown(KeyCode.LeftControl))
    {
        isCrouching = true;
        SetHeight(crouchHeight);
        if (animator != null)
            animator.SetBool("isCrouching", true);
    }
    else if (Input.GetKeyUp(KeyCode.LeftControl))
    {
        isCrouching = false;
        SetHeight(normalHeight);
        if (animator != null)
            animator.SetBool("isCrouching", false);
    }
    }

    void SetHeight(float height)
    {
        Vector3 scale = playerBody.localScale;
        scale.y = height / 2f;
        playerBody.localScale = scale;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Angle(contact.normal, Vector3.up) < 45f)
                IsGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        IsGrounded = false;
    }
}