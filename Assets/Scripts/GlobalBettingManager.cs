using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

    [Header("Economía Global")]
    public float playerMoney = 1000f;

    [Header("Estado de las Apuestas")]
    public bool hasActiveBet = false;
    public float activeBetAmount = 0f;

    // Referencia a la casa de apuestas activa
    public BettingHouseUI activeBettingHouse;

    // Alias para compatibilidad con RaceManager y otros scripts
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

    /// <summary>
    /// Registra una pantalla de apuestas en la lista global.
    /// </summary>
    public void RegisterBetScreen(BetScreen screen)
    {
        if (screen != null && !allBetScreens.Contains(screen))
        {
            allBetScreens.Add(screen);
        }
    }

    /// <summary>
    /// Desactiva el GraphicRaycaster de todas las pantallas de apuestas registradas.
    /// </summary>
    public void DisableAllRaycasters()
    {
        for (int i = allBetScreens.Count - 1; i >= 0; i--)
        {
            if (allBetScreens[i] == null)
            {
                allBetScreens.RemoveAt(i);
                continue;
            }

            // Desactiva el raycaster si existe
            if (allBetScreens[i].graphicRaycaster != null)
            {
                allBetScreens[i].graphicRaycaster.enabled = false;
            }
        }
    }

    /// <summary>
    /// Confirmación de apuesta enviada desde BettingHouseUI.
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

        Debug.Log($"[GlobalBettingManager] Apuesta de {totalAmount}$ confirmada en {house.houseName}. Dinero restante: {playerMoney}$");
        return true;
    }

    /// <summary>
    /// Procesa el resultado de la carrera cuando RaceManager notifica el podio.
    /// </summary>
    public void ProcessRaceResults(List<HorseRunner> finishedOrder)
    {
        if (!hasActiveBet || activeBettingHouse == null)
        {
            Debug.LogWarning("[GlobalBettingManager] Se intentó procesar la carrera pero no había apuesta activa.");
            return;
        }

        float totalEarnings = 0f;

        for (int i = 0; i < Mathf.Min(3, finishedOrder.Count); i++)
        {
            HorseRunner runner = finishedOrder[i];
            HorseUIElement horseData = activeBettingHouse.horses.Find(h => h.horseName == runner.horseName);

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

                Debug.Log($"[GlobalBettingManager] ¡{horseData.horseName} quedó en #{i + 1}! Apuesta: {horseData.currentBet}$ x {multiplier:F2}x = {payout}$");
            }
        }

        playerMoney += totalEarnings;
        Debug.Log($"[GlobalBettingManager] Fin de carrera. Ganancias totales: {totalEarnings}$. Nuevo total: {playerMoney}$");

        ResetBetsState();
    }

    /// <summary>
    /// Resetea el estado de las apuestas.
    /// </summary>
    public void ResetBetsState()
    {
        if (activeBettingHouse != null)
        {
            foreach (var horse in activeBettingHouse.horses)
            {
                horse.currentBet = 0f;
            }
        }

        hasActiveBet = false;
        activeBetAmount = 0f;
    }
}
