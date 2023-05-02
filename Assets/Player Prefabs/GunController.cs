using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public float bulletSpeed = 20f;
    public int numberOfBullets = 5;
    public GameObject bulletPrefab;
    public static List<GameObject> bullets = new List<GameObject>();
    public GameObject Player { private get; set; }


    private void Start()
    {
        for (int i = 0; i < numberOfBullets; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bullets.Add(bullet);
        }
    }
    private void OnEnable()
    {
        // Bullets should be disabled by default.
        //EnableDisableBullets(true);
    }

    // TODO: Sometimes there's a delay in shooting. Not entirely sure why.
    public void ShootBullet()
    {
        GameObject inactiveBullet = GetInactiveBullet();

        if(inactiveBullet != null)
        {
            inactiveBullet.transform.rotation = Player.transform.rotation;
            inactiveBullet.transform.position = gameObject.transform.position + Player.transform.forward;
            inactiveBullet.SetActive(true);
            inactiveBullet.GetComponent<Rigidbody>().velocity = Player.transform.forward * bulletSpeed;
        }
    }

    public GameObject GetInactiveBullet()
    {
        foreach(var bullet in bullets)
        {
            if (!bullet.activeInHierarchy)
            {
                return bullet;
            }
        }

        return null;
    }
}
