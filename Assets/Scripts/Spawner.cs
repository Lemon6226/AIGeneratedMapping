using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// starý spawner, už se nepoužívá (nahrazeno EnemySpawnerem)
public class Spawner : MonoBehaviour
{
    public GameObject enemy;
    public float timer;

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            timer = 10;
            Instantiate(enemy, Random.Range(-10f, 10f) * Vector3.right + Random.Range(-10f, 10f) * Vector3.forward, Quaternion.identity);
        }
    }
}
