using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerController playerMovement;

    private Vector3 initialCamPos;
    private float bobTimer;

    [Header("Bob Settings")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 18f;
    public float runBobAmount = 0.1f;

    void Start()
    {
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerController>();

        initialCamPos = cameraTransform.localPosition;
    }

    void Update()
    {
        bool isMoving = playerMovement.IsMoving;
        bool isRunning = playerMovement.IsRunning;

        if (isMoving)
        {
            float speed = isRunning ? runBobSpeed : walkBobSpeed;
            float amount = isRunning ? runBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * speed;
            float yOffset = Mathf.Sin(bobTimer) * amount;

            cameraTransform.localPosition = initialCamPos + new Vector3(0f, yOffset, 0f);
        }
        else
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, initialCamPos, Time.deltaTime * 5f);
        }
    }
}
