using UnityEngine;
using UnityEngine.UI;

public class AmmoEffect : MonoBehaviour
{
    public Image ammoOverlay;
    public float duration = 0.5f;
    public Color flashColor = new Color(0, 255, 41, 0.20f); // красный с альфой
    private Color transparentColor;
    private float timer;

    void Start()
    {
        transparentColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0);
        ammoOverlay.color = transparentColor;
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            ammoOverlay.color = Color.Lerp(flashColor, transparentColor, 1 - (timer / duration));
        }
    }

    public void ShowEffect()
    {
        timer = duration;
        ammoOverlay.color = flashColor;
    }
}
