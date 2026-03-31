using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;

    // regenerace zdraví, začne se po uplynutí regenDelay sekund od posledního zásahu
    public float healthRegen = 10f;
    public float regenDelay = 5f;
    public bool canRegenerate = true;
    public bool tookDamage = false;

    private float timeSinceLastDamage = 0f;

    public Image healthProgressUI;
    public CanvasGroup sliderCanvasGroup;

    private void Awake()
    {
        // automatické hledání UI elementů pokud nejsou přiřazené
        if (healthProgressUI == null)
            healthProgressUI = GameObject.FindGameObjectWithTag("HealthUI")?.GetComponent<Image>();

        if (sliderCanvasGroup == null)
            sliderCanvasGroup = GameObject.FindGameObjectWithTag("HealthCanvas")?.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        // měření doby od posledního zásahu, pak vypnutí tookDamage
        if (tookDamage)
        {
            timeSinceLastDamage += Time.deltaTime;
            if (timeSinceLastDamage >= regenDelay)
                tookDamage = false;
        }

        // regenerace zdraví pokud není aktivní damage flag
        if (canRegenerate && !tookDamage && health < maxHealth)
        {
            health += healthRegen * Time.deltaTime;
            health = Mathf.Clamp(health, 0, maxHealth);
            UpdateHealthUI();
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        tookDamage = true;
        timeSinceLastDamage = 0f;
        UpdateHealthUI();

        if (health <= 0)
        {
            GameManager.Instance.LoseGame();
        }
    }

    public void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthProgressUI != null)
            healthProgressUI.fillAmount = health / maxHealth;

        sliderCanvasGroup.alpha = 1;
    }
}
