using UnityEngine;
using UnityEngine.AI;

public class PopulationManager : MonoBehaviour
{
    [Header("Connections")]
    public EcosystemManager ecoManager;

    [Header("Animal Blueprints (Prefabs)")]
    public GameObject preyPrefab;
    public GameObject predatorPrefab;

    [Header("Spawning Settings")]
    public float spawnRadius = 20f;

    void Start()
    {
        // When the game starts, spawn the initial herds
        SpawnHerd(preyPrefab, Mathf.RoundToInt(ecoManager.preyPercent));
        SpawnHerd(predatorPrefab, Mathf.RoundToInt(ecoManager.predatorPercent));
    }

    void SpawnHerd(GameObject animalPrefab, int amountToSpawn)
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            bool successfullySpawned = false;
            int attempts = 0;
            int maxAttempts = 30; // THE FAILSAFE: Never try more than 30 times

            while (!successfullySpawned && attempts < maxAttempts)
            {
                // Pick a random spot inside a giant sphere
                Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
                // Keep them on the same level as the spawner (the floor)
                randomPos.y = transform.position.y;

                // Add the spawner's position so it centers around the GameManager
                randomPos += transform.position;

                NavMeshHit hit;
                // We check if there is a NavMesh within 2 meters of our random point
                if (NavMesh.SamplePosition(randomPos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    Instantiate(animalPrefab, hit.position, Quaternion.identity);
                    successfullySpawned = true; // Success! Exit the while loop.
                }

                attempts++; // Add to our attempt counter
            }

            if (!successfullySpawned)
            {
                Debug.LogWarning("Could not find a valid NavMesh spot to spawn an animal. Is your floor big enough?");
            }
        }
    }
}