using UnityEngine;
using TMPro; // Required for the UI

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 5f;
    public TextMeshProUGUI promptText; // Connect this in Inspector!

    private PlayerSurvival survivalStats;
    private EcosystemManager ecoManager;
    private bool isCarryingCarcass = false;

    void Start()
    {
        survivalStats = GetComponentInParent<PlayerSurvival>();
        // Automatically finds the EcosystemManager in the scene
        ecoManager = FindFirstObjectByType<EcosystemManager>();
    }

    void Update()
    {
        HandleRaycastAndInput();
    }

    void HandleRaycastAndInput()
    {
        // 1. Default to empty text every frame so it disappears when you look away
        if (promptText != null) promptText.text = "";

        RaycastHit hit;
        // 2. Shoot the invisible laser
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactRange))
        {
            // CARCASS
            if (hit.collider.CompareTag("Carcass"))
            {
                if (!isCarryingCarcass)
                {
                    SetPrompt("[E] Pick Up Carcass");
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        isCarryingCarcass = true;
                        Destroy(hit.collider.gameObject);
                    }
                }
                else SetPrompt("Hands Full! Take Carcass to Table.");
            }
            // PROCESSING TABLE (THE MORAL CHOICE)
            else if (hit.collider.CompareTag("Table"))
            {
                if (isCarryingCarcass)
                {
                    SetPrompt("[E] Eat Meat  |  [B] Extract Biomass");

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        isCarryingCarcass = false;
                        survivalStats.EatFood(); // Feed yourself
                    }
                    else if (Input.GetKeyDown(KeyCode.B))
                    {
                        isCarryingCarcass = false;
                        survivalStats.biomassSeeds++; // Save the planet
                    }
                }
                else SetPrompt("Processing Table (Requires Carcass)");
            }
            // WATER TANK
            else if (hit.collider.CompareTag("WaterTank"))
            {
                SetPrompt("[E] Drink Water");
                if (Input.GetKeyDown(KeyCode.E)) survivalStats.DrinkWater();
            }
            // PLANTING ON THE GROUND
            // If you have seeds, and you are looking at the floor (Untagged)...
            else if (survivalStats.biomassSeeds > 0 && hit.collider.CompareTag("Untagged"))
            {
                SetPrompt("[F] Plant Flora");
                if (Input.GetKeyDown(KeyCode.F))
                {
                    survivalStats.biomassSeeds--;
                    PlantFlora(hit.point);
                }
            }
        }
    }

    void SetPrompt(string message)
    {
        if (promptText != null) promptText.text = message;
    }

    void PlantFlora(Vector3 position)
    {
        // 1. Spawn the physical plant exactly where you are looking
        GameObject floraPrefab = Resources.Load<GameObject>("Flora_Prefab");
        if (floraPrefab != null)
        {
            Instantiate(floraPrefab, position, Quaternion.identity);
        }

        // 2. Tell the Ecosystem math that the world just got greener!
        if (ecoManager != null) ecoManager.AddFlora(2f);
    }
}