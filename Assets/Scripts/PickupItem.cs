using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PickupType
{
    Health,
    Energy,
    Both
}

public class PickupItem : MonoBehaviour
{
    public PickupType type;
    public float healthAmount = 25f;
    public float energyAmount = 25f;

    private bool playerInside = false;
    private Player player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            player = other.GetComponent<Player>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            player = null;
        }
    }

    private void Update()
    {
        // sebrání itemu klávesou E při kontaktu s hráčem
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            EatItem();
        }
    }

    void EatItem()
    {
        if (player == null) return;

        // doplnění zdraví
        if (type == PickupType.Health || type == PickupType.Both)
        {
            player.healthController.Heal(healthAmount);
        }

        // doplnění staminy
        if (type == PickupType.Energy || type == PickupType.Both)
        {
            player.staminaController.stamina += energyAmount;
            player.staminaController.stamina =
                Mathf.Clamp(player.staminaController.stamina, 0, player.staminaController.maxStamina);

            player.staminaController.UpdateStamina();
        }

        Destroy(gameObject);
    }
}
