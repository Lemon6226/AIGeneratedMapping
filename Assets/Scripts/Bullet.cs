using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // když zasáhne nepřítele
        if (collision.transform.TryGetComponent<Enemy_Woman>(out Enemy_Woman enemy))
        {
            enemy.health -= 10;
        }

        // když zasáhne spawner
        if (collision.transform.TryGetComponent<EnemySpawner>(out EnemySpawner spawner))
        {
            spawner.TakeHit();
        }

        // střela se po dopadu zničí
        Destroy(gameObject);
    }
}
