using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalBettingManager : MonoBehaviour
{
    public static GlobalBettingManager Instance { get; private set; }

    [Header("Economía Global")]
    public float playerMoney = 1000f;
    public TextMeshProUGUI globalMoneyText;

    [Header("Estado Global de Apuestas")]
    public bool hasActiveBet = false;
    public BettingHouseUI activeBettingHouse = null;

    [Header("Terminales Registradas en la Escena Actual")]
    public List<BetScreen> allBetScreens = new List<BetScreen>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Al hacer persistente la raíz, ScreenFader (hijo) también se vuelve persistente
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateGlobalMoneyUI();
    }

    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
            globalMoneyText.text = $"Dinero Global: {playerMoney:F0} $";
    }

    public bool ConfirmBetFromHouse(BettingHouseUI house, float totalBetAmount)
    {
        if (hasActiveBet || totalBetAmount > playerMoney)
            return false;

        playerMoney -= totalBetAmount;
        hasActiveBet = true;
        activeBettingHouse = house;

        UpdateGlobalMoneyUI();
        LockAllBetScreens();

        return true;
    }

    public void DisableAllRaycasters()
    {
        foreach (var screen in allBetScreens)
        {
            if (screen != null)
            {
                var gr = screen.GetComponentInChildren<UnityEngine.UI.GraphicRaycaster>(true);
                if (gr != null) gr.enabled = false;
            }
        }
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
