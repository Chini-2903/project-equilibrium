using UnityEngine;
using TMPro;

public class PlayerSurvival : MonoBehaviour
{
    [Header("Survival Stats")]
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;

    private float currentHealth;
    private float currentHunger;
    private float currentThirst;

    [Header("Inventory")]
    public int biomassSeeds = 0; // <--- NEW: Tracks your seeds!

    [Header("Drain Rates")]
    public float hungerDrainRate = 0.2f;
    public float thirstDrainRate = 0.3f;
    public float healthDrainRate = 10f;

    [Header("UI")]
    public TextMeshProUGUI playerStatsText;
    private WardenController wardenMovement;

    void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        wardenMovement = GetComponent<WardenController>();
    }

    void Update()
    {
        HandleSurvivalDrain();
        ApplySpeedPenalties();
        UpdateHUD();
    }

    void HandleSurvivalDrain()
    {
        currentHunger -= hungerDrainRate * Time.deltaTime;
        currentThirst -= thirstDrainRate * Time.deltaTime;

        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);

        if (currentHunger <= 0 || currentThirst <= 0)
        {
            currentHealth -= healthDrainRate * Time.deltaTime;
            if (currentHealth <= 0) Debug.Log("WARDEN DEAD");
        }
        else if (currentHealth < maxHealth)
        {
            currentHealth += 0.5f * Time.deltaTime;
        }
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    void ApplySpeedPenalties()
    {
        if (wardenMovement == null) return;
        float targetSpeed = wardenMovement.baseWalkSpeed;

        if (currentHunger < 30f) targetSpeed *= 0.7f;
        else if (currentHunger < 50f) targetSpeed *= 0.9f;

        wardenMovement.currentWalkSpeed = targetSpeed;
    }

    void UpdateHUD()
    {
        if (playerStatsText != null)
        {
            string hColor = currentHunger < 30 ? "<color=red>" : "<color=white>";
            string tColor = currentThirst < 30 ? "<color=red>" : "<color=white>";
            string hpColor = currentHealth < 30 ? "<color=red>" : "<color=green>";

            playerStatsText.text = "WARDEN VITALS\n" +
                                   hpColor + "Health: " + Mathf.RoundToInt(currentHealth) + "</color>\n" +
                                   hColor + "Hunger: " + Mathf.RoundToInt(currentHunger) + "</color>\n" +
                                   tColor + "Thirst: " + Mathf.RoundToInt(currentThirst) + "</color>\n" +
                                   "<color=yellow>Biomass Seeds: " + biomassSeeds + "</color>";
        }
    }

    public void EatFood() { currentHunger = maxHunger; }
    public void DrinkWater() { currentThirst = maxThirst; }
}