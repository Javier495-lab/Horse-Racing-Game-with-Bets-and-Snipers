using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BettingHouseUI : MonoBehaviour
{
    [System.Serializable]
    public class HorseUIElement
    {
        public string horseName = "Caballo";
        [Range(0.01f, 1f)] public float winProbability = 0.125f;
        public float currentBet = 0f;

        [Header("Referencias UI")]
        public GameObject panelRoot;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI betText;
        public Button selectButton;
        public GameObject controlsGroup;
        public Button plusButton;
        public Button minusButton;

        [HideInInspector] public float mult1st, mult2nd, mult3rd;
    }

    [Header("Configuración de esta Casa de Apuestas")]
    public string houseName = "Casa VIP";
    [Tooltip("Cantidad de dinero que se suma/resta en cada clic (ej: 100, 500)")]
    public float betStep = 100f;
    [Tooltip("Cantidad máxima de dinero que se permite apostar en total en esta casa")]
    public float maxBetLimit = 5000f;

    [Header("Configuración de Pagos Top 3")]
    public float factor1st = 1.0f;
    public float factor2nd = 0.5f;
    public float factor3rd = 0.25f;

    [Header("Lista de 8 Caballos")]
    public List<HorseUIElement> horses = new List<HorseUIElement>(8);

    [Header("Botones y Textos UI")]
    public Button confirmBetsButton;
    public TextMeshProUGUI statusMessageText;

    private int selectedHorseIndex = -1;

    private void Start()
    {
        SetupUIEvents();
        RandomizeInitialProbabilities();
        CalculateOdds();
        UpdateAllUI();
    }

    private void SetupUIEvents()
    {
        for (int i = 0; i < horses.Count; i++)
        {
            int index = i;

            if (horses[i].selectButton != null)
                horses[i].selectButton.onClick.AddListener(() => SelectHorse(index));

            if (horses[i].plusButton != null)
            {
                horses[i].plusButton.onClick.AddListener(() => ChangeBet(index, betStep));
                // Actualizar texto del botón a +100, +500, etc.
                TextMeshProUGUI btnText = horses[i].plusButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"+{betStep}";
            }

            if (horses[i].minusButton != null)
            {
                horses[i].minusButton.onClick.AddListener(() => ChangeBet(index, -betStep));
                // Actualizar texto del botón a -100, -500, etc.
                TextMeshProUGUI btnText = horses[i].minusButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"-{betStep}";
            }
        }

        if (confirmBetsButton != null)
            confirmBetsButton.onClick.AddListener(ConfirmBets);
    }

    public void RandomizeInitialProbabilities()
    {
        foreach (var h in horses)
            h.winProbability = UnityEngine.Random.Range(1.0f, 10.0f);

        NormalizeProbabilities();
    }

    private void NormalizeProbabilities()
    {
        float totalSum = 0f;
        foreach (var h in horses)
        {
            if (h.winProbability <= 0f) h.winProbability = 0.1f;
            totalSum += h.winProbability;
        }

        foreach (var h in horses)
            h.winProbability = h.winProbability / totalSum;
    }

    private void CalculateOdds()
    {
        foreach (var h in horses)
        {
            float risk = (1f - h.winProbability) / h.winProbability;
            h.mult1st = 1f + (risk * factor1st);
            h.mult2nd = 1f + (risk * factor2nd);
            h.mult3rd = 1f + (risk * factor3rd);
        }
    }

    public void SelectHorse(int index)
    {
        if (GlobalBettingManager.Instance.hasActiveBet) return;
        selectedHorseIndex = index;
        UpdateControlsVisibility();
    }

    public void ChangeBet(int index, float amount)
    {
        if (GlobalBettingManager.Instance.hasActiveBet) return;

        HorseUIElement horse = horses[index];
        float currentTotalHouseBets = GetTotalBetsAmount();

        if (amount > 0)
        {
            // 1. Validar límite máximo de la casa
            if (currentTotalHouseBets + amount > maxBetLimit)
            {
                SetStatus($"¡Límite máximo de apuesta para esta casa alcanzado ({maxBetLimit}$)! ");
                return;
            }

            // 2. Validar dinero global disponible
            if (GlobalBettingManager.Instance.playerMoney - currentTotalHouseBets >= amount)
            {
                horse.currentBet += amount;
            }
            else
            {
                SetStatus("¡No tienes suficiente dinero global disponible!");
            }
        }
        else if (amount < 0)
        {
            horse.currentBet = Mathf.Max(0f, horse.currentBet + amount);
        }

        UpdateAllUI();
    }

    public float GetTotalBetsAmount()
    {
        float total = 0f;
        foreach (var h in horses) total += h.currentBet;
        return total;
    }

    public void ConfirmBets()
    {
        float total = GetTotalBetsAmount();
        if (total <= 0)
        {
            SetStatus("Debes apostar al menos a un caballo.");
            return;
        }

        // Llamar al Manager Central para fijar la apuesta activa y bloquear todo
        bool success = GlobalBettingManager.Instance.ConfirmBetFromHouse(this, total);

        if (success)
        {
            SetStatus($"¡Apuesta de {total}$ confirmada! Todas las casas han cerrado apuestas para esta carrera.");
            selectedHorseIndex = -1;
            UpdateAllUI();

            // Cerrar o desactivar interfaz activa si fuera necesario
            BetScreen screen = GetComponentInParent<BetScreen>();
            if (screen != null)
            {
                screen.OnReturn();
            }
        }
    }

    public void UpdateAllUI()
    {
        for (int i = 0; i < horses.Count; i++)
        {
            HorseUIElement h = horses[i];

            if (h.infoText != null)
            {
                float probPct = h.winProbability * 100f;
                h.infoText.text = $"<b>{h.horseName}</b> ({probPct:F1}%)\n" +
                                  $"<color=#FFD700>1º: {h.mult1st:F2}x</color> | " +
                                  $"<color=#C0C0C0>2º: {h.mult2nd:F2}x</color> | " +
                                  $"<color=#CD7F32>3º: {h.mult3rd:F2}x</color>";
            }

            if (h.betText != null)
                h.betText.text = $"Apuesta: {h.currentBet:F0} $";
        }

        UpdateControlsVisibility();

        if (confirmBetsButton != null)
        {
            bool hasBet = GetTotalBetsAmount() > 0;
            bool isLocked = GlobalBettingManager.Instance.hasActiveBet;
            confirmBetsButton.interactable = hasBet && !isLocked;
        }
    }

    private void UpdateControlsVisibility()
    {
        bool isLocked = GlobalBettingManager.Instance.hasActiveBet;
        for (int i = 0; i < horses.Count; i++)
        {
            if (horses[i].controlsGroup != null)
            {
                bool isSelected = (i == selectedHorseIndex) && !isLocked;
                horses[i].controlsGroup.SetActive(isSelected);
            }
        }
    }

    private void SetStatus(string message)
    {
        if (statusMessageText != null)
            statusMessageText.text = message;
    }
}
