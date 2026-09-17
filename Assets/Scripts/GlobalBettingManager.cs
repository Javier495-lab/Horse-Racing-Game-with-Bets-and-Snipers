using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

    [Header("Economía Compartida del Jugador")]
    public float playerMoney = 5000f;
    public TextMeshProUGUI globalMoneyText;

    [Header("Estado Global de Apuestas")]
    public bool hasActiveBet = false;
    public BettingHouseUI activeBettingHouse = null;

    [Header("Lista de las 5 Pantallas de Apuestas 3D")]
    public List<BetScreen> allBetScreens = new List<BetScreen>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UpdateGlobalMoneyUI();
    }

    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
            globalMoneyText.text = $"Dinero Global: {playerMoney:F0} $";
    }

    /// <summary>
    /// Registra la apuesta confirmada de una casa y bloquea todas las terminales.
    /// </summary>
    public bool ConfirmBetFromHouse(BettingHouseUI house, float totalBetAmount)
    {
        if (hasActiveBet)
        {
            Debug.LogWarning("Ya hay una apuesta activa confirmada para esta carrera.");
            return false;
        }

        if (totalBetAmount > playerMoney)
        {
            Debug.LogWarning("No tienes suficiente dinero global.");
            return false;
        }

        // Restar dinero y registrar la apuesta activa
        playerMoney -= totalBetAmount;
        hasActiveBet = true;
        activeBettingHouse = house;

        UpdateGlobalMoneyUI();

        // Cancelar y bloquear la interacción en TODAS las casas de apuestas
        LockAllBetScreens();

        Debug.Log($"¡Apuesta confirmada en la casa '{house.houseName}' por un total de {totalBetAmount}$!");
        return true;
    }

    /// <summary>
    /// Desactiva la interacción en los 5 interactuables BetScreen.
    /// </summary>
    public void LockAllBetScreens()
    {
        foreach (var screen in allBetScreens)
        {
            if (screen != null)
            {
                screen.SetInteractionLocked(true);
            }
        }
    }

    /// <summary>
    /// Reactiva las terminales (se llamará en el futuro tras concluir la simulación).
    /// </summary>
    public void UnlockAllBetScreens()
    {
        hasActiveBet = false;
        activeBettingHouse = null;

        foreach (var screen in allBetScreens)
        {
            if (screen != null)
            {
                screen.SetInteractionLocked(false);
            }
        }
    }
}
