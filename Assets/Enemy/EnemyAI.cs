using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 6f;
    public float stopDistance = 10f;
    public float fireRate = 2f;
    public float damage = 10f;
    public float rayDistance = 100f;
    public float extraGravity = 20f;

    private float fireCooldown = 0f;
    public Transform firePoint;

    private Rigidbody rb;
    public Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null || firePoint == null) return;

        Vector3 direction = (player.position - transform.position);
        direction.y = 0f; // Не двигаемся по высоте
        float distance = direction.magnitude;
        direction.Normalize();

        // Поворачиваемся к игроку
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);

        // Передвижение
        if (distance > stopDistance)
        {
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
            animator.SetBool("isMoving", true);
            animator.SetTrigger("shoot");
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // сохраняем падение
            animator.SetBool("isMoving", false);

            if (fireCooldown <= 0f)
            {
                Fire();
                fireCooldown = fireRate;
            }
        }

        // Кулдаун выстрела
        if (fireCooldown > 0f)
            fireCooldown -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // Добавим дополнительную силу вниз для быстрого падения
        rb.AddForce(Vector3.down * extraGravity);
    }

    void Fire()
    {
        animator.SetTrigger("shoot");

        Ray ray = new Ray(firePoint.position, firePoint.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                PlayerHealth ph = hit.collider.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damage);
                }
            }
        }

        Debug.DrawRay(firePoint.position, firePoint.forward * rayDistance, Color.red, 0.5f);
    }
}
