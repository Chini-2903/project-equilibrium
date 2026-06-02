using UnityEngine;

public class PulseTool : MonoBehaviour
{
    [Header("Tool Settings")]
    public float pulseRadius = 20f;
    public float cooldownTime = 3f;

    private float currentCooldown = 0f;
    private int currentLevel = 1; // 1 = Panic, 2 = Attract Prey, 3 = Attract Predator

    void Update()
    {
        if (currentCooldown > 0) currentCooldown -= Time.deltaTime;

        // --- FREQUENCY SWITCHING ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentLevel = 1;
            Debug.Log("Pulse Tool: PANIC FREQUENCY (All)");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentLevel = 2;
            Debug.Log("Pulse Tool: SHEPHERD FREQUENCY (Attract Prey)");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentLevel = 3;
            Debug.Log("Pulse Tool: ALPHA FREQUENCY (Attract Predator)");
        }

        // --- FIRING THE TOOL ---
        if (Input.GetMouseButtonDown(0) && currentCooldown <= 0f)
        {
            FirePulse();
            currentCooldown = cooldownTime;
        }
    }

    void FirePulse()
    {
        string pulseType = "Panic";
        if (currentLevel == 2) pulseType = "AttractPrey";
        if (currentLevel == 3) pulseType = "AttractPredator";

        Debug.Log("Fired Pulse: " + pulseType);

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Prey"))
            {
                PreyAI prey = hit.GetComponent<PreyAI>();
                if (prey != null) prey.ReactToPulse(transform.position, pulseType);
            }
            else if (hit.CompareTag("Predator"))
            {
                PredatorAI predator = hit.GetComponent<PredatorAI>();
                if (predator != null) predator.ReactToPulse(transform.position, pulseType);
            }
        }
    }
}