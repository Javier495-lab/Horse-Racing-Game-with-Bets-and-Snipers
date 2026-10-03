using TMPro;
using UnityEngine;

public class RaceResultsUI : MonoBehaviour
{
    [Header("Referencias de Texto UI")]
    [SerializeField] private TextMeshProUGUI titleText;         // Ej: "¡RESULTADOS DE LA CARRERA!"
    [SerializeField] private TextMeshProUGUI totalBetText;      // Ej: "Total Apostado: 300$"
    [SerializeField] private TextMeshProUGUI netProfitText;     // Ej: "Beneficio Neto: +450$" o "-100$"
    [SerializeField] private TextMeshProUGUI newBalanceText;    // Ej: "Saldo Actual: 1,450$"

    [Header("Configuración de Colores")]
    [SerializeField] private Color winColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color lossColor = new Color(0.9f, 0.2f, 0.2f);

    private void OnEnable()
    {
        UpdateResultsUI();
    }

    public void UpdateResultsUI()
    {
        if (GlobalBettingManager.Instance == null) return;

        var gbm = GlobalBettingManager.Instance;

        float totalBet = gbm.lastRaceTotalBet;
        float netProfit = gbm.lastRaceNetProfit;
        float currentMoney = gbm.playerMoney;
        int winners = gbm.lastRaceWinningBetsCount;

        // 1. Mostrar gasto total realizado
        if (totalBetText != null)
        {
            totalBetText.text = $"Inversión Total en Apuestas: <color=#FFD700>{totalBet:F0} $</color>";
        }

        // 2. Cálculo del Beneficio Neto
        if (netProfit > 0)
        {
            if (titleText != null)
            {
                titleText.text = winners > 1 ? "¡APUESTAS GANADORAS!" : "¡APUESTA GANADORA!";
                titleText.color = winColor;
            }

            if (netProfitText != null)
            {
                netProfitText.text = $"Beneficio Neto: <color=#00FF00>+{netProfit:F0} $</color>";
            }
        }
        else if (netProfit < 0)
        {
            if (titleText != null)
            {
                titleText.text = "RESULTADO EN PÉRDIDAS";
                titleText.color = lossColor;
            }

            if (netProfitText != null)
            {
                // Muestra la pérdida en negativo
                netProfitText.text = $"Pérdida Neta: <color=#FF0000>{netProfit:F0} $</color>";
            }
        }
        else
        {
            if (titleText != null) titleText.text = "SIN BENEFICIOS NI PÉRDIDAS";
            if (netProfitText != null) netProfitText.text = "Beneficio Neto: 0 $";
        }

        // 3. Saldo Total del Jugador
        if (newBalanceText != null)
        {
            newBalanceText.text = $"Saldo Total Actual: {currentMoney:F0} $";
        }
    }
}
