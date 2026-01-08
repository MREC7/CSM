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
    public int bulletsamount = 30;
    public int totalbulletscount = 60;
    public int defbulletsamount = 30;
    public float reloadTime = 3f;
    private bool isReloading = false;

    private Coroutine flashRoutine;
    private Coroutine reloadroutine;

    void Start()
    {
        cameraShake = FindObjectOfType<CameraShake>();
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
        else if (bulletsamount == 0 && !isReloading && totalbulletscount > 0)
        {          
            Reloading();
        }
    }
    void Reloading()
    {
        reloadroutine = StartCoroutine(ReloadingRoutine());
    }
    IEnumerator ReloadingRoutine()
    {
        isReloading = true;
        gunAnimator.SetTrigger("Reload");
        yield return new WaitForSeconds(reloadTime);
        totalbulletscount -= (defbulletsamount - bulletsamount);
        bulletsamount += (defbulletsamount - bulletsamount);
        isReloading = false;
    }

    void Shoot()
    {
        PlayMuzzleLight();
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (gunAudio != null && gunShotSound != null)
            gunAudio.PlayOneShot(gunShotSound);
        gunAnimator.SetTrigger("Shoot");
        cameraShake.Shake();


        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 1f);
            }

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
    }

    void PlayMuzzleLight()
    {
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
    void HandleReload() {
        if (Input.GetKeyDown(KeyCode.R) && gunAnimator != null && bulletsamount != defbulletsamount && totalbulletscount > 0)
        {
            Reloading();
        }

    }
}
