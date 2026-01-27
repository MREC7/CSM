using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
    public Camera cam;
    public float range = 100f;
    public float damage = 10f;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject hitEffectPrefab;
    public AudioSource gunAudio;
    public AudioClip gunShotSound;

    [Header("Firing Settings")]
    public float fireRate = 0.1f;
    private float nextTimeToFire = 0f;
    public Animator gunAnimator;
    private CameraShake cameraShake;
    public Light muzzleLight;
    public float lightDuration = 0.05f;

    [Header("Ammo Settings")]
    public int bulletsamount = 2;
    public int totalbulletscount = 30;
    public int defbulletsamount = 2;
    public float reloadTime = 2f;
    private bool isReloading = false;

    private Coroutine flashRoutine;

    void Start()
    {
        cameraShake = FindObjectOfType<CameraShake>();
        
        // Инициализация: выключаем свет в начале
        if (muzzleLight != null)
            muzzleLight.enabled = false;
    }

    void Update()
    {
        HandleReload();
        HandleFire();
    }

    void HandleFire()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire && bulletsamount > 0 && !isReloading)
        {
            nextTimeToFire = Time.time + fireRate;
            bulletsamount -= 1;
            Shoot();
        }
    }

    void HandleReload()
    {
        // Автоматическая перезарядка при пустом магазине
        if (bulletsamount == 0 && !isReloading && totalbulletscount > 0)
        {
            StartReloading();
        }
        // Ручная перезарядка по кнопке R
        else if (Input.GetKeyDown(KeyCode.R) && !isReloading && bulletsamount < defbulletsamount && totalbulletscount > 0)
        {
            StartReloading();
        }
    }

    void StartReloading()
    {
        if (!isReloading)
        {
            StartCoroutine(ReloadingRoutine());
        }
    }

    IEnumerator ReloadingRoutine()
    {
        isReloading = true;
        
        if (gunAnimator != null)
            gunAnimator.SetTrigger("Reload");

        yield return new WaitForSeconds(reloadTime);

        // Вычисляем сколько патронов нужно добавить
        int bulletsNeeded = defbulletsamount - bulletsamount;
        
        // Берём минимум из того что нужно и того что есть
        int bulletsToReload = Mathf.Min(bulletsNeeded, totalbulletscount);
        
        // Обновляем количество патронов
        bulletsamount += bulletsToReload;
        totalbulletscount -= bulletsToReload;

        isReloading = false;
    }

    void Shoot()
    {
        PlayMuzzleLight();

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (gunAudio != null && gunShotSound != null)
            gunAudio.PlayOneShot(gunShotSound);

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Shoot");

        if (cameraShake != null)
            cameraShake.Shake();

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            // Создаём эффект попадания
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 1f);
            }

            // Проверяем урон по цели
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
    }

    void PlayMuzzleLight()
    {
        if (muzzleLight == null)
            return;

        // Останавливаем предыдущую корутину если она есть
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(MuzzleFlashRoutine());
    }

    IEnumerator MuzzleFlashRoutine()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }
}