using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace HorseRacing.CanvasUI
{
    [Serializable]
    public class HorseUIElement
    {
        [Header("Datos del Caballo")]
        public string horseName = "Caballo";
        [Range(0.01f, 1f)] public float winProbability = 0.125f; // 12.5% base
        public int currentBet = 0;

        [Header("Referencias UI (Canvas)")]
        public GameObject panelRoot;
        public TextMeshProUGUI infoText;      // Muestra: Nombre, %, Cuotas (1º, 2º, 3º)
        public TextMeshProUGUI betText;       // Muestra: "Apuestado: 30$"
        public Button selectButton;          // Botón principal del caballo
        public GameObject controlsGroup;     // Contenedor de los botones +10 / -10
        public Button plus10Button;
        public Button minus10Button;

        // Multiplicadores calculados
        [HideInInspector] public float mult1st;
        [HideInInspector] public float mult2nd;
        [HideInInspector] public float mult3rd;
    }
    public class HorseBetManager : MonoBehaviour
    {
        [Header("Economía del Jugador")]
        public float playerMoney = 1000f;
        public TextMeshProUGUI moneyText;

        [Header("Configuración de Pagos Top 3")]
        public float factor1st = 1.0f;  // 100% de beneficio por riesgo
        public float factor2nd = 0.5f;  // 50% de beneficio por riesgo
        public float factor3rd = 0.25f; // 25% de beneficio por riesgo

        [Header("Lista de 8 Caballos")]
        public List<HorseUIElement> horses = new List<HorseUIElement>(8);

        [Header("Botones Principales UI")]
        public Button confirmBetsButton;
        public Button simulateRaceButton;
        public TextMeshProUGUI statusMessageText;

        private int selectedHorseIndex = -1;
        private bool betsConfirmed = false;

        private void Start()
        {
            SetupUIEvents();
            RandomizeInitialProbabilities();
            NormalizeProbabilities();
            CalculateOdds();
            UpdateAllUI();
        }

        private void RandomizeInitialProbabilities()
        {
            if (horses == null || horses.Count == 0) return;

            // Asignamos a CADA caballo una puntuación aleatoria totalmente independiente
            for (int i = 0; i < horses.Count; i++)
            {
                // Genera un número decimal aleatorio entre 1.0 y 10.0 para cada posición
                horses[i].winProbability = UnityEngine.Random.Range(1.0f, 10.0f);
            }

            // Normalizamos los valores para que la suma total sea 1.0 (100%)
            NormalizeProbabilities();
        }
        // --- 1. CONFIGURACIÓN E INICIALIZACIÓN ---
        private void SetupUIEvents()
        {
            for (int i = 0; i < horses.Count; i++)
            {
                int index = i; // Copia local para la lambda

                if (horses[i].selectButton != null)
                    horses[i].selectButton.onClick.AddListener(() => SelectHorse(index));

                if (horses[i].plus10Button != null)
                    horses[i].plus10Button.onClick.AddListener(() => ChangeBet(index, 10));

                if (horses[i].minus10Button != null)
                    horses[i].minus10Button.onClick.AddListener(() => ChangeBet(index, -10));
            }

            if (confirmBetsButton != null)
                confirmBetsButton.onClick.AddListener(ConfirmBets);

            if (simulateRaceButton != null)
                simulateRaceButton.onClick.AddListener(SimulateRaceInstant);
        }

        // Normaliza para asegurar que la suma de probabilidades de los 8 caballos dé 100% (1.0)
        private void NormalizeProbabilities()
        {
            float sum = 0f;
            foreach (var h in horses) sum += h.winProbability;
            if (sum <= 0f) sum = 1f;

            foreach (var h in horses)
            {
                h.winProbability = Mathf.Clamp(h.winProbability / sum, 0.02f, 0.80f);
            }
        }

        // --- 2. CÁLCULO DE CUOTAS / PREMIOS ---
        private void CalculateOdds()
        {
            foreach (var h in horses)
            {
                // Factor de riesgo R = (1 - P) / P
                float risk = (1f - h.winProbability) / h.winProbability;

                h.mult1st = 1f + (risk * factor1st);
                h.mult2nd = 1f + (risk * factor2nd);
                h.mult3rd = 1f + (risk * factor3rd);
            }
        }

        // --- 3. SELECCIÓN Y APUESTAS (+10 / -10) ---
        public void SelectHorse(int index)
        {
            if (betsConfirmed) return; // No cambiar apuestas si ya están confirmadas

            selectedHorseIndex = index;
            UpdateControlsVisibility();
        }

        public void ChangeBet(int index, int amount)
        {
            if (betsConfirmed) return;

            HorseUIElement horse = horses[index];

            if (amount > 0)
            {
                // Verificar si tiene suficiente dinero no apostado
                float totalBets = GetTotalBetsAmount();
                if (playerMoney - totalBets >= amount)
                {
                    horse.currentBet += amount;
                }
                else
                {
                    SetStatus("¡No tienes suficiente dinero disponible!");
                }
            }
            else if (amount < 0)
            {
                horse.currentBet = Mathf.Max(0, horse.currentBet + amount);
            }

            UpdateAllUI();
        }

        private float GetTotalBetsAmount()
        {
            float total = 0;
            foreach (var h in horses) total += h.currentBet;
            return total;
        }

        public void ConfirmBets()
        {
            float total = GetTotalBetsAmount();
            if (total <= 0)
            {
                SetStatus("Debes apostar al menos a un caballo antes de confirmar.");
                return;
            }

            betsConfirmed = true;
            playerMoney -= total; // Descontar el dinero apostado
            SetStatus($"¡Apuestas confirmadas! Total apostado: {total}$.         Carrera Lista.");
            UpdateAllUI();
        }

        // --- 4. SIMULACIÓN INSTANTÁNEA Y RESULTADOS ---
        public void SimulateRaceInstant()
        {
            if (!betsConfirmed)
            {
                SetStatus("Primero debes confirmar tus apuestas antes de correr.");
                return;
            }

            // A. Simular orden de llegada ponderado por probabilidad
            List<HorseUIElement> pool = new List<HorseUIElement>(horses);
            List<HorseUIElement> finishOrder = new List<HorseUIElement>();

            while (pool.Count > 0)
            {
                float totalWeight = 0f;
                foreach (var h in pool) totalWeight += h.winProbability;

                float rand = UnityEngine.Random.Range(0f, totalWeight);
                float current = 0f;

                HorseUIElement selected = pool[0];
                foreach (var h in pool)
                {
                    current += h.winProbability;
                    if (rand <= current)
                    {
                        selected = h;
                        break;
                    }
                }

                finishOrder.Add(selected);
                pool.Remove(selected);
            }

            // B. Calcular Ganancias del Jugador
            float totalEarnings = 0f;

            // 1er Puesto
            HorseUIElement h1 = finishOrder[0];
            if (h1.currentBet > 0) totalEarnings += h1.currentBet * h1.mult1st;

            // 2º Puesto
            HorseUIElement h2 = finishOrder[1];
            if (h2.currentBet > 0) totalEarnings += h2.currentBet * h2.mult2nd;

            // 3er Puesto
            HorseUIElement h3 = finishOrder[2];
            if (h3.currentBet > 0) totalEarnings += h3.currentBet * h3.mult3rd;

            playerMoney += totalEarnings;

            // C. Actualizar Probabilidades de la Próxima Carrera según Posición
            for (int pos = 0; pos < finishOrder.Count; pos++)
            {
                HorseUIElement h = finishOrder[pos];

                // Los mejores puestos aumentan probabilidad, los peores la bajan
                if (pos == 0) h.winProbability += 0.05f;      // 1º
                else if (pos == 1) h.winProbability += 0.03f; // 2º
                else if (pos == 2) h.winProbability += 0.01f; // 3º
                else if (pos >= 5) h.winProbability -= 0.02f; // 6º a 8º

                h.currentBet = 0; // Limpiar apuesta
            }

            // D. Recalcular para la nueva carrera
            NormalizeProbabilities();
            CalculateOdds();

            betsConfirmed = false;
            selectedHorseIndex = -1;

            SetStatus($"Podio: 1º {h1.horseName} | 2º {h2.horseName} | 3º {h3.horseName}. Ganaste: {totalEarnings:F1}$");
            UpdateAllUI();
        }

        // --- 5. ACTUALIZACIÓN VISUAL DE LA UI ---
        private void UpdateAllUI()
        {
            if (moneyText != null)
                moneyText.text = $"Dinero: {playerMoney:F0} $ (Disponible: {(playerMoney - (betsConfirmed ? 0 : GetTotalBetsAmount())):F0} $)";

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
                {
                    h.betText.text = $"Apuesta: {h.currentBet} $";
                }
            }

            UpdateControlsVisibility();

            if (confirmBetsButton != null)
                confirmBetsButton.interactable = !betsConfirmed && GetTotalBetsAmount() > 0;

            if (simulateRaceButton != null)
                simulateRaceButton.interactable = betsConfirmed;
        }

        private void UpdateControlsVisibility()
        {
            for (int i = 0; i < horses.Count; i++)
            {
                if (horses[i].controlsGroup != null)
                {
                    // Solo muestra los botones +10/-10 del caballo seleccionado y si aún no se han confirmado las apuestas
                    bool isSelected = (i == selectedHorseIndex) && !betsConfirmed;
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
}
