using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;

    public LayerMask groundMask, playerMask, obstacleMask;

    [Header("Vision")]
    public float viewAngle = 90f;
    public float viewDistance = 20f;
    public float detectionTime = 0.3f;

    [Header("Ranges")]
    public float attackRange = 10f;

    [Header("Combat")]
    public float fireRate = 1f;
    public GameObject projectile;
    public Transform shootPoint;

    [Header("Movement")]
    public float patrolRadius = 10f;
    public float searchDuration = 3f;

    [Header("Stats")]
    public int health = 100;

    private EnemyState currentState;

    private float fireCooldown;
    private float searchTimer;

    private float detectTimer;
    private bool playerDetected;

    private Vector3 lastKnownPlayerPos;
    private Vector3 patrolPoint;
    private bool patrolPointSet;

    private bool playerInAttackRange;

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Search,
        Dead
    }

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        UpdatePerception();
        UpdateState();
        ExecuteState();
    }

    // ================= PERCEPTION =================

    void UpdatePerception()
    {
        bool canSee = CanSeePlayer();

        if (canSee)
        {
            detectTimer += Time.deltaTime;

            if (detectTimer >= detectionTime)
            {
                playerDetected = true;
                lastKnownPlayerPos = player.position;
            }
        }
        else
        {
            detectTimer = 0f;
        }

        playerInAttackRange = playerDetected &&
            Vector3.Distance(transform.position, player.position) < attackRange;
    }

    bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dirToPlayer = (player.position - origin).normalized;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle < viewAngle / 2f)
        {
            float distance = Vector3.Distance(origin, player.position);

            if (distance < viewDistance)
            {
                if (!Physics.Raycast(origin, dirToPlayer, distance, obstacleMask))
                {
                    return true;
                }
            }
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

        if (playerDetected && playerInAttackRange)
        {
            currentState = EnemyState.Attack;
        }
        else if (playerDetected)
        {
            currentState = EnemyState.Chase;
        }
        else if (!playerDetected && searchTimer > 0)
        {
            currentState = EnemyState.Search;
        }
        else
        {
            currentState = EnemyState.Patrol;
        }
    }

    void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                Chase();
                break;

            case EnemyState.Attack:
                Attack();
                break;

            case EnemyState.Search:
                Search();
                break;

            case EnemyState.Dead:
                Die();
                break;
        }
    }

    // ================= STATES =================

    void Patrol()
    {
        if (!patrolPointSet)
            SetPatrolPoint();

        agent.SetDestination(patrolPoint);

        if (Vector3.Distance(transform.position, patrolPoint) < 1.5f)
            patrolPointSet = false;
    }

    void Chase()
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(player.position, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        searchTimer = searchDuration;
    }

    void Attack()
    {
        agent.SetDestination(transform.position);

        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0;

        transform.forward = Vector3.Lerp(transform.forward, lookDir, Time.deltaTime * 10f);

        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
        }

        fireCooldown -= Time.deltaTime;
    }

    void Search()
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(lastKnownPlayerPos, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        searchTimer -= Time.deltaTime;
    }

    void Die()
    {
        agent.enabled = false;
        Destroy(gameObject, 2f);
    }

    // ================= ACTIONS =================

    void Shoot()
    {
        Vector3 shootDir = (player.position - shootPoint.position).normalized;

        GameObject bullet = Instantiate(projectile, shootPoint.position, Quaternion.LookRotation(shootDir));

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootDir * 40f, ForceMode.Impulse);
    }

    void SetPatrolPoint()
    {
        float randX = Random.Range(-patrolRadius, patrolRadius);
        float randZ = Random.Range(-patrolRadius, patrolRadius);

        Vector3 randomPoint = new Vector3(
            transform.position.x + randX,
            transform.position.y,
            transform.position.z + randZ
        );

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
            patrolPointSet = true;
        }
    }

    // ================= DAMAGE =================

    public void TakeDamage(int dmg)
    {
        health -= dmg;

        if (health > 0)
        {
            playerDetected = true;
            lastKnownPlayerPos = player.position;
            currentState = EnemyState.Chase;
        }
    }
}