using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        for (int i = 0; i < runners.Count; i++)
        {
            // 1. Posicionar en SpawnPoints
            if (i < spawnPoints.Count && spawnPoints[i] != null)
            {
                runners[i].transform.position = spawnPoints[i].position;
                runners[i].transform.rotation = spawnPoints[i].rotation;
            }

            // 2. Asignar waypoints y resetear
            runners[i].SetWaypoints(circuitWaypoints);
            runners[i].ResetRunner();
        }

        // 3. Cargar probabilidades desde la casa de apuestas
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.currentHouse != null)
        {
            var house = GlobalBettingManager.Instance.currentHouse;
            for (int i = 0; i < runners.Count && i < house.horses.Count; i++)
            {
                runners[i].SetupHorseData(house.horses[i]);
            }
        }

        StartCoroutine(StartCountdownRoutine());
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

            // Si todos los caballos que no cayeron terminaron
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
        if (resultsPanel != null) resultsPanel.SetActive(true);

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

        if (podiumText != null) podiumText.text = text;

        // Notificar resultados al manager global de apuestas si existe
        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.ProcessRaceResults(finishedOrder);
        }
    }
}
