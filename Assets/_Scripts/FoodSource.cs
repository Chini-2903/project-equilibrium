using UnityEngine;

public class FoodSource : MonoBehaviour
{
    [Header("Tracking Data")]
    public float ageInSeconds = 0f;
    public bool isInfected = false;

    private int currentEaters = 0;
    private int maxEaters = 0; // Tracks the highest number of simultaneous eaters

    void Update()
    {
        // 1 real second = 1 game minute. 1440 seconds = 1 game day.
        ageInSeconds += Time.deltaTime;
    }

    // Called by an animal when it starts chewing
    public void StartEating()
    {
        currentEaters++;
        if (currentEaters > maxEaters) maxEaters = currentEaters;
    }

    // Called if an animal gets scared and runs away mid-meal
    public void StopEating()
    {
        currentEaters--;
    }

    // Called when the meal is finished to check the math
    public bool WasShared()
    {
        return maxEaters > 1; // If more than 1 animal took a bite, it's shared!
    }
}