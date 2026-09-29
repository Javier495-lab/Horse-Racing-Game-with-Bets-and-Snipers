using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HorseRunner : MonoBehaviour
{
    [Header("Datos del Caballo")]
    public string horseName = "Caballo";
    public float baseWinProbability = 0.125f;
    public float winProbabilityModifier = 0f;
    public float fallProbabilityModifier = 0f;

    [Header("Parámetros de Movimiento")]
    public float baseSpeed = 10f;
    public float speedVariationRange = 2f;
    public float currentSpeed;

    [Header("Configuración NavMesh y Ruta")]
    public List<Transform> waypoints = new List<Transform>();
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle; // Para cuando se caiga o le disparen

    [Header("Mecánica de Caída")]
    [Range(0f, 100f)] public float baseFallChancePercent = 5f;
    public float fallCheckInterval = 2.0f;
    private float fallTimer = 0f;

    [Header("Estado")]
    public bool isRunning = false;
    public bool hasFinished = false;
    public bool hasFallen = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        // Añadir o recuperar el NavMeshObstacle
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.enabled = false; // Desactivado al inicio
        obstacle.carving = true;  // Hace que los demás agentes recalculen su ruta a su alrededor
    }

    public void SetupHorseData(HorseUIElement horseData)
    {
        if (horseData == null) return;
        horseName = horseData.horseName;
        baseWinProbability = horseData.winProbability;
        winProbabilityModifier = horseData.winProbabilityModifier;
        fallProbabilityModifier = horseData.fallProbabilityModifier;
    }

    public void SetWaypoints(List<Transform> circuitWaypoints)
    {
        waypoints = circuitWaypoints;
    }

    public void ResetRunner()
    {
        isRunning = false;
        hasFinished = false;
        hasFallen = false;
        currentWaypointIndex = 0;
        currentSpeed = 0f;
        fallTimer = 0f;

        if (obstacle != null) obstacle.enabled = false;
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.Rebind();
        }
    }

    public void StartRunning()
    {
        if (hasFallen || waypoints.Count == 0) return;

        isRunning = true;
        hasFinished = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            SetNextDestination();
        }

        if (animator != null) animator.SetBool("IsRunning", true);
    }

    private void Update()
    {
        if (!isRunning || hasFinished || hasFallen) return;

        // 1. Calcular y aplicar velocidad al Agent
        CalculateSpeed();
        if (agent.enabled)
        {
            agent.speed = currentSpeed;
        }

        // 2. Comprobar si ha llegado al Waypoint actual
        CheckWaypointProgress();

        // 3. Comprobación periódica de caídas
        HandleFallCheck();
    }

    private void CalculateSpeed()
    {
        float totalProb = Mathf.Max(0.01f, baseWinProbability + winProbabilityModifier);
        float probBonus = totalProb * 8f;
        float noise = Random.Range(-speedVariationRange, speedVariationRange);

        currentSpeed = baseSpeed + probBonus + noise;
    }

    private void CheckWaypointProgress()
    {
        if (!agent.enabled || waypoints.Count == 0) return;

        // Si está cerca del destino actual, pasa al siguiente waypoint
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.5f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex < waypoints.Count)
            {
                SetNextDestination();
            }
        }
    }

    private void SetNextDestination()
    {
        if (currentWaypointIndex < waypoints.Count && waypoints[currentWaypointIndex] != null)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void HandleFallCheck()
    {
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallCheckInterval)
        {
            fallTimer = 0f;
            float effectiveFallChance = Mathf.Clamp(baseFallChancePercent + fallProbabilityModifier, 0f, 100f);

            if (Random.Range(0f, 100f) < effectiveFallChance)
            {
                TriggerFall();
            }
        }
    }

    /// <summary>
    /// Llamado cuando el caballo cae por probabilidad o por un disparo.
    /// </summary>
    public void TriggerFall()
    {
        if (hasFallen) return;

        hasFallen = true;
        isRunning = false;
        currentSpeed = 0f;

        // Desactivar el agente e intercambiarlo por el obstáculo dinámico
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (obstacle != null)
        {
            obstacle.enabled = true; // El cuerpo tirado se convierte en obstáculo para los demás
        }

        if (animator != null) animator.SetTrigger("Fall");

        Debug.Log($"¡{horseName} se ha caído/sido disparado!");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckRaceCompletion();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FinishLine") && isRunning && !hasFinished)
        {
            hasFinished = true;
            isRunning = false;

            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
            }

            if (animator != null) animator.SetBool("IsRunning", false);

            if (RaceManager.Instance != null)
            {
                RaceManager.Instance.OnHorseFinished(this);
            }
        }
    }
}
