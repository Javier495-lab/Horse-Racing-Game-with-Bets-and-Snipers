using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Configuración de Carrera")]
    public List<HorseRunner> runners = new List<HorseRunner>();
    public Transform finishLineTransform;

    [Header("Posiciones de Salida")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("UI de Carrera")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI raceStatusText;
    public GameObject resultsPanel;
    public TextMeshProUGUI podiumText;

    [Header("Estado")]
    public bool isRaceActive = false;
    private List<HorseRunner> finishedOrder = new List<HorseRunner>();

    [Header("Ruta NavMesh")]
    public List<Transform> circuitWaypoints = new List<Transform>();

    [Header("Configuración de Escenas")]
    public string bettingHouseSceneName;
    public float fadeDuration = 2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeRace();
    }

    public void InitializeRace()
    {
        isRaceActive = false;
        finishedOrder.Clear();

        if (resultsPanel != null) resultsPanel.SetActive(false);

        // 1. Cargar probabilidades e información desde la casa de apuestas activa
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.currentHouse != null)
        {
            var house = GlobalBettingManager.Instance.currentHouse;
            for (int i = 0; i < runners.Count && i < house.horses.Count; i++)
            {
                runners[i].SetupHorseData(house.horses[i]);
            }
        }

        // 2. Precalcular el resultado y puestos de la carrera según probabilidades (Weighted Random)
        PrecalculateRaceOutcome();

        // 3. Posicionar caballos en parrilla y preparar waypoints
        for (int i = 0; i < runners.Count; i++)
        {
            if (i < spawnPoints.Count && spawnPoints[i] != null)
            {
                runners[i].transform.position = spawnPoints[i].position;
                runners[i].transform.rotation = spawnPoints[i].rotation;
            }

            runners[i].SetWaypoints(circuitWaypoints);
            runners[i].ResetRunner();
        }

        StartCoroutine(StartCountdownRoutine());
    }

    /// <summary>
    /// Sortea las posiciones finales según la probabilidad acumulada de cada caballo.
    /// </summary>
    private void PrecalculateRaceOutcome()
    {
        List<HorseRunner> pool = new List<HorseRunner>(runners);
        int currentRank = 1;

        while (pool.Count > 0)
        {
            float totalWeight = 0f;
            foreach (var r in pool)
            {
                float prob = Mathf.Max(0.01f, r.baseWinProbability + r.winProbabilityModifier);
                totalWeight += prob;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            HorseRunner selectedRunner = null;
            foreach (var r in pool)
            {
                float prob = Mathf.Max(0.01f, r.baseWinProbability + r.winProbabilityModifier);
                cumulative += prob;

                if (roll <= cumulative)
                {
                    selectedRunner = r;
                    break;
                }
            }

            if (selectedRunner == null) selectedRunner = pool[0];

            selectedRunner.ConfigureScriptedBehavior(currentRank, runners.Count);
            currentRank++;

            pool.Remove(selectedRunner);
        }
    }

    private IEnumerator StartCountdownRoutine()
    {
        int timer = 3;
        while (timer > 0)
        {
            if (countdownText != null) countdownText.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
        }

        if (countdownText != null) countdownText.text = "¡GO!";
        StartRace();

        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void StartRace()
    {
        isRaceActive = true;
        foreach (var runner in runners)
        {
            runner.StartRunning();
        }

        if (raceStatusText != null) raceStatusText.text = "¡Carrera en curso!";
    }

    /// <summary>
    /// Calcula el puesto en vivo de un caballo basándose en si ya llegó o en su avance por la ruta.
    /// </summary>
    public int GetLiveHorseRank(HorseRunner targetRunner)
    {
        if (targetRunner == null) return -1;

        // Si ya está fuera de la carrera por caída o disparo
        if (targetRunner.hasFallen || targetRunner.hasBeenShot) return -1;

        // Si ya ha cruzado la meta, devolver su posición definitiva
        if (targetRunner.hasFinished)
        {
            return finishedOrder.IndexOf(targetRunner) + 1;
        }

        // Para los caballos en carrera, contamos cuántos van por delante de él
        int rank = finishedOrder.Count + 1;

        foreach (var other in runners)
        {
            if (other == targetRunner || other.hasFinished || other.hasFallen || other.hasBeenShot) continue;

            // Comparar progreso por el índice del waypoint
            if (other.currentWaypointIndex > targetRunner.currentWaypointIndex)
            {
                rank++;
            }
            else if (other.currentWaypointIndex == targetRunner.currentWaypointIndex)
            {
                // Si están en el mismo waypoint, el que tenga menor distancia restante va por delante
                var agentOther = other.GetComponent<UnityEngine.AI.NavMeshAgent>();
                var agentTarget = targetRunner.GetComponent<UnityEngine.AI.NavMeshAgent>();

                if (agentOther != null && agentTarget != null && agentOther.enabled && agentTarget.enabled)
                {
                    if (agentOther.remainingDistance < agentTarget.remainingDistance)
                    {
                        rank++;
                    }
                }
            }
        }

        return rank;
    }

    /// <summary>
    /// Llamado desde HorseRunner cuando cruza la meta.
    /// </summary>
    public void OnHorseFinished(HorseRunner runner)
    {
        if (!finishedOrder.Contains(runner))
        {
            finishedOrder.Add(runner);
            Debug.Log($"¡{runner.horseName} ha cruzado la meta en la posición #{finishedOrder.Count}!");

            if (raceStatusText != null)
                raceStatusText.text = $"Último en llegar: {runner.horseName} (#{finishedOrder.Count})";

            CheckRaceCompletion();
        }
    }

    public void CheckRaceCompletion()
    {
        int inactiveOrFinished = 0;
        foreach (var r in runners)
        {
            // Se contabiliza si terminó, se cayó o recibió un disparo
            if (r.hasFinished || r.hasFallen || r.hasBeenShot) inactiveOrFinished++;
        }

        if (inactiveOrFinished >= runners.Count && isRaceActive)
        {
            EndRace();
        }
    }

    private void EndRace()
    {
        isRaceActive = false;
        if (raceStatusText != null) raceStatusText.text = "¡Carrera finalizada!";

        ShowResults();
    }

    private void ShowResults()
    {
        // 1. Procesar primero la economía en GlobalBettingManager para calcular las ganancias
        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.ProcessRaceResults(finishedOrder);
        }

        // 2. Rellenar el texto de podio simple
        if (podiumText != null)
        {
            string text = "<b>--- PODIO ---</b>\n\n";
            for (int i = 0; i < finishedOrder.Count; i++)
            {
                string medal = i switch
                {
                    0 => "🥇 1º",
                    1 => "🥈 2º",
                    2 => "🥉 3º",
                    _ => $"{i + 1}º"
                };
                text += $"{medal}: {finishedOrder[i].horseName}\n";
            }
            podiumText.text = text;
        }

        // 3. Activar el panel de resultados
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }
    }

    public void ReturnToBettingHouse()
    {
        if (ScreenFade.Instance != null)
        {
            ScreenFade.Instance.LoadSceneWithFade(bettingHouseSceneName, fadeDuration);
        }
        else
        {
            Debug.LogWarning("ScreenFade.Instance no encontrado. Cargando escena directamente...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(bettingHouseSceneName);
        }
    }
}
