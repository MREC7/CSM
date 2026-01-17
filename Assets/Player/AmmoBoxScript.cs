using UnityEngine;

public class AmmoBoxScript : MonoBehaviour
{
    [Header("Ammo Settings")]
    public Gun gun;
    public int ammoCount = 30;
    
    [Header("Effects")]
    public AmmoEffect ammoEffect;
    public AudioClip pickupSound;
    
    [Header("Optional Settings")]
    public float destroyDelay = 0f; // Задержка перед уничтожением (для звука/эффектов)
    
    // Cached components
    private AudioSource audioSource;
    private bool hasBeenCollected;
    
    // Константы
    private const string PlayerTag = "Player";
    
    void Start()
    {
        // Кэшируем AudioSource если есть звук
        if (pickupSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Используем OnTriggerEnter вместо OnCollisionEnter
        // Это более оптимально для подбираемых предметов
        
        if (hasBeenCollected)
            return;
            
        // Проверяем сам объект, родителя и корневой объект
        if (IsPlayer(other.gameObject))
        {
            CollectAmmo();
        }
    }
    
    // Для обратной совместимости, если у вас уже настроены коллизии
    void OnCollisionEnter(Collision collision)
    {
        if (hasBeenCollected)
            return;
            
        if (IsPlayer(collision.gameObject))
        {
            CollectAmmo();
        }
    }
    
    // Проверка игрока - ищет по тегу, PlayerController или CharacterController
    bool IsPlayer(GameObject obj)
    {
        // Проверяем сам объект
        if (obj.CompareTag(PlayerTag))
            return true;
        
        // Проверяем наличие PlayerController на объекте
        if (obj.GetComponent<PlayerController>() != null)
            return true;
        
        // Проверяем родителя
        Transform parent = obj.transform.parent;
        if (parent != null)
        {
            if (parent.CompareTag(PlayerTag))
                return true;
                
            if (parent.GetComponent<PlayerController>() != null)
                return true;
        }
        
        // Проверяем корневой объект (root)
        Transform root = obj.transform.root;
        if (root != null && root != obj.transform)
        {
            if (root.CompareTag(PlayerTag))
                return true;
                
            if (root.GetComponent<PlayerController>() != null)
                return true;
        }
        
        // Проверяем CharacterController в иерархии
        CharacterController controller = obj.GetComponentInParent<CharacterController>();
        if (controller != null)
            return true;
        
        return false;
    }
    
    void CollectAmmo()
    {
        hasBeenCollected = true;
        
        // Добавляем патроны
        if (gun != null)
        {
            gun.totalbulletscount += ammoCount;
        }
        else
        {
            Debug.LogWarning("AmmoBoxScript: Gun reference is missing!");
        }
        
        // Показываем эффект
        if (ammoEffect != null)
        {
            ammoEffect.ShowEffect();
        }
        
        // Воспроизводим звук
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }
        
        // Уничтожаем объект
        if (destroyDelay > 0f)
        {
            // Скрываем визуал, но оставляем объект для проигрывания звука
            HideVisuals();
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void HideVisuals()
    {
        // Отключаем рендереры и коллайдеры
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }
}