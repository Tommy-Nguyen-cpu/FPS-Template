using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.name.Contains("Enemy"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            EnemyController controller = collision.gameObject.GetComponent<EnemyController>();

            controller.BulletHit();

            rb.AddExplosionForce(5000f, transform.position, 5f);
        }

        gameObject.SetActive(false);
    }
}
