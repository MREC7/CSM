using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform firePoint;
    public Animator animator;
    public Target targetScript;
    
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 8f;
    public float retreatDistance = 5f; // Отступает если игрок слишком близко
    
    [Header("Combat Settings")]
    public float detectionRange = 30f;
    public float fireRange = 25f;
    public float fireRate = 0.3f; // Между выстрелами
    public float burstFireRate = 0.1f; // Внутри очереди
    public int burstCount = 3; // Выстрелов в очереди
    public float damage = 10f;
    public float aimAccuracy = 0.95f; // 0-1, чем выше тем точнее
    
    [Header("Line of Sight")]
    public float visionAngle = 90f;
    public LayerMask obstacleMask = ~0; // По умолчанию все слои
    public bool useObstacleMask = true; // Включить/выключить проверку препятствий
    
    [Header("Audio")]
    public AudioClip shootSound;
    
    // Components
    private NavMeshAgent agent;
    private AudioSource audioSource;
    private Transform cachedTransform;
    
    // State
    private enum EnemyState { Idle, Patrol, Chase, Attack, TakingCover }
    private EnemyState currentState;
    
    // Combat
    private float nextFireTime;
    private int currentBurstShot;
    private float nextBurstTime;
    private bool hasLineOfSight;
    private Vector3 lastKnownPlayerPos;
    
    // Optimization
    private float stateUpdateInterval = 0.2f;
    private float nextStateUpdate;
    private bool isAlive = true;
    
    // Animation hashes (cached)
    private int hashIsMoving;
    private int hashShoot;
    private int hashSpeed;
    
    void Start()
    {
        // Cache components
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        cachedTransform = transform;
        
        // Setup NavMeshAgent
        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        
        // ВАЖНО: Rigidbody должен быть kinematic при использовании NavMeshAgent
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Cache animator hashes
        if (animator != null)
        {
            hashIsMoving = Animator.StringToHash("isMoving");
            hashShoot = Animator.StringToHash("shoot");
            hashSpeed = Animator.StringToHash("Speed");
        }
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Setup audio
        if (audioSource == null && shootSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        
        // Subscribe to death event
        if (targetScript != null)
        {
            targetScript.onDeath += OnDeath;
        }
        
        currentState = EnemyState.Idle;
    }
    
    void Update()
    {
        if (!isAlive || player == null) return;
        
        // Optimize state updates
        if (Time.time >= nextStateUpdate)
        {
            nextStateUpdate = Time.time + stateUpdateInterval;
            UpdateState();
        }
        
        ExecuteState();
        UpdateAnimations();
    }
    
    void UpdateState()
    {
        float distanceToPlayer = Vector3.Distance(cachedTransform.position, player.position);
        hasLineOfSight = CheckLineOfSight();
        
        // State machine
        if (distanceToPlayer <= detectionRange)
        {
            if (hasLineOfSight)
            {
                lastKnownPlayerPos = player.position;
                
                if (distanceToPlayer <= retreatDistance)
                {
                    currentState = EnemyState.TakingCover;
                }
                else if (distanceToPlayer <= fireRange)
                {
                    currentState = EnemyState.Attack;
                }
                else
                {
                    currentState = EnemyState.Chase;
                }
            }
            else if (lastKnownPlayerPos != Vector3.zero)
            {
                // Идём к последней известной позиции
                currentState = EnemyState.Chase;
            }
        }
        else
        {
            currentState = EnemyState.Idle;
            lastKnownPlayerPos = Vector3.zero;
        }
    }
    
    void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                break;
                
            case EnemyState.Chase:
                Vector3 targetPos = hasLineOfSight ? player.position : lastKnownPlayerPos;
                agent.isStopped = false;
                agent.SetDestination(targetPos);
                RotateTowards(player.position);
                break;
                
            case EnemyState.Attack:
                agent.isStopped = true;
                RotateTowards(player.position);
                TryShoot();
                break;
                
            case EnemyState.TakingCover:
                // Отступаем от игрока
                Vector3 retreatDir = (cachedTransform.position - player.position).normalized;
                Vector3 retreatPos = cachedTransform.position + retreatDir * 3f;
                
                agent.isStopped = false;
                agent.SetDestination(retreatPos);
                RotateTowards(player.position);
                TryShoot(); // Стреляем во время отступления
                break;
        }
    }
    
    bool CheckLineOfSight()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = player.position - cachedTransform.position;
        float distance = directionToPlayer.magnitude;
        float angle = Vector3.Angle(cachedTransform.forward, directionToPlayer);
        
        // Проверка угла зрения
        if (angle > visionAngle * 0.5f)
            return false;
        
        // Raycast на препятствия (если включено)
        if (useObstacleMask)
        {
            Vector3 rayStart = cachedTransform.position + Vector3.up * 1.5f; // Стреляем с высоты глаз
            
            if (Physics.Raycast(rayStart, directionToPlayer.normalized, distance, obstacleMask))
            {
                // Попали в препятствие - нет прямой видимости
                return false;
            }
        }
        
        return true;
    }
    
    void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - cachedTransform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    void TryShoot()
    {
        if (!hasLineOfSight) return;
        
        if (Time.time >= nextFireTime)
        {
            if (currentBurstShot < burstCount)
            {
                if (Time.time >= nextBurstTime)
                {
                    Fire();
                    currentBurstShot++;
                    nextBurstTime = Time.time + burstFireRate;
                }
            }
            else
            {
                // Конец очереди, перезарядка
                currentBurstShot = 0;
                nextFireTime = Time.time + fireRate;
            }
        }
    }
    
    void Fire()
    {
        if (firePoint == null) return;
        
        // Анимация
        if (animator != null)
        {
            animator.SetTrigger(hashShoot);
        }
        
        // Звук
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        // Вычисляем точку прицеливания с разбросом
        Vector3 targetPoint = player.position + Vector3.up * 1.5f; // Целимся в центр масс
        Vector3 aimDirection = (targetPoint - firePoint.position).normalized;
        
        // Добавляем неточность
        float inaccuracy = 1f - aimAccuracy;
        aimDirection += new Vector3(
            Random.Range(-inaccuracy, inaccuracy),
            Random.Range(-inaccuracy * 0.5f, inaccuracy * 0.5f),
            Random.Range(-inaccuracy, inaccuracy)
        );
        aimDirection.Normalize();
        
        // Raycast
        Ray ray = new Ray(firePoint.position, aimDirection);
        if (Physics.Raycast(ray, out RaycastHit hit, fireRange))
        {
            // Проверяем попадание в игрока
            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            }
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            Debug.DrawRay(firePoint.position, aimDirection * hit.distance, Color.red, 0.3f);
        }
        else
        {
            Debug.DrawRay(firePoint.position, aimDirection * fireRange, Color.yellow, 0.3f);
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        float speed = agent.velocity.magnitude / moveSpeed;
        
        animator.SetBool(hashIsMoving, isMoving);
        animator.SetFloat(hashSpeed, speed);
    }
    
    void OnDeath()
    {
        isAlive = false;
        agent.isStopped = true;
        enabled = false;
    }
    
    void OnDestroy()
    {
        if (targetScript != null)
        {
            targetScript.onDeath -= OnDeath;
        }
    }
    
    // Debug визуализация
    void OnDrawGizmosSelected()
    {
        // Дистанция обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Дистанция стрельбы
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fireRange);
        
        // Угол зрения
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}