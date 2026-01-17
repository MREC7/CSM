using UnityEngine;
using System;

public class Target : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float health = 100f;
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip deathSound;
    
    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;
    public GameObject deathEffectPrefab;
    
    [Header("Death Settings")]
    public float destroyDelay = 2f; // Задержка перед уничтожением (для рагдолла/анимации)
    public bool useRagdoll = false;
    
    // Events
    public event Action onDeath;
    public event Action<float> onDamage; // Передаёт количество урона
    
    // Components
    private AudioSource audioSource;
    private Animator animator;
    
    // State
    private bool isDead;
    
    // Animation hashes
    private int hashHit;
    private int hashDeath;
    
    // Optimization
    private float lastHitSoundTime;
    private const float minSoundInterval = 0.1f;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        // Setup audio
        if (audioSource == null && (hitSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        
        // Cache animator hashes
        if (animator != null)
        {
            hashHit = Animator.StringToHash("Hit");
            hashDeath = Animator.StringToHash("Death");
        }
        
        health = maxHealth;
    }
    
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        
        health -= amount;
        health = Mathf.Max(0f, health);
        
        // Invoke damage event
        onDamage?.Invoke(amount);
        
        // Play hit animation
        if (animator != null && health > 0f)
        {
            animator.SetTrigger(hashHit);
        }
        
        // Play hit sound (с ограничением частоты)
        if (Time.time - lastHitSoundTime > minSoundInterval)
        {
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
                lastHitSoundTime = Time.time;
            }
        }
        
        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        
        // Check death
        if (health <= 0f)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        health += amount;
        health = Mathf.Min(health, maxHealth);
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Invoke death event
        onDeath?.Invoke();
        
        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger(hashDeath);
        }
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Spawn death effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        
        // Enable ragdoll if needed
        if (useRagdoll)
        {
            EnableRagdoll();
        }
        
        // Disable scripts and components
        DisableComponents();
        
        // Destroy after delay
        Destroy(gameObject, destroyDelay);
    }
    
    void EnableRagdoll()
    {
        // Disable animator
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Enable all rigidbodies
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
        }
        
        // Enable all colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
    }
    
    void DisableComponents()
    {
        // Disable AI
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }
        
        // Disable NavMeshAgent
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // Disable main collider if not using ragdoll
        if (!useRagdoll)
        {
            Collider mainCollider = GetComponent<Collider>();
            if (mainCollider != null)
            {
                mainCollider.enabled = false;
            }
        }
    }
    
    // Public getters
    public float GetHealthPercent()
    {
        return health / maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
}