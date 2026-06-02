using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class PreyAI : MonoBehaviour
{
    [Header("Survival Stats")]
    public int health = 3;
    public float baseSpeed = 2.5f;
    public float fearRadius = 15f;

    [Header("Infection System")]
    public bool isInfected = false;
    public Material infectedMaterial;
    private Renderer myRenderer;
    private float sicknessTimer = 0f;
    private float spreadTimer = 2f;

    [Header("Wander Settings")]
    public float wanderRadius = 15f;
    public float minGrazeTime = 2f;
    public float maxGrazeTime = 6f;

    private NavMeshAgent agent;
    private float waitTimer;
    private bool isWandering;
    private float myRunSpeed;
    private float myWalkSpeed;

    private bool isKnockedBack = false;
    private float panicTimer = 0f;

    private Transform targetFlora = null;
    private bool isEating = false;
    private EcosystemManager ecoManager;

    private int nutritionPoints = 0;
    private List<PredatorAI> attackers = new List<PredatorAI>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ecoManager = FindFirstObjectByType<EcosystemManager>();
        myRenderer = GetComponent<Renderer>();

        float geneticMultiplier = Random.Range(0.8f, 1.4f);
        myWalkSpeed = baseSpeed * geneticMultiplier;
        myRunSpeed = (baseSpeed * 3.5f) * geneticMultiplier;

        agent.speed = myWalkSpeed;
        SetNewGrazeTime();

        // If spawned as Patient Zero via the Inspector
        if (isInfected) BecomeInfected();
    }

    void Update()
    {
        HandleInfection(); // <--- NEW: Process sickness every frame

        if (isKnockedBack || isEating) return;
        if (LookForDanger()) return;

        agent.speed = myWalkSpeed;

        if (isWandering)
        {
            if (targetFlora == null && agent.hasPath)
            {
                agent.ResetPath();
                isWandering = false;
                SetNewGrazeTime();
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                isWandering = false;
                if (targetFlora != null) StartCoroutine(GrazeOnPhysicalFlora(targetFlora));
                else SetNewGrazeTime();
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) PickNewWanderTarget();
        }
    }

    // --- THE BLIGHT LOGIC ---
    public void BecomeInfected()
    {
        if (isInfected) return;
        isInfected = true;
        if (myRenderer != null && infectedMaterial != null) myRenderer.material = infectedMaterial;
    }

    void HandleInfection()
    {
        if (!isInfected) return;

        sicknessTimer += Time.deltaTime;

        // Die naturally after 1 in-game day (1440 seconds)
        if (sicknessTimer >= 1440f)
        {
            SpawnMeatAndDie();
        }

        // Proximity Spread Check (Every 60 seconds)
        spreadTimer += Time.deltaTime;
        if (spreadTimer >= 60f)
        {
            spreadTimer = 0f;
            Collider[] hits = Physics.OverlapSphere(transform.position, fearRadius);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Prey"))
                {
                    PreyAI other = hit.GetComponent<PreyAI>();
                    // 20% chance to infect nearby healthy herd members
                    if (other != null && !other.isInfected && Random.value < 0.20f)
                    {
                        other.BecomeInfected();
                        Debug.Log("The Blight has spread to a new Prey!");
                    }
                }
            }
        }
    }

    void SpawnMeatAndDie()
    {
        GameObject meat = Resources.Load<GameObject>("Meat_Prefab");
        if (meat != null)
        {
            GameObject spawnedMeat = Instantiate(meat, transform.position, Quaternion.identity);

            // Pass the sickness into the meat!
            if (isInfected) spawnedMeat.GetComponent<FoodSource>().isInfected = true;
        }
        Destroy(gameObject);
    }

    // --- EXISTING LOGIC BELOW ---
    bool LookForDanger()
    {
        if (panicTimer > 0f) { panicTimer -= Time.deltaTime; return true; }
        Collider[] hits = Physics.OverlapSphere(transform.position, fearRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Predator"))
            {
                if (isEating && targetFlora != null)
                {
                    FoodSource fs = targetFlora.GetComponent<FoodSource>();
                    if (fs != null) fs.StopEating();
                }
                isEating = false; targetFlora = null; agent.speed = myRunSpeed;
                Vector3 bestEscapePoint = FindBestEscapeRoute(hit.transform.position);
                agent.SetDestination(bestEscapePoint);
                panicTimer = 0.5f; isWandering = false;
                return true;
            }
        }
        return false;
    }

    Vector3 FindBestEscapeRoute(Vector3 predatorPos)
    {
        Vector3 baseFleeDir = (transform.position - predatorPos).normalized;
        Vector3 bestPoint = transform.position;
        float bestScore = -Mathf.Infinity;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 testDir = Quaternion.Euler(0, angle, 0) * baseFleeDir;
            Vector3 testPoint = transform.position + (testDir * 15f);
            NavMeshHit hit;
            bool hitWall = NavMesh.Raycast(transform.position, testPoint, out hit, NavMesh.AllAreas);
            float distanceToWall = Vector3.Distance(transform.position, hit.position);
            float distanceFromPredator = Vector3.Distance(hit.position, predatorPos);
            float score = distanceToWall + (distanceFromPredator * 0.5f);
            if (score > bestScore) { bestScore = score; bestPoint = hit.position; }
        }
        return bestPoint;
    }

    IEnumerator GrazeOnPhysicalFlora(Transform floraToEat)
    {
        isEating = true; agent.ResetPath();
        FoodSource foodScript = floraToEat.GetComponent<FoodSource>();
        if (foodScript != null) foodScript.StartEating();
        float mySize = transform.localScale.magnitude;
        float floraSize = floraToEat.localScale.magnitude;
        float timeToEat = 3f * (floraSize / mySize);
        float timer = 0f;
        while (timer < timeToEat)
        {
            if (floraToEat == null || isKnockedBack)
            {
                if (foodScript != null && floraToEat != null) foodScript.StopEating();
                isEating = false; targetFlora = null;
                if (!isKnockedBack) SetNewGrazeTime();
                yield break;
            }
            timer += Time.deltaTime; yield return null;
        }
        if (floraToEat != null)
        {
            bool wasShared = foodScript != null && foodScript.WasShared();
            nutritionPoints += wasShared ? 2 : 5;
            Destroy(floraToEat.gameObject);
            if (ecoManager != null) ecoManager.ConsumeFlora(2f);
            if (nutritionPoints >= 10)
            {
                nutritionPoints = 0;
                GameObject baby = Resources.Load<GameObject>("Prey_Animal");
                if (baby != null) Instantiate(baby, transform.position + new Vector3(1f, 0, 1f), Quaternion.identity);
            }
        }
        isEating = false; targetFlora = null; SetNewGrazeTime();
    }

    public void TakeDamage(PredatorAI attacker)
    {
        health--;
        if (!attackers.Contains(attacker)) attackers.Add(attacker);
        if (health <= 0)
        {
            bool wasSharedKill = attackers.Count > 1;
            foreach (PredatorAI pred in attackers) { if (pred != null) pred.AwardHuntingPoints(wasSharedKill); }
            SpawnMeatAndDie(); // Call our new method
            return;
        }
        Vector3 pushDirection = (transform.position - attacker.transform.position).normalized;
        pushDirection.y = 0; StartCoroutine(SmoothKnockback(pushDirection));
    }

    IEnumerator SmoothKnockback(Vector3 direction)
    {
        isKnockedBack = true; agent.ResetPath();
        float duration = 0.2f; float knockbackSpeed = 15f; float timer = 0f;
        while (timer < duration) { timer += Time.deltaTime; agent.Move(direction * knockbackSpeed * Time.deltaTime); yield return null; }
        isKnockedBack = false;
    }

    void SetNewGrazeTime() { waitTimer = Random.Range(minGrazeTime, maxGrazeTime); }

    void PickNewWanderTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, fearRadius);
        Transform closestFlora = null; float closestDist = Mathf.Infinity;
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Flora"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist) { closestDist = dist; closestFlora = hit.transform; }
            }
        }
        if (closestFlora != null) { targetFlora = closestFlora; agent.SetDestination(targetFlora.position); isWandering = true; return; }
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position; NavMeshHit navHit;
        if (NavMesh.SamplePosition(randomDirection, out navHit, wanderRadius, 1)) { agent.SetDestination(navHit.position); isWandering = true; }
    }

    public void ReactToPulse(Vector3 pulseSource, string pulseType)
    {
        if (pulseType == "Panic")
        {
            if (isEating && targetFlora != null) { FoodSource fs = targetFlora.GetComponent<FoodSource>(); if (fs != null) fs.StopEating(); }
            isEating = false; agent.speed = myRunSpeed;
            Vector3 bestEscapePoint = FindBestEscapeRoute(pulseSource); agent.SetDestination(bestEscapePoint);
            panicTimer = 2.0f; isWandering = false;
        }
        else if (pulseType == "AttractPrey")
        {
            if (isEating && targetFlora != null) { FoodSource fs = targetFlora.GetComponent<FoodSource>(); if (fs != null) fs.StopEating(); }
            isEating = false; agent.speed = myWalkSpeed; agent.SetDestination(pulseSource); panicTimer = 5.0f; isWandering = false;
        }
    }
}