using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider healthSlider;
    public PlayerHealth playerHealth;

    void Start()
    {
        healthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.currentHealth;
    }

    void Update()
    {
        healthSlider.value = playerHealth.currentHealth;
    }
}
