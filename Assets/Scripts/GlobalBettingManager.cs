using System.Collections.Generic;
using UnityEngine;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

    [System.Serializable]
    public class ActiveBetData
    {
        public string horseName;
        public float amount;
        public float mult1st;
        public float mult2nd;
        public float mult3rd;
    }

    [Header("Economía Global")]
    public float playerMoney = 1000f;

    [Header("Estado de las Apuestas")]
    public bool hasActiveBet = false;
    public float activeBetAmount = 0f;

    [Header("Últimos Resultados de Carrera (Multiapuesta)")]
    public float lastRaceTotalBet = 0f;      // Total apostado entre todos los caballos
    public float lastRaceTotalEarnings = 0f; // Ingresos brutos recuperados del podio
    public float lastRaceNetProfit = 0f;     // Beneficio neto (Ganancias - Total Apostado)
    public int lastRaceWinningBetsCount = 0; // Cuántas de las apuestas entraron en podio

    // Estructura PERSISTENTE que sobrevive al cambio de escena
    [Header("Apuestas Activas Persistentes")]
    public List<ActiveBetData> currentActiveBets = new List<ActiveBetData>();

    // Referencia temporal a la casa activa (Solo usada mientras se está en la escena de apuestas)
    [HideInInspector] public BettingHouseUI currentHouse;

    [Header("Registro de Pantallas de UI")]
    public List<BetScreen> allBetScreens = new List<BetScreen>();

    [Header("Escena Objetivo")]
    public string targetTrackSceneName = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterBetScreen(BetScreen screen)
    {
        if (screen != null && !allBetScreens.Contains(screen))
        {
            allBetScreens.Add(screen);
        }
    }

    public void DisableAllRaycasters()
    {
        for (int i = allBetScreens.Count - 1; i >= 0; i--)
        {
            if (allBetScreens[i] == null)
            {
                allBetScreens.RemoveAt(i);
                continue;
            }

            if (allBetScreens[i].graphicRaycaster != null)
            {
                allBetScreens[i].graphicRaycaster.enabled = false;
            }
        }
    }

    /// <summary>
    /// Registra las apuestas copiando los datos a una estructura independiente que no se borra al cambiar de escena.
    /// </summary>
    public bool ConfirmBetFromHouse(BettingHouseUI house, float totalAmount)
    {
        if (hasActiveBet) return false;
        if (playerMoney < totalAmount) return false;

        playerMoney -= totalAmount;
        activeBetAmount = totalAmount;
        currentHouse = house;
        targetTrackSceneName = house.trackSceneName; // 👈 Guardamos el nombre de la pista de forma persistente
        hasActiveBet = true;

        currentActiveBets.Clear();

        // Copiar los datos de cada apuesta a la estructura persistente
        foreach (var h in house.horses)
        {
            if (h.currentBet > 0)
            {
                ActiveBetData bet = new ActiveBetData
                {
                    horseName = h.horseName,
                    amount = h.currentBet,
                    mult1st = h.mult1st,
                    mult2nd = h.mult2nd,
                    mult3rd = h.mult3rd
                };
                currentActiveBets.Add(bet);
                Debug.Log($"[GlobalBettingManager] Apuesta guardada: {h.currentBet}$ a {h.horseName} (1º:{h.mult1st:F2}x, 2º:{h.mult2nd:F2}x, 3º:{h.mult3rd:F2}x)");
            }
        }

        Debug.Log($"[GlobalBettingManager] Apuesta total de {totalAmount}$ confirmada. Dinero restante: {playerMoney}$");
        return true;
    }

    /// <summary>
    /// Procesa el resultado de la carrera usando la lista persistente 'currentActiveBets'.
    /// </summary>
    public void ProcessRaceResults(List<HorseRunner> finishedOrder)
    {
        if (!hasActiveBet || currentActiveBets.Count == 0)
        {
            Debug.LogWarning("[GlobalBettingManager] Se intentó procesar la carrera pero no había apuestas registradas.");
            return;
        }

        float totalEarnings = 0f;
        int winningBets = 0;

        // Evaluar el Podio (los 3 primeros que cruzaron la meta)
        for (int i = 0; i < Mathf.Min(3, finishedOrder.Count); i++)
        {
            HorseRunner runner = finishedOrder[i];

            // Buscar si apostamos por este caballo
            ActiveBetData bet = currentActiveBets.Find(b =>
                b.horseName.Trim().Equals(runner.horseName.Trim(), System.StringComparison.OrdinalIgnoreCase));

            if (bet != null && bet.amount > 0)
            {
                float multiplier = i switch
                {
                    0 => bet.mult1st,
                    1 => bet.mult2nd,
                    2 => bet.mult3rd,
                    _ => 0f
                };

                float payout = bet.amount * multiplier;
                totalEarnings += payout;
                winningBets++;

                Debug.Log($"[GlobalBettingManager] ¡{bet.horseName} quedó en #{i + 1}! Apuesta: {bet.amount}$ x {multiplier:F2}x = {payout}$");
            }
        }

        // Asignar variables globales para la UI
        lastRaceTotalBet = activeBetAmount;
        lastRaceTotalEarnings = totalEarnings;
        lastRaceNetProfit = totalEarnings - activeBetAmount;
        lastRaceWinningBetsCount = winningBets;

        // Sumar ingresos de apuestas ganadoras
        playerMoney += totalEarnings;

        Debug.Log($"[GlobalBettingManager] Resultados liquidados. Inversión: {lastRaceTotalBet}$. Recuperado: {totalEarnings}$. Neto: {lastRaceNetProfit}$. Saldo actual: {playerMoney}$");

        ResetBetsState();
    }

    public void ResetBetsState()
    {
        if (currentHouse != null)
        {
            foreach (var horse in currentHouse.horses)
            {
                horse.currentBet = 0f;
            }
        }

        currentActiveBets.Clear();
        hasActiveBet = false;
        activeBetAmount = 0f;
    }
}
