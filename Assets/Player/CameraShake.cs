using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.1f;

    private Vector3 initialPos;
    private float shakeTimer = 0f;

    void Start()
    {
        initialPos = transform.localPosition;
    }

    void Update()
    {
        if (shakeTimer > 0)
        {
            transform.localPosition = initialPos + Random.insideUnitSphere * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPos, Time.deltaTime * 5f);
        }
    }

    public void Shake()
    {
        shakeTimer = shakeDuration;
    }
}
