using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


[System.Serializable]
public class ActiveBetDisplaySlot
{
    [Header("Contenedor e Imagen")]
    public GameObject slotContainer;             // El objeto padre/panel de esta ranura
    public Image horsePortraitImage;             // Imagen del retrato del caballo

    [Header("Textos Independientes")]
    public TextMeshProUGUI horseNameText;        // 1. Texto para el nombre del caballo
    public TextMeshProUGUI statsText;            // 2. Texto para Win % y Caída %
    public TextMeshProUGUI betAmountText;        // 3. Texto para la cantidad de dinero apostado
}

public class ActiveBetsHUD : MonoBehaviour
{
    [Header("Ranuras de UI (Configurar 8 en el Inspector)")]
    public List<ActiveBetDisplaySlot> displaySlots = new List<ActiveBetDisplaySlot>(8);

    private void Start()
    {
        PopulateActiveBetsHUD();
    }

    public void PopulateActiveBetsHUD()
    {
        if (GlobalBettingManager.Instance == null)
        {
            Debug.LogWarning("[ActiveBetsHUD] No se encontró GlobalBettingManager en la escena.");
            return;
        }

        List<GlobalBettingManager.ActiveBetData> activeBets = GlobalBettingManager.Instance.currentActiveBets;

        for (int i = 0; i < displaySlots.Count; i++)
        {
            var slot = displaySlots[i];

            // Si hay una apuesta activa para esta ranura
            if (i < activeBets.Count)
            {
                var bet = activeBets[i];

                if (slot.slotContainer != null) slot.slotContainer.SetActive(true);

                // 1. Asignar Imagen del Retrato directamente desde los datos persistentes
                if (slot.horsePortraitImage != null)
                {
                    slot.horsePortraitImage.gameObject.SetActive(bet.horsePortrait != null);
                    slot.horsePortraitImage.sprite = bet.horsePortrait;
                }

                // 2. Asignar Nombre del Caballo
                if (slot.horseNameText != null)
                {
                    slot.horseNameText.text = bet.horseName;
                }

                // 3. Asignar Estadísticas (Solo Probabilidad de Victoria y Probabilidad de Caída)
                if (slot.statsText != null)
                {
                    float winPct = bet.winProbability * 100f;
                    slot.statsText.text = $"Win: {winPct:F1}% | Caída: {bet.fallProbability:F1}%";
                }

                // 4. Asignar Dinero Apostado
                if (slot.betAmountText != null)
                {
                    slot.betAmountText.text = $"Apuesta: {bet.amount:F0} $";
                }
            }
            else
            {
                // Si la ranura no tiene apuesta asociada, se desactiva por completo el contenedor
                if (slot.slotContainer != null)
                {
                    slot.slotContainer.SetActive(false);
                }
            }
        }
    }
}