using HorseRacing.CanvasUI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BlackMarketHorseUIElement
{
    [Header("Información del Caballo")]
    public string horseName = "Caballo";

    [Header("Referencias UI Locales")]
    public GameObject panelRoot;            // Se activa únicamente si el caballo tiene apuestas
    public TextMeshProUGUI infoText;        // Texto que muestra estadísticas con valores modificados en verde
    public Button assignSteroidButton;      // Botón para equipar 1 esteroide
    public Button assignHorseshoeButton;    // Botón para equipar 1 herradura
}

public class BlackMarketUI : MonoBehaviour
{
    [Header("Casa de Apuestas Activa")]
    public BettingHouseUI currentHouseUI;

    [Header("Lista Manual de 8 Caballos (Inspector)")]
    public List<BlackMarketHorseUIElement> marketHorses = new List<BlackMarketHorseUIElement>(8);

    [Header("UI General de Inventario y Estado")]
    public TextMeshProUGUI statusText;

    [Header("--- BOTONES DE COMPRA GENERALES (TIENDA) ---")]
    public Button buyPolvoMagicoButton;
    public Button buyVipButton;
    public Button buyMagicClockButton;
    public Button buySteroidButton;
    public Button buyHorseshoeButton;

    [Header("Precios Base de los Artículos")]
    public int pricePolvoMagico = 300;
    public int priceVip = 1000;
    public int priceMagicClock = 750;
    public int priceSteroid = 200;
    public int priceHorseshoe = 250;

    private void OnEnable()
    {
        SyncWithBettingHouse();
        SetupUIEvents();
        RefreshMarketUI();
    }

    private void SyncWithBettingHouse()
    {
        if (currentHouseUI == null) return;

        for (int i = 0; i < marketHorses.Count && i < currentHouseUI.horses.Count; i++)
        {
            marketHorses[i].horseName = currentHouseUI.horses[i].horseName;
        }
    }

    private void SetupUIEvents()
    {
        // 1. Configurar botones de asignación por caballo
        for (int i = 0; i < marketHorses.Count; i++)
        {
            int index = i;

            if (marketHorses[i].assignSteroidButton != null)
            {
                marketHorses[i].assignSteroidButton.onClick.RemoveAllListeners();
                marketHorses[i].assignSteroidButton.onClick.AddListener(() => AssignSteroid(index));
            }

            if (marketHorses[i].assignHorseshoeButton != null)
            {
                marketHorses[i].assignHorseshoeButton.onClick.RemoveAllListeners();
                marketHorses[i].assignHorseshoeButton.onClick.AddListener(() => AssignHorseshoe(index));
            }
        }

        // 2. Configurar botones de compra generales de la tienda
        if (buyPolvoMagicoButton != null)
        {
            buyPolvoMagicoButton.onClick.RemoveAllListeners();
            buyPolvoMagicoButton.onClick.AddListener(() => BuyItem("PolvoMagico", pricePolvoMagico));
        }

        if (buyVipButton != null)
        {
            buyVipButton.onClick.RemoveAllListeners();
            buyVipButton.onClick.AddListener(() => BuyItem("EntradaVIP", priceVip));
        }

        if (buyMagicClockButton != null)
        {
            buyMagicClockButton.onClick.RemoveAllListeners();
            buyMagicClockButton.onClick.AddListener(() => BuyItem("RelojMagico", priceMagicClock));
        }

        if (buySteroidButton != null)
        {
            buySteroidButton.onClick.RemoveAllListeners();
            buySteroidButton.onClick.AddListener(() => BuyItem("Esteroides", priceSteroid));
        }

        if (buyHorseshoeButton != null)
        {
            buyHorseshoeButton.onClick.RemoveAllListeners();
            buyHorseshoeButton.onClick.AddListener(() => BuyItem("Herradura", priceHorseshoe));
        }
    }

    private void BuyItem(string itemType, int basePrice)
    {
        if (BlackMarketManager.Instance != null)
        {
            bool success = BlackMarketManager.Instance.BuyItem(itemType, basePrice);
            if (success)
            {
                RefreshMarketUI();
            }
        }
    }

    public void AssignSteroid(int index)
    {
        if (currentHouseUI == null || BlackMarketManager.Instance == null) return;

        string houseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        string hName = marketHorses[index].horseName;

        int ownedSteroids = BlackMarketManager.Instance.GetSteroidsOwned(houseKey);
        int totalAssigned = GetTotalAssigned(BlackMarketManager.Instance.assignedSteroidsToHorses);

        if (ownedSteroids - totalAssigned > 0)
        {
            int current = BlackMarketManager.Instance.assignedSteroidsToHorses.ContainsKey(hName)
                ? BlackMarketManager.Instance.assignedSteroidsToHorses[hName] : 0;

            BlackMarketManager.Instance.assignedSteroidsToHorses[hName] = current + 1;
            RefreshMarketUI();
        }
    }

    public void AssignHorseshoe(int index)
    {
        if (currentHouseUI == null || BlackMarketManager.Instance == null) return;

        string houseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        string hName = marketHorses[index].horseName;

        int ownedHorseshoes = BlackMarketManager.Instance.GetHorseshoesOwned(houseKey);
        int totalAssigned = GetTotalAssigned(BlackMarketManager.Instance.assignedHorseshoesToHorses);

        if (ownedHorseshoes - totalAssigned > 0)
        {
            int current = BlackMarketManager.Instance.assignedHorseshoesToHorses.ContainsKey(hName)
                ? BlackMarketManager.Instance.assignedHorseshoesToHorses[hName] : 0;

            BlackMarketManager.Instance.assignedHorseshoesToHorses[hName] = current + 1;
            RefreshMarketUI();
        }
    }

    private int GetTotalAssigned(Dictionary<string, int> dictionary)
    {
        int total = 0;
        foreach (var kvp in dictionary) total += kvp.Value;
        return total;
    }

    public void RefreshMarketUI()
    {
        if (currentHouseUI == null || BlackMarketManager.Instance == null) return;

        string houseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        int ownedSteroids = BlackMarketManager.Instance.GetSteroidsOwned(houseKey);
        int ownedHorseshoes = BlackMarketManager.Instance.GetHorseshoesOwned(houseKey);

        int assignedSteroidsTotal = GetTotalAssigned(BlackMarketManager.Instance.assignedSteroidsToHorses);
        int assignedHorseshoesTotal = GetTotalAssigned(BlackMarketManager.Instance.assignedHorseshoesToHorses);

        int freeSteroids = ownedSteroids - assignedSteroidsTotal;
        int freeHorseshoes = ownedHorseshoes - assignedHorseshoesTotal;

        if (statusText != null)
        {
            statusText.text = $"Esteroides Libres: {freeSteroids} | Herraduras Libres: {freeHorseshoes}";
        }

        // 1. Actualizar interactividad de botones generales de la tienda
        if (buyPolvoMagicoButton != null) buyPolvoMagicoButton.interactable = !BlackMarketManager.Instance.GetPolvoMagicoOwned(houseKey);
        if (buyVipButton != null) buyVipButton.interactable = !BlackMarketManager.Instance.GetVipOwned(houseKey);
        if (buyMagicClockButton != null) buyMagicClockButton.interactable = !BlackMarketManager.Instance.GetMagicClockOwned(houseKey);
        if (buySteroidButton != null) buySteroidButton.interactable = (ownedSteroids < 3);
        if (buyHorseshoeButton != null) buyHorseshoeButton.interactable = (ownedHorseshoes < 3);

        // 2. Filtrar y mostrar ÚNICAMENTE los caballos que tienen dinero apostado (> 0)
        for (int i = 0; i < marketHorses.Count; i++)
        {
            BlackMarketHorseUIElement slot = marketHorses[i];

            if (i < currentHouseUI.horses.Count)
            {
                HorseUIElement horseData = currentHouseUI.horses[i];
                slot.horseName = horseData.horseName;

                // DETECCIÓN CLAVE: Evalúa si hay apuesta realizada en este caballo
                bool isBetPlaced = horseData.currentBet > 0;
                if (slot.panelRoot != null) slot.panelRoot.SetActive(isBetPlaced);

                if (!isBetPlaced) continue;

                float baseWin = horseData.winProbability;
                float baseFall = 5f;

                int assignedSteroids = BlackMarketManager.Instance.assignedSteroidsToHorses.ContainsKey(slot.horseName)
                    ? BlackMarketManager.Instance.assignedSteroidsToHorses[slot.horseName] : 0;

                int assignedHorseshoes = BlackMarketManager.Instance.assignedHorseshoesToHorses.ContainsKey(slot.horseName)
                    ? BlackMarketManager.Instance.assignedHorseshoesToHorses[slot.horseName] : 0;

                float modifiedWin = baseWin + (assignedSteroids * BlackMarketManager.Instance.steroidWinBoost);
                float modifiedFall = Mathf.Max(0f, baseFall - (assignedHorseshoes * BlackMarketManager.Instance.horseshoeFallReduction));

                // Escribir modificadores en el objeto de datos del caballo para que RaceManager los aplique
                horseData.winProbabilityModifier = assignedSteroids * BlackMarketManager.Instance.steroidWinBoost;
                horseData.fallProbabilityModifier = -(assignedHorseshoes * BlackMarketManager.Instance.horseshoeFallReduction);

                // Colorear en verde los datos si están modificados
                string winStr = assignedSteroids > 0 ? $"<color=#00FF00>{modifiedWin * 100f:F1}% (+)</color>" : $"{baseWin * 100f:F1}%";
                string fallStr = assignedHorseshoes > 0 ? $"<color=#00FF00>{modifiedFall:F1}% (-)</color>" : $"{baseFall:F1}%";

                if (slot.infoText != null)
                {
                    slot.infoText.text = $"<b>{slot.horseName}</b>\nWin: {winStr} | Caída: {fallStr}\n(Est: {assignedSteroids} | Herr: {assignedHorseshoes})";
                }

                if (slot.assignSteroidButton != null)
                {
                    slot.assignSteroidButton.interactable = (freeSteroids > 0);
                }

                if (slot.assignHorseshoeButton != null)
                {
                    slot.assignHorseshoeButton.interactable = (freeHorseshoes > 0);
                }
            }
            else
            {
                if (slot.panelRoot != null) slot.panelRoot.SetActive(false);
            }
        }
    }
}
