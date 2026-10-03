using System.Collections.Generic;
using UnityEngine;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

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

    // Diccionario/Estructura interna para recordar cuánto se apostó a cada caballo
    public Dictionary<string, float> currentBetsPerHorse = new Dictionary<string, float>();

    // Referencia a la casa de apuestas activa
    public BettingHouseUI activeBettingHouse;

    public BettingHouseUI currentHouse
    {
        get => activeBettingHouse;
        set => activeBettingHouse = value;
    }

    [Header("Registro de Pantallas de UI")]
    public List<BetScreen> allBetScreens = new List<BetScreen>();

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
    /// Confirmación de apuestas enviada desde BettingHouseUI.
    /// Registra el desglose de dinero apostado por cada caballo individualmente.
    /// </summary>
    public bool ConfirmBetFromHouse(BettingHouseUI house, float totalAmount)
    {
        if (hasActiveBet)
        {
            Debug.LogWarning("[GlobalBettingManager] Ya hay una apuesta activa.");
            return false;
        }

        if (playerMoney < totalAmount)
        {
            Debug.LogWarning("[GlobalBettingManager] Dinero insuficiente.");
            return false;
        }

        playerMoney -= totalAmount;
        activeBetAmount = totalAmount;
        activeBettingHouse = house;
        hasActiveBet = true;

        // Guardar apuestas individuales de cada caballo
        currentBetsPerHorse.Clear();
        foreach (var h in house.horses)
        {
            if (h.currentBet > 0)
            {
                currentBetsPerHorse[h.horseName] = h.currentBet;
                Debug.Log($"[GlobalBettingManager] Registrada apuesta de {h.currentBet}$ a {h.horseName}");
            }
        }

        Debug.Log($"[GlobalBettingManager] Apuesta total de {totalAmount}$ confirmada en {house.houseName}. Dinero restante: {playerMoney}$");
        return true;
    }

    /// <summary>
    /// Procesa el resultado de la carrera evaluando todas las apuestas realizadas.
    /// </summary>
    public void ProcessRaceResults(List<HorseRunner> finishedOrder)
    {
        if (!hasActiveBet || activeBettingHouse == null)
        {
            Debug.LogWarning("[GlobalBettingManager] Se intentó procesar la carrera pero no había apuesta activa.");
            return;
        }

        float totalEarnings = 0f;
        int winningBets = 0;

        // Evaluar hasta los 3 primeros puestos (Podio)
        for (int i = 0; i < Mathf.Min(3, finishedOrder.Count); i++)
        {
            HorseRunner runner = finishedOrder[i];

            // Búsqueda del caballo ignorando mayúsculas/minúsculas y espacios
            HorseUIElement horseData = activeBettingHouse.horses.Find(h =>
                h.horseName.Trim().Equals(runner.horseName.Trim(), System.StringComparison.OrdinalIgnoreCase));

            if (horseData != null && horseData.currentBet > 0)
            {
                float multiplier = i switch
                {
                    0 => horseData.mult1st,
                    1 => horseData.mult2nd,
                    2 => horseData.mult3rd,
                    _ => 0f
                };

                float payout = horseData.currentBet * multiplier;
                totalEarnings += payout;
                winningBets++;

                Debug.Log($"[GlobalBettingManager] ¡{horseData.horseName} quedó en #{i + 1}! Apuesta: {horseData.currentBet}$ x {multiplier:F2}x = {payout}$");
            }
        }

        // Registrar balance para la UI de resultados
        lastRaceTotalBet = activeBetAmount;
        lastRaceTotalEarnings = totalEarnings;
        lastRaceNetProfit = totalEarnings - activeBetAmount; // Puede ser positivo (ganancia) o negativo (pérdida)
        lastRaceWinningBetsCount = winningBets;

        // Sumar al saldo global las ganancias brutas obtenidas
        playerMoney += totalEarnings;

        Debug.Log($"[GlobalBettingManager] Fin de carrera. Total Apostado: {lastRaceTotalBet}$. Recuperado: {totalEarnings}$. Beneficio Neto: {lastRaceNetProfit}$. Nuevo Saldo: {playerMoney}$");

        ResetBetsState();
    }

    public void ResetBetsState()
    {
        if (activeBettingHouse != null)
        {
            foreach (var horse in activeBettingHouse.horses)
            {
                horse.currentBet = 0f;
            }
        }

        currentBetsPerHorse.Clear();
        hasActiveBet = false;
        activeBetAmount = 0f;
    }
}
