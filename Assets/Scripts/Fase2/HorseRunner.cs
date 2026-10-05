using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HorseRunner : MonoBehaviour
{
    [Header("Datos del Caballo")]
    public string horseName = "Caballo";
    public Sprite horsePortrait; // 📷 Imagen única de este caballo para la UI
    public float baseWinProbability = 0.125f;
    public float winProbabilityModifier = 0f;
    public float fallProbabilityModifier = 0f;

    [Header("Parámetros de Movimiento Base")]
    public float baseSpeed = 12f;
    public float speedVariationRange = 2.5f;
    public float currentSpeed;

    [Header("Configuración NavMesh y Ruta")]
    public List<Transform> waypoints = new List<Transform>();
    public int currentWaypointIndex = 0;

    [Tooltip("Número mínimo de Waypoint que el caballo debe alcanzar antes de poder cruzar la meta.")]
    public int minWaypointToFinish = 2;

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;

    [Header("Mecánica de Caída (Precalculada)")]
    [Range(0f, 100f)] public float baseFallChancePercent = 5f;
    public bool willFallThisRace = false;
    public int fallAtWaypointIndex = -1;

    [Header("Estado de la Carrera")]
    public bool isRunning = false;
    public bool hasFinished = false;
    public bool hasFallen = false;
    public bool hasBeenShot = false; // 🎯 Nuevo estado: Disparado

    [Header("Simulación Predeterminada (Scripted)")]
    [SerializeField] private int assignedFinalRank = -1;
    [SerializeField] private int totalCompetitors = 8;
    [SerializeField] private int currentSectorIndex = 0;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }
        obstacle.enabled = false;
        obstacle.carving = true;
    }

    public void SetupHorseData(HorseUIElement horseData)
    {
        if (horseData == null) return;
        horseName = horseData.horseName;
        baseWinProbability = horseData.winProbability;
        winProbabilityModifier = horseData.winProbabilityModifier;
        fallProbabilityModifier = horseData.fallProbabilityModifier;
    }

    public void ConfigureScriptedBehavior(int rank, int total)
    {
        assignedFinalRank = rank;
        totalCompetitors = total;
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
        hasBeenShot = false;
        currentWaypointIndex = 0;
        currentSectorIndex = 0;
        currentSpeed = 0f;

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

        CalculateSingleFallChance();
    }

    private void CalculateSingleFallChance()
    {
        float effectiveFallChance = Mathf.Clamp(baseFallChancePercent + fallProbabilityModifier, 0f, 100f);
        willFallThisRace = (Random.Range(0f, 100f) < effectiveFallChance);

        if (willFallThisRace && waypoints.Count > 1)
        {
            fallAtWaypointIndex = Random.Range(1, waypoints.Count);
        }
        else
        {
            willFallThisRace = false;
            fallAtWaypointIndex = -1;
        }
    }

    public void StartRunning()
    {
        if (hasFallen || hasBeenShot || waypoints.Count == 0) return;

        isRunning = true;
        hasFinished = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            SetNextDestination();
        }

        if (animator != null) animator.SetBool("IsRunning", true);
        OnEnterTrackSector(0, 4);
    }

    private void Update()
    {
        if (!isRunning || hasFinished || hasFallen || hasBeenShot) return;
        CheckWaypointProgress();
    }

    public void OnEnterTrackSector(int sectorIndex, int totalSectors)
    {
        if (hasFallen || hasBeenShot || hasFinished) return;

        currentSectorIndex = sectorIndex;

        if (sectorIndex < totalSectors - 1)
        {
            float noise = Random.Range(-speedVariationRange, speedVariationRange);
            currentSpeed = baseSpeed + noise;
            SetAgentPhysics(currentSpeed, acceleration: 12f);
        }
        else
        {
            if (assignedFinalRank > 0)
            {
                float rankBonus = (totalCompetitors - assignedFinalRank) * 0.9f;
                currentSpeed = baseSpeed + rankBonus + 3f;
                SetAgentPhysics(currentSpeed, acceleration: 28f);
            }
            else
            {
                currentSpeed = baseSpeed + Random.Range(0f, speedVariationRange);
                SetAgentPhysics(currentSpeed, acceleration: 15f);
            }
        }
    }

    private void SetAgentPhysics(float targetSpeed, float acceleration)
    {
        if (agent == null || !agent.enabled) return;

        currentSpeed = Mathf.Max(4f, targetSpeed);
        agent.speed = currentSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = 120f + (currentSpeed * 4f);
    }

    private void CheckWaypointProgress()
    {
        if (agent == null || !agent.enabled || waypoints.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.5f)
        {
            if (willFallThisRace && currentWaypointIndex == fallAtWaypointIndex)
            {
                TriggerFall();
                return;
            }

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

    public void TriggerFall()
    {
        if (hasFallen || hasBeenShot) return;

        hasFallen = true;
        isRunning = false;
        currentSpeed = 0f;

        StopAndBlockNavMesh();

        if (animator != null) animator.SetTrigger("Fall");

        Debug.Log($"💥 ¡{horseName} ha sufrido una caída en el Waypoint #{currentWaypointIndex}!");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckRaceCompletion();
        }
    }

    /// <summary>
    /// 🎯 Reacción del caballo al ser impactado por un disparo del francotirador.
    /// Queda fuera de la carrera y se detiene en la posición actual.
    /// </summary>
    public void TriggerShot()
    {
        if (hasBeenShot || hasFallen || hasFinished) return;

        hasBeenShot = true;
        isRunning = false;
        currentSpeed = 0f;

        StopAndBlockNavMesh();

        if (animator != null)
        {
            // Puedes usar un Trigger de animación 'Shot' o reutilizar 'Fall'
            animator.SetTrigger("Shot");
        }

        Debug.Log($"🎯 ¡{horseName} fue alcanzado por un disparo!");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckRaceCompletion();
        }
    }

    private void StopAndBlockNavMesh()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (obstacle != null)
        {
            obstacle.enabled = true; // Activa obstáculo para que otros caballos lo esquiven
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FinishLine") && isRunning && !hasFinished && !hasFallen && !hasBeenShot)
        {
            if (currentWaypointIndex >= minWaypointToFinish)
            {
                CrossFinishLine();
            }
        }
    }

    private void CrossFinishLine()
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
