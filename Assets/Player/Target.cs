using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 100f;
    public AudioClip hitSound; // звук при попадании

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (audioSource != null && hitSound != null && !audioSource.isPlaying)
        {       
            audioSource.PlayOneShot(hitSound);
        }


        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
