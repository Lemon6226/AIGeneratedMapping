using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int maxHealth = 5;
    public int currentHealth;

    // každých 10 sekund spawne nepřítele
    public float spawnInterval = 10f;
    private float timer;

    // limit aktivních nepřátel na spawner aby hra nebyla nehratelná
    public int maxActiveEnemies = 5;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    public GameObject enemyPrefab;

    private void Start()
    {
        currentHealth = maxHealth;
        timer = spawnInterval;

        if (SpawnerController.Instance != null)
            SpawnerController.Instance.RegisterSpawner(this);
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // odstranění mrtvých nepřátel ze seznamu
        spawnedEnemies.RemoveAll(e => e == null);

        // spawn přeskočen pokud je dosažen limit aktivních nepřátel
        if (spawnedEnemies.Count >= maxActiveEnemies) return;

        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        spawnedEnemies.Add(enemy);
    }

    public void TakeHit()
    {
        currentHealth--;

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (SpawnerController.Instance != null)
            SpawnerController.Instance.UnregisterSpawner(this);

        Destroy(gameObject);
    }
}
