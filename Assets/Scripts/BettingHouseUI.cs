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

        [Header("Referencias UI Locales")]
        public GameObject panelRoot;
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI betText;
        public Button selectButton;
        public GameObject controlsGroup;
        public Button plusButton;
        public Button minusButton;

        [HideInInspector] public float mult1st, mult2nd, mult3rd;
    }

    [Header("Configuración de esta Casa")]
    public string houseName = "Casa de Apuestas";
    public float betStep = 100f;
    public float maxBetLimit = 5000f;

    [Header("Multiplicadores")]
    public float factor1st = 1.0f;
    public float factor2nd = 0.5f;
    public float factor3rd = 0.25f;

    [Header("Lista de 8 Caballos")]
    public List<HorseUIElement> horses = new List<HorseUIElement>(8);

    [Header("UI General")]
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

    public void SetupUIEvents()
    {
        for (int i = 0; i < horses.Count; i++)
        {
            int index = i;

            if (horses[i].selectButton != null)
            {
                horses[i].selectButton.onClick.RemoveAllListeners();
                horses[i].selectButton.onClick.AddListener(() => SelectHorse(index));
            }

            if (horses[i].plusButton != null)
            {
                horses[i].plusButton.onClick.RemoveAllListeners();
                horses[i].plusButton.onClick.AddListener(() => ChangeBet(index, betStep));

                TextMeshProUGUI btnText = horses[i].plusButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"+{betStep}";
            }

            if (horses[i].minusButton != null)
            {
                horses[i].minusButton.onClick.RemoveAllListeners();
                horses[i].minusButton.onClick.AddListener(() => ChangeBet(index, -betStep));

                TextMeshProUGUI btnText = horses[i].minusButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"-{betStep}";
            }
        }

        if (confirmBetsButton != null)
        {
            confirmBetsButton.onClick.RemoveAllListeners();
            confirmBetsButton.onClick.AddListener(ConfirmBets);
        }
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
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet) return;
        selectedHorseIndex = index;
        UpdateControlsVisibility();
    }

    public void ChangeBet(int index, float amount)
    {
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet) return;

        HorseUIElement horse = horses[index];
        float currentTotalHouseBets = GetTotalBetsAmount();

        if (amount > 0)
        {
            if (currentTotalHouseBets + amount > maxBetLimit)
            {
                SetStatus($"¡Límite máximo alcanzado ({maxBetLimit}$)! ");
                return;
            }

            if (GlobalBettingManager.Instance != null &&
                (GlobalBettingManager.Instance.playerMoney - currentTotalHouseBets >= amount))
            {
                horse.currentBet += amount;
            }
            else
            {
                SetStatus("¡No tienes suficiente dinero global!");
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

        bool success = GlobalBettingManager.Instance.ConfirmBetFromHouse(this, total);

        if (success)
        {
            SetStatus($"¡Apuesta de {total}$ confirmada!");
            selectedHorseIndex = -1;
            UpdateAllUI();

            BetScreen screen = GetComponentInParent<BetScreen>();
            if (screen != null)
            {
                screen.OnReturn();
            }
        }
    }

    public void DisableAllButtons()
    {
        foreach (var h in horses)
        {
            if (h.selectButton != null) h.selectButton.interactable = false;
            if (h.plusButton != null) h.plusButton.interactable = false;
            if (h.minusButton != null) h.minusButton.interactable = false;
            if (h.controlsGroup != null) h.controlsGroup.SetActive(false);
        }

        if (confirmBetsButton != null) confirmBetsButton.interactable = false;
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
            bool isLocked = GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet;
            confirmBetsButton.interactable = hasBet && !isLocked;
        }
    }

    private void UpdateControlsVisibility()
    {
        bool isLocked = GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet;
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
