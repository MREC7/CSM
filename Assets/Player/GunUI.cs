using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GunUI : MonoBehaviour
{
    public Gun Gun;
    public TMP_Text text;

    void Start()
    {
        text.text = Gun.bulletsamount.ToString() + "/" + Gun.totalbulletscount.ToString();
    }

    void Update()
    {
    	text.text = Gun.bulletsamount.ToString() + "/" + Gun.totalbulletscount.ToString();
    }
}
