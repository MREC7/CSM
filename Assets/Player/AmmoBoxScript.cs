using UnityEngine;

public class AmmoBoxScript : MonoBehaviour
{
    public Gun Gun;
    public int ammocount = 30;
    public AmmoEffect ammoEffect;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (ammoEffect != null)
            {
                ammoEffect.ShowEffect();
            }
            Gun.totalbulletscount += ammocount;
            Destroy(gameObject);
        }
    }
}
