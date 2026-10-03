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

    [Header("Parámetros de Movimiento Base")]
    public float baseSpeed = 12f;
    public float speedVariationRange = 2.5f;
    public float currentSpeed;

    [Header("Configuración NavMesh y Ruta")]
    public List<Transform> waypoints = new List<Transform>();
    public int currentWaypointIndex = 0;

    [Tooltip("Número mínimo de Waypoint que el caballo debe alcanzar antes de poder cruzar la meta.")]
    public int minWaypointToFinish = 2; // Evita activar la meta nada más salir

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;

    [Header("Mecánica de Caída (Precalculada)")]
    [Range(0f, 100f)] public float baseFallChancePercent = 5f;
    public bool willFallThisRace = false;  // Determinado 1 sola vez al inicio
    public int fallAtWaypointIndex = -1;  // En qué Waypoint se caerá

    [Header("Estado de la Carrera")]
    public bool isRunning = false;
    public bool hasFinished = false;
    public bool hasFallen = false;

    [Header("Simulación Predeterminada (Scripted)")]
    [SerializeField] private int assignedFinalRank = -1; // 1 = Ganador, 2 = Segundo, etc.
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

    /// <summary>
    /// Asigna los datos desde la UI de la Casa de Apuestas.
    /// </summary>
    public void SetupHorseData(HorseUIElement horseData)
    {
        if (horseData == null) return;
        horseName = horseData.horseName;
        baseWinProbability = horseData.winProbability;
        winProbabilityModifier = horseData.winProbabilityModifier;
        fallProbabilityModifier = horseData.fallProbabilityModifier;
    }

    /// <summary>
    /// Configura la posición final predeterminada en la fase de sorteo previa a la carrera.
    /// </summary>
    public void ConfigureScriptedBehavior(int rank, int total)
    {
        assignedFinalRank = rank;
        totalCompetitors = total;
    }

    public void SetWaypoints(List<Transform> circuitWaypoints)
    {
        waypoints = circuitWaypoints;
    }

    /// <summary>
    /// Reinicia los parámetros para una nueva carrera.
    /// </summary>
    public void ResetRunner()
    {
        isRunning = false;
        hasFinished = false;
        hasFallen = false;
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

        // Calcular la caída una única vez al preparar el caballo
        CalculateSingleFallChance();
    }

    /// <summary>
    /// Calcula UNA SOLA VEZ si el caballo caerá en esta carrera y en qué punto.
    /// Si el caballo tiene asignado quedar entre los primeros o tiene probabilidad de caer, se evalúa.
    /// </summary>
    private void CalculateSingleFallChance()
    {
        float effectiveFallChance = Mathf.Clamp(baseFallChancePercent + fallProbabilityModifier, 0f, 100f);

        // Tirada única de dados (0 a 100)
        willFallThisRace = (Random.Range(0f, 100f) < effectiveFallChance);

        if (willFallThisRace && waypoints.Count > 1)
        {
            // Elige un Waypoint aleatorio entre el 1º después de la salida y el penúltimo antes de meta
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
        if (hasFallen || waypoints.Count == 0) return;

        isRunning = true;
        hasFinished = false;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            SetNextDestination();
        }

        if (animator != null) animator.SetBool("IsRunning", true);

        // Establecer velocidad inicial del Sector 0
        OnEnterTrackSector(0, 4);
    }

    private void Update()
    {
        if (!isRunning || hasFinished || hasFallen) return;

        // Comprobar avance de waypoints y destinos NavMesh
        CheckWaypointProgress();
    }

    /// <summary>
    /// Invocado por los Checkpoints/Triggers de la pista a lo largo del circuito.
    /// Ajusta la velocidad dinámicamente para dar emoción y forzar el resultado predeterminado al final.
    /// </summary>
    public void OnEnterTrackSector(int sectorIndex, int totalSectors)
    {
        if (hasFallen || hasFinished) return;

        currentSectorIndex = sectorIndex;

        // TRAMOS INICIALES E INTERMEDIOS: Adelantamientos dinámicos y variaciones impredecibles
        if (sectorIndex < totalSectors - 1)
        {
            float noise = Random.Range(-speedVariationRange, speedVariationRange);
            currentSpeed = baseSpeed + noise;

            SetAgentPhysics(currentSpeed, acceleration: 12f);
        }
        // TRAMO FINAL / META: SPRINT FINAL QUE GARANTIZA EL PUESTO PREDETERMINADO
        else
        {
            if (assignedFinalRank > 0)
            {
                // El puesto 1 recibe la mayor bonificación de velocidad; los puestos más bajos reciben menos
                float rankBonus = (totalCompetitors - assignedFinalRank) * 0.9f;
                currentSpeed = baseSpeed + rankBonus + 3f;

                SetAgentPhysics(currentSpeed, acceleration: 28f);
            }
            else
            {
                // Respaldo por si no se asignó puesto previo
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

        // Ajuste fluido de giro según velocidad
        agent.angularSpeed = 120f + (currentSpeed * 4f);
    }

    private void CheckWaypointProgress()
    {
        if (agent == null || !agent.enabled || waypoints.Count == 0) return;

        // Comprobar si el NavMeshAgent ha llegado al waypoint actual
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.5f)
        {
            // === ¿ES ESTE EL WAYPOINT DONDE DEBÍA CAER? ===
            if (willFallThisRace && currentWaypointIndex == fallAtWaypointIndex)
            {
                TriggerFall();
                return;
            }

            // Avanzar al siguiente Waypoint
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
        if (hasFallen) return;

        hasFallen = true;
        isRunning = false;
        currentSpeed = 0f;

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (obstacle != null)
        {
            obstacle.enabled = true;
        }

        if (animator != null) animator.SetTrigger("Fall");

        Debug.Log($"💥 ¡{horseName} ha sufrido una caída en el Waypoint #{currentWaypointIndex}!");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckRaceCompletion();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detectar si cruza la meta tras cumplir el mínimo de waypoints
        if (other.CompareTag("FinishLine") && isRunning && !hasFinished)
        {
            if (currentWaypointIndex >= minWaypointToFinish)
            {
                CrossFinishLine();
            }
            else
            {
                Debug.Log($"[Meta Ignorada] {horseName} pasó por la meta pero aún está en el Waypoint #{currentWaypointIndex} (Mínimo requerido: #{minWaypointToFinish}).");
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
