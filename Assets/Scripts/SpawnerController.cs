using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SpawnerController : MonoBehaviour
{
    public static SpawnerController Instance;

    public Image spawnerBarUI;
    public CanvasGroup spawnerCanvasGroup;

    private List<EnemySpawner> activeSpawners = new List<EnemySpawner>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this.gameObject);
    }

    public void RegisterSpawner(EnemySpawner spawner)
    {
        if (!activeSpawners.Contains(spawner))
            activeSpawners.Add(spawner);

        UpdateUI();
    }

    public void UnregisterSpawner(EnemySpawner spawner)
    {
        if (activeSpawners.Contains(spawner))
            activeSpawners.Remove(spawner);

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (spawnerBarUI == null) return;

        // celkové zdraví všech spawnerů dohromady, zobrazení jako progress bar
        float totalSpawners = Mathf.Max(1, MapGeneratorSpawnerCount.cachedSpawnerCount);
        float healthSum = 0f;

        foreach (var spawner in activeSpawners)
        {
            healthSum += (float)spawner.currentHealth / spawner.maxHealth;
        }

        float fillAmount = Mathf.Clamp01(healthSum / totalSpawners);
        spawnerBarUI.fillAmount = fillAmount;

        if (spawnerCanvasGroup != null)
            spawnerCanvasGroup.alpha = 1f;

        // všechny spawnery zničeny = výhra
        if (activeSpawners.Count == 0)
        {
            GameManager.Instance.WinGame();
        }
    }
}
