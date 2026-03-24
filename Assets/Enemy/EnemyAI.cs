// UPGRADED EnemyAI (animation-independent shooting + improvements)
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // ================= COMPONENTS =================
    public NavMeshAgent agent;
    public Transform player;
    private PlayerHealth playerHealth;
    public Animator anim;
    public AudioSource audioSource;

    // ================= SETTINGS =================
    public EnemyType enemyType;
    public enum EnemyType { Melee, Ranged }

    [Header("Vision")]
    public float viewAngle = 120f;
    public float viewDistance = 20f;
    public float detectionTime = 0.2f;
    public float losePlayerTime = 3f;

    [Header("Combat")]
    public float attackRange = 12f;
    public float meleeRange = 2.5f;
    public float minAttackDistance = 5f;
    public float maxAttackDistance = 12f;
    public float fireRate = 1.5f;
    public float damage = 10f;

    [Header("Gun")]
    public Transform shootPoint;
    public float fireRange = 50f;
    public float spread = 0.03f;
    public LayerMask hitMask;
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;

    [Header("Advanced AI")]
    public float strafeRadius = 4f;
    public float strafeChangeTime = 1.2f;
    public float dodgeChance = 0.15f;
    public float dashDistance = 4f;

    [Header("Shooting Mode")]
    public bool useAnimationForShooting = false; // 🔥 NEW

    [Header("Stats")]
    public int health = 100;

    [Header("Patrol")]
    public Transform[] patrolPoints;

    // ================= STATE =================
    private float fireCooldown;
    private float detectTimer;
    private float strafeTimer;
    private float loseTimer;

    private bool playerDetected;
    private bool isDead;

    private Vector3 lastKnownPlayerPos;
    private Vector3 strafeTarget;
    private int currentPatrolIndex;

    public enum EnemyState { Patrol, Chase, Attack, Search, Dead }
    private EnemyState currentState;

    // ================= INIT =================
    void Awake()
    {
        InitPlayer();
        InitComponents();
        SetupAgent();
    }

    void InitPlayer()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");

        if (playerGO == null)
        {
            Debug.LogError("[EnemyAI] Player not found!");
            return;
        }

        player = playerGO.transform;
        playerHealth = playerGO.GetComponent<PlayerHealth>();
    }

    void InitComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void SetupAgent()
    {
        agent.updateRotation = false;
        agent.acceleration = 25f;
        agent.angularSpeed = 720f;
        agent.speed = 4.5f;
    }

    void Update()
    {
        if (isDead) return;

        UpdatePerception();
        UpdateState();
        ExecuteState();
        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        float speed = agent.velocity.magnitude;
        anim.SetFloat("Speed", speed);
        anim.SetBool("IsMoving", speed > 0.1f);
    }

    // ================= PERCEPTION =================
    void UpdatePerception()
    {
        if (CanSeePlayer())
        {
            detectTimer += Time.deltaTime;
            loseTimer = 0f;

            if (detectTimer >= detectionTime)
            {
                playerDetected = true;
                lastKnownPlayerPos = player.position;
            }
        }
        else
        {
            detectTimer = 0f;

            if (playerDetected)
            {
                loseTimer += Time.deltaTime;
                if (loseTimer >= losePlayerTime)
                    playerDetected = false;
            }
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up;
        Vector3 dir = target - origin;

        if (dir.magnitude > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, viewDistance, ~LayerMask.GetMask("Enemy")))
        {
            return hit.collider.transform.root == player.root;
        }

        return false;
    }

    // ================= FSM =================
    void UpdateState()
    {
        if (health <= 0)
        {
            currentState = EnemyState.Dead;
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (playerDetected && dist <= attackRange)
            currentState = EnemyState.Attack;
        else if (playerDetected)
            currentState = EnemyState.Chase;
        else if (patrolPoints.Length > 0)
            currentState = EnemyState.Patrol;
        else
            currentState = EnemyState.Search;
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol: Patrol(); break;
            case EnemyState.Chase: Chase(); break;
            case EnemyState.Attack: Attack(); break;
            case EnemyState.Search: MoveTo(lastKnownPlayerPos); break;
            case EnemyState.Dead: HandleDeath(); break;
        }
    }

    // ================= MOVEMENT =================
    void MoveTo(Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void SmoothLook(Vector3 dir)
    {
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 10f);
    }

    void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        agent.isStopped = false;

        Transform target = patrolPoints[currentPatrolIndex];
        MoveTo(target.position);
        SmoothLook(agent.velocity);

        if (!agent.pathPending && agent.remainingDistance < 1.5f)
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    void Chase()
    {
        agent.isStopped = false;
        MoveTo(player.position);
        SmoothLook(player.position - transform.position);
    }

    // ================= ATTACK =================
    void Attack()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        SmoothLook(dir);

        float dist = Vector3.Distance(transform.position, player.position);

        if (enemyType == EnemyType.Ranged)
            HandleRanged(dist, dir);
        else
            HandleMelee(dist);
    }

    void HandleRanged(float dist, Vector3 dir)
    {
        if (dist < minAttackDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(transform.position - dir * 3f);
            return;
        }

        if (dist > maxAttackDistance)
        {
            agent.isStopped = false;
            MoveTo(player.position);
            return;
        }

        agent.isStopped = true;
        RangedAttack();
        TryDodge();
    }

    void HandleMelee(float dist)
    {
        agent.isStopped = false;
        MeleeAttack();
        TryDodge();
    }

    void TryDodge()
    {
        if (Random.value < dodgeChance * Time.deltaTime)
        {
            Vector3 dir = Vector3.Cross(Vector3.up, (player.position - transform.position).normalized);
            MoveTo(transform.position + dir * dashDistance);
        }
    }

    // ================= RANGED =================
    void RangedAttack()
    {
        fireCooldown -= Time.deltaTime;

        if (fireCooldown <= 0f)
        {
            if (useAnimationForShooting && HasAnimatorParam("Shoot"))
            {
                anim.SetTrigger("Shoot");
            }
            else
            {
                ShootHitscan(); // 🔥 instant shot (no animation needed)
            }

            fireCooldown = 1f / fireRate;
        }
    }

    public void OnShoot() => ShootHitscan(); // optional animation event

    void ShootHitscan()
    {
        if (shootPoint == null)
        {
            Debug.LogError("[EnemyAI] shootPoint not assigned!");
            return;
        }

        muzzleFlash?.Play();
        audioSource?.PlayOneShot(shootSound);

        Vector3 dir = (player.position + Vector3.up - shootPoint.position).normalized;
        dir += Random.insideUnitSphere * spread;

        if (Physics.Raycast(shootPoint.position, dir, out RaycastHit hit, fireRange, hitMask))
        {
            PlayerHealth ph = hit.collider.GetComponentInParent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damage);
        }
    }

    // ================= MELEE =================
    void MeleeAttack()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > meleeRange)
        {
            MoveTo(player.position);
            return;
        }

        fireCooldown -= Time.deltaTime;

        if (fireCooldown <= 0f)
        {
            ApplyMeleeDamage();
            fireCooldown = 1f / fireRate;
        }
    }

    void ApplyMeleeDamage()
    {
        if (Vector3.Distance(transform.position, player.position) <= meleeRange * 1.3f)
            playerHealth?.TakeDamage(damage);
    }

    // ================= DAMAGE =================
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        health -= dmg;
        anim.SetTrigger("Hit");

        playerDetected = true;
        lastKnownPlayerPos = player.position;
        loseTimer = 0f;

        if (health <= 0)
            HandleDeath();
    }

    void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;

        if (HasAnimatorParam("Die")) anim.SetTrigger("Die");

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Destroy(gameObject, 3f);
    }

    // ================= HELPERS =================
    bool HasAnimatorParam(string name)
    {
        if (anim == null) return false;

        foreach (var p in anim.parameters)
            if (p.name == name) return true;

        return false;
    }
}