using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                // Usamos la probabilidad base + modificadores de cada caballo
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

            // Configurar el rango/puesto asignado al caballo
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
        int activeOrFinished = 0;
        foreach (var r in runners)
        {
            if (r.hasFinished || r.hasFallen) activeOrFinished++;
        }

        if (activeOrFinished >= runners.Count && isRaceActive)
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

        // 3. Activar el panel de resultados. Al activarse, disparará OnEnable() en RaceResultsUI
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
