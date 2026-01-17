using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerController playerMovement;
    
    private Vector3 initialCamPos;
    private float bobTimer;
    
    [Header("Bob Settings")]
    public float runBobSpeed = 16f;
    public float runBobAmount = 0.08f;
    
    [Header("Dash Settings")]
    public float dashBobSpeed = 25f;
    public float dashBobAmount = 0.15f;
    public float dashTiltAmount = 2f; // Небольшой наклон камеры во время рывка
    
    [Header("Crouch Settings")]
    public float crouchBobSpeed = 10f;
    public float crouchBobAmount = 0.03f;
    
    [Header("Smooth Settings")]
    public float returnSpeed = 8f;
    
    // Cached values
    private float currentTilt;
    private float targetTilt;
    
    void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerController>();
        }
        
        if (cameraTransform == null)
        {
            cameraTransform = transform;
        }
        
        initialCamPos = cameraTransform.localPosition;
    }
    
    void Update()
    {
        bool isMoving = playerMovement.IsMoving;
        bool isDashing = playerMovement.IsDashing;
        
        if (isMoving || isDashing)
        {
            ApplyHeadBob(isDashing);
        }
        else
        {
            ResetHeadBob();
        }
    }
    
    void ApplyHeadBob(bool isDashing)
    {
        float speed, amount;
        
        if (isDashing)
        {
            // Интенсивная тряска во время рывка
            speed = dashBobSpeed;
            amount = dashBobAmount;
            targetTilt = dashTiltAmount;
        }
        else
        {
            // Обычный бег (без отдельной ходьбы)
            speed = runBobSpeed;
            amount = runBobAmount;
            targetTilt = 0f;
        }
        
        bobTimer += Time.deltaTime * speed;
        
        // Синусоидальное покачивание
        float yOffset = Mathf.Sin(bobTimer) * amount;
        
        // Плавный наклон камеры
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 10f);
        
        // Применяем позицию и вращение
        cameraTransform.localPosition = initialCamPos + new Vector3(0f, yOffset, 0f);
        
        // Небольшой Z-наклон во время рывка для динамики
        if (isDashing)
        {
            float zTilt = Mathf.Sin(bobTimer * 0.5f) * currentTilt;
            cameraTransform.localRotation = Quaternion.Euler(
                cameraTransform.localRotation.eulerAngles.x,
                0f,
                zTilt
            );
        }
    }
    
    void ResetHeadBob()
    {
        bobTimer = 0f;
        targetTilt = 0f;
        
        // Плавное возвращение к исходной позиции
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, 
            initialCamPos, 
            Time.deltaTime * returnSpeed
        );
        
        // Возврат наклона
        currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * 10f);
        
        if (Mathf.Abs(currentTilt) > 0.01f)
        {
            cameraTransform.localRotation = Quaternion.Lerp(
                cameraTransform.localRotation,
                Quaternion.identity,
                Time.deltaTime * 10f
            );
        }
        else
        {
            cameraTransform.localRotation = Quaternion.identity;
        }
    }
}