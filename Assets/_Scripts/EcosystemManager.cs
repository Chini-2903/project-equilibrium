using UnityEngine;
using TMPro;

public class EcosystemManager : MonoBehaviour
{
    [Header("Ecosystem Percentages (Must Total 100)")]
    public float floraPercent = 40f;   // Max 40
    public float preyPercent = 35f;
    public float predatorPercent = 15f;
    public float waterPercent = 10f;   // Max 10

    [Header("Target Tolerances")]
    public float floraTarget = 40f;
    public float preyTarget = 35f;
    public float predatorTarget = 15f;
    public float floraPreyTolerance = 5f;
    public float predatorTolerance = 3f;

    [Header("Time System")]
    public int currentDay = 1;
    public int currentHour = 6;
    public int currentMinute = 0;
    private float timer = 0f;

    [Header("Win Condition")]
    public int daysStabilized = 0;
    public bool isBalanced = false;

    [Header("UI References")]
    public TextMeshProUGUI statsText;

    void Update()
    {
        HandleTime();
        CheckEquilibrium();
        UpdateHUD();
        TestMathWithKeyboard();
    }

    // --- THE FIXED MATH (Conservation of Mass) --- //

    public void AddPredator(float amount)
    {
        // Only grow if there is enough prey to eat
        if (preyPercent >= amount)
        {
            predatorPercent += amount;
            preyPercent -= amount;
        }
    }

    public void AddPrey(float amount)
    {
        float floraCost = amount * 0.8f;
        float waterCost = amount * 0.2f;

        // Only grow if there is enough food and water
        if (floraPercent >= floraCost && waterPercent >= waterCost)
        {
            preyPercent += amount;
            floraPercent -= floraCost;
            waterPercent -= waterCost;
        }
    }

    public void AddFlora(float amount)
    {
        floraPercent += amount;
        if (floraPercent > 40f) floraPercent = 40f; // Enforce the hard cap
    }

    // Simulates the slow, natural shift of the world over time
    void NaturalEcosystemDrain()
    {
        // Every in-game hour, Prey naturally eats a tiny bit of Flora
        float naturalGrazing = 0.1f;

        if (floraPercent >= naturalGrazing && waterPercent >= (naturalGrazing * 0.2f))
        {
            preyPercent += naturalGrazing;
            floraPercent -= naturalGrazing;
            waterPercent -= (naturalGrazing * 0.2f);
        }
    }

    // --- SYSTEMS --- //

    void HandleTime()
    {
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            currentMinute++;
            timer = 0f;

            if (currentMinute >= 60)
            {
                currentMinute = 0;
                currentHour++;

                NaturalEcosystemDrain(); // The world shifts slightly every hour

                if (currentHour >= 24)
                {
                    currentHour = 0;
                    currentDay++;

                    if (isBalanced)
                    {
                        daysStabilized++;
                        if (daysStabilized >= 3) Debug.Log("YOU WIN! THE ECOSYSTEM IS STABLE!");
                    }
                    else
                    {
                        daysStabilized = 0;
                    }
                }
            }
        }
    }

    void CheckEquilibrium()
    {
        bool floraOk = Mathf.Abs(floraPercent - floraTarget) <= floraPreyTolerance;
        bool preyOk = Mathf.Abs(preyPercent - preyTarget) <= floraPreyTolerance;
        bool predOk = Mathf.Abs(predatorPercent - predatorTarget) <= predatorTolerance;
        bool waterOk = waterPercent >= 10f;

        isBalanced = floraOk && preyOk && predOk && waterOk;
    }

    public void UpdateHUD()
    {
        if (statsText != null)
        {
            float total = floraPercent + preyPercent + predatorPercent + waterPercent;
            string timeString = string.Format("Day {0} - {1:00}:{2:00}", currentDay, currentHour, currentMinute);
            string balanceStatus = isBalanced ? "<color=green>STABLE</color>" : "<color=red>UNSTABLE</color>";

            statsText.text = "ECOSYSTEM STATUS\n" +
                             "----------------\n" +
                             "Flora: " + floraPercent.ToString("F1") + "%\n" +
                             "Prey: " + preyPercent.ToString("F1") + "%\n" +
                             "Predators: " + predatorPercent.ToString("F1") + "%\n" +
                             "Water: " + waterPercent.ToString("F1") + "%\n" +
                             "<i>Total Mass: " + total.ToString("F1") + "%</i>\n\n" +
                             timeString + "\n" +
                             "Status: " + balanceStatus + "\n" +
                             "Stable Days: " + daysStabilized + "/3";
        }
    }

    void TestMathWithKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.P)) AddPredator(2f);
        if (Input.GetKeyDown(KeyCode.O)) AddPrey(2f);
    }

    // Called when a physical plant is destroyed by a hungry Prey
    public void ConsumeFlora(float amount)
    {
        floraPercent -= amount;
        if (floraPercent < 0f) floraPercent = 0f;
    }
}