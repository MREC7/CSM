using UnityEngine;
using UnityEngine.UI;

public class HPUI : MonoBehaviour
{
    public Image heartFill;        // вместо Slider
    public PlayerHealth playerHealth;

    void Start()
    {
        UpdateHeart();
    }

    void Update()
    {
        UpdateHeart();
    }

    void UpdateHeart()
    {
        float t = playerHealth.currentHealth / playerHealth.maxHealth;

        heartFill.fillAmount = t;

        heartFill.color = Color.Lerp(Color.black, Color.white, t);
    }
}