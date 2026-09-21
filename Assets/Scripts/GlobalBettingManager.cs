using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

    [Header("Economía Global")]
    public float playerMoney = 5000f;
    public TextMeshProUGUI globalMoneyText;

    [Header("Estado Global de Apuestas")]
    public bool hasActiveBet = false;
    public BettingHouseUI activeBettingHouse = null;

    [Header("Terminales Registradas")]
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

    public bool ConfirmBetFromHouse(BettingHouseUI house, float totalBetAmount)
    {
        if (hasActiveBet)
        {
            Debug.LogWarning("Ya hay una apuesta activa confirmada.");
            return false;
        }

        if (totalBetAmount > playerMoney)
        {
            Debug.LogWarning("Dinero global insuficiente.");
            return false;
        }

        playerMoney -= totalBetAmount;
        hasActiveBet = true;
        activeBettingHouse = house;

        UpdateGlobalMoneyUI();
        LockAllBetScreens();

        Debug.Log($"¡Apuesta confirmada en '{house.houseName}' por {totalBetAmount}$!");
        return true;
    }

    public void LockAllBetScreens()
    {
        foreach (var screen in allBetScreens)
        {
            if (screen != null) screen.SetInteractionLocked(true);
        }
    }

    public void UnlockAllBetScreens()
    {
        hasActiveBet = false;
        activeBettingHouse = null;

        foreach (var screen in allBetScreens)
        {
            if (screen != null) screen.SetInteractionLocked(false);
        }
    }
}
