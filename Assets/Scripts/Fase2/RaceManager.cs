using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("Configuración de Pista")]
    public float trackLength = 200f;
    public List<HorseRunner> horses = new List<HorseRunner>(8);

    [Header("Condicionales de Carrera")]
    [Range(0f, 100f)] public float fallProbabilityPercentage = 7f;

    [Header("Estado")]
    public bool isRaceActive = false;
    public bool isRaceFinished = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PreSimulateRace();
    }

    private void PreSimulateRace()
    {
        List<BettingHouseUI.HorseUIElement> betHorses = null;
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.activeBettingHouse != null)
        {
            betHorses = GlobalBettingManager.Instance.activeBettingHouse.horses;
        }

        List<HorseSimData> simDataList = new List<HorseSimData>();

        for (int i = 0; i < horses.Count; i++)
        {
            float prob = (betHorses != null && i < betHorses.Count) ? betHorses[i].winProbability : 0.125f;
            string hName = (betHorses != null && i < betHorses.Count) ? betHorses[i].horseName : $"Caballo {i + 1}";

            bool willFall = Random.Range(0f, 100f) <= fallProbabilityPercentage;
            float fallNormalizedPos = willFall ? Random.Range(0.25f, 0.75f) : 1f;

            float performanceScore = willFall ? -100f : (prob * Random.Range(0.8f, 1.2f));

            simDataList.Add(new HorseSimData
            {
                horseIndex = i,
                horseName = hName,
                probability = prob,
                score = performanceScore,
                willFall = willFall,
                fallNormalizedPosition = fallNormalizedPos
            });
        }

        var rankedList = simDataList.OrderByDescending(x => x.score).ToList();

        for (int rank = 0; rank < rankedList.Count; rank++)
        {
            var data = rankedList[rank];
            HorseRunner runner = horses[data.horseIndex];

            runner.Initialize(
                data.horseName,
                targetRank: rank + 1,
                willFall: data.willFall,
                fallProgress: data.fallNormalizedPosition,
                trackLength: trackLength
            );
        }
    }

    public void StartRace()
    {
        if (isRaceActive) return;
        isRaceActive = true;

        foreach (var horse in horses)
        {
            horse.StartRunning();
        }
    }

    public void OnHorseFinished(HorseRunner horse)
    {
        bool allDone = horses.All(h => h.hasFinished || h.hasFallen);
        if (allDone && !isRaceFinished)
        {
            isRaceFinished = true;
            Debug.Log("🎉 ¡Carrera finalizada!");
        }
    }

    private class HorseSimData
    {
        public int horseIndex;
        public string horseName;
        public float probability;
        public float score;
        public bool willFall;
        public float fallNormalizedPosition;
    }
}
