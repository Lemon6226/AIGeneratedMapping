using UnityEngine;
using UnityEngine.UI;

public class StaminaControler : MonoBehaviour
{
    public float stamina = 100.0f;
    public float maxStamina = 100.0f;

    public bool allowRegen = true;
    public bool hasRegenerated = true;
    public bool weAreSprinting = false;

    // kolik staminy ubývá/přibývá za sekundu
    private float staminaDrain = 25f;
    private float staminaRegen = 20f;

    // zpomalení po vyčerpání staminy
    private int slowedRunSpeed = 4;
    private int normalRunSpeed = 8;

    public Image staminaProgressUI;
    public CanvasGroup sliderCanvasGroup;

    private Player playerControler;

    private void Awake()
    {
        if (staminaProgressUI == null)
            staminaProgressUI = GameObject.FindGameObjectWithTag("StaminaUI")?.GetComponent<Image>();

        if (sliderCanvasGroup == null)
            sliderCanvasGroup = GameObject.FindGameObjectWithTag("StaminaCanvas")?.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        playerControler = GetComponent<Player>();
    }

    private void Update()
    {
        // regenerace staminy když není aktivní sprint
        if (allowRegen && !weAreSprinting)
        {
            if (stamina < maxStamina)
            {
                stamina += staminaRegen * Time.deltaTime;
                stamina = Mathf.Clamp(stamina, 0, maxStamina);
                UpdateStamina();

                hasRegenerated = true;
                playerControler.SetRunSpeed(normalRunSpeed);
            }
        }
    }

    public void Sprinting()
    {
        // sprint povolený jen při dostupné stamině
        if (hasRegenerated)
        {
            weAreSprinting = true;
            stamina -= staminaDrain * Time.deltaTime;
            stamina = Mathf.Clamp(stamina, 0, maxStamina);
            UpdateStamina();

            // stamina došla = zpomalení hráče a zákaz sprintu dokud se nedobije
            if (stamina <= 0)
            {
                hasRegenerated = false;
                playerControler.SetRunSpeed(slowedRunSpeed);
                sliderCanvasGroup.alpha = 1;
            }
        }
    }

    public void UpdateStamina()
    {
        if (staminaProgressUI != null)
            staminaProgressUI.fillAmount = stamina / maxStamina;

        sliderCanvasGroup.alpha = 1;
    }

    public void AddStamina(float amount)
    {
        stamina += amount;
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        UpdateStamina();
    }
}
