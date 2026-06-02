using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PredatorAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speedWander = 3.0f;
    public float speedChase = 5.5f;
    public float wanderRadius = 20f;

    [Header("Hunting Settings")]
    public float detectionRadius = 15f;
    public float attackRadius = 2.5f;
    public float attackCooldown = 2.0f;

    [Header("Scavenger Settings")]
    // 1 in-game day = 1440 real seconds. (Lower this number to 10f temporarily if you want to test it quickly!)
    public float minCarcassAgeToEat = 1440f;

    private NavMeshAgent agent;
    private Transform currentPrey;
    private float cooldownTimer = 0f;
    private float panicTimer = 0f;

    private bool isEating = false;
    private int nutritionPoints = 0; // Target is 20 to breed!

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 1.5f;
        PickNewWanderTarget();
    }

    void Update()
    {
        HandleInfection(); // <--- WE CALL IT HERE NOW!

        if (panicTimer > 0f)
        {
            panicTimer -= Time.deltaTime;
            return;
        }

        if (isEating) return;
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        LookForTargets();

        if (currentPrey != null)
        {
            agent.speed = speedChase;
            agent.SetDestination(currentPrey.position);

            if (Vector3.Distance(transform.position, currentPrey.position) <= attackRadius)
            {
                if (currentPrey.CompareTag("Carcass"))
                {
                    StartCoroutine(GrazeOnCarcass(currentPrey));
                }
                else if (cooldownTimer <= 0f)
                {
                    PreyAI preyScript = currentPrey.GetComponent<PreyAI>();
                    if (preyScript != null)
                    {
                        preyScript.TakeDamage(this);
                        cooldownTimer = attackCooldown;
                    }
                }
            }
        }
        else
        {
            agent.speed = speedWander;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                PickNewWanderTarget();
            }
        }
    }

    void LookForTargets()
    {
        if (currentPrey != null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Prey"))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = hit.transform;
                }
            }
            else if (hit.CompareTag("Carcass"))
            {
                FoodSource food = hit.GetComponent<FoodSource>();

                // AGE CHECK: Ignore the carcass entirely if it is less than 1 day old!
                if (food != null && food.ageInSeconds >= minCarcassAgeToEat)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position) * 0.5f; // Favor old carcasses highly
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestTarget = hit.transform;
                    }
                }
            }
        }

        if (bestTarget != null) currentPrey = bestTarget;
    }

    // Called automatically by the PreyAI script when the prey dies
    public void AwardHuntingPoints(bool wasSharedKill)
    {
        // Solo Kill = 2 pts. Shared Kill = 1 pt.
        nutritionPoints += wasSharedKill ? 1 : 2;
        CheckBreeding();
    }

    IEnumerator GrazeOnCarcass(Transform carcassToEat)
    {
        isEating = true;
        agent.ResetPath();

        FoodSource foodScript = carcassToEat.GetComponent<FoodSource>();
        if (foodScript != null) foodScript.StartEating();

        float timer = 0f;
        while (timer < 4f)
        {
            if (carcassToEat == null)
            {
                if (foodScript != null) foodScript.StopEating();
                isEating = false;
                currentPrey = null;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (carcassToEat != null)
        {
            bool wasShared = foodScript != null && foodScript.WasShared();
            if (!wasShared) nutritionPoints += 10;

            // --- NEW: INFECTION CHECK! ---
            if (foodScript != null && foodScript.isInfected)
            {
                BecomeInfected();
                Debug.Log("A Predator ate tainted meat and caught The Blight!");
            }

            Destroy(carcassToEat.gameObject);
            CheckBreeding();
        }

        isEating = false;
        currentPrey = null;
        PickNewWanderTarget();
    }

    void CheckBreeding()
    {
        if (nutritionPoints >= 20)
        {
            nutritionPoints = 0;
            GameObject baby = Resources.Load<GameObject>("Predator_Animal");
            if (baby != null) Instantiate(baby, transform.position + new Vector3(1.5f, 0, 1.5f), Quaternion.identity);
        }
    }

    void PickNewWanderTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, wanderRadius, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void ReactToPulse(Vector3 pulseSource, string pulseType)
    {
        if (pulseType == "Panic")
        {
            if (isEating && currentPrey != null)
            {
                FoodSource fs = currentPrey.GetComponent<FoodSource>();
                if (fs != null) fs.StopEating();
            }
            isEating = false;
            currentPrey = null;
            agent.speed = speedChase;
            Vector3 fleeDir = (transform.position - pulseSource).normalized;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position + (fleeDir * 15f), out hit, 15f, 1))
            {
                agent.SetDestination(hit.position);
            }
            panicTimer = 2.5f;
        }
        else if (pulseType == "AttractPredator")
        {
            if (isEating && currentPrey != null)
            {
                FoodSource fs = currentPrey.GetComponent<FoodSource>();
                if (fs != null) fs.StopEating();
            }
            isEating = false;
            currentPrey = null;
            agent.speed = speedWander;
            agent.SetDestination(pulseSource);
            panicTimer = 4.0f;
        }
    }

    // --- PREDATOR INFECTION LOGIC ---
    [Header("Infection System")]
    public bool isInfected = false;
    public Material infectedMaterial;
    private float sicknessTimer = 0f;

    public void BecomeInfected()
    {
        if (isInfected) return;
        isInfected = true;

        Renderer myRenderer = GetComponent<Renderer>();
        if (myRenderer != null && infectedMaterial != null) myRenderer.material = infectedMaterial;

        // 50% Speed Penalty!
        speedWander *= 0.5f;
        speedChase *= 0.5f;
    }

    void HandleInfection()
    {
        if (!isInfected) return;

        sicknessTimer += Time.deltaTime;

        // Predators die faster from it (12 hours / 720 seconds)
        if (sicknessTimer >= 720f) Destroy(gameObject);
    }
}