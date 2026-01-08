using UnityEngine;
using UnityEngine.UI;

public class DamageEffect : MonoBehaviour
{
    public Image damageOverlay;
    public float duration = 0.5f;
    public Color flashColor = new Color(1, 0, 0, 0.5f); // красный с альфой
    private Color transparentColor;
    private float timer;

    void Start()
    {
        transparentColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
        damageOverlay.color = transparentColor;
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            damageOverlay.color = Color.Lerp(flashColor, transparentColor, 1 - (timer / duration));
        }
    }

    public void ShowDamage()
    {
        timer = duration;
        damageOverlay.color = flashColor;
    }
}
