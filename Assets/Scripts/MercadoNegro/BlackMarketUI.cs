using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class HousePriceMultiplier
{
    [Tooltip("Nombre exacto de la casa de apuestas (houseName)")]
    public string houseName = "Casa Base";
    [Tooltip("Multiplicador sobre el precio base (ej: 1.5 = 50% más caro)")]
    public float priceMultiplier = 1.0f;
}

[Serializable]
public class BlackMarketHorseUIElement
{
    [Header("Información del Caballo")]
    public string horseName = "Caballo";

    [Header("Referencias UI Locales")]
    public GameObject panelRoot;
    public Image horseSpriteImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI winStatText;
    public TextMeshProUGUI fallStatText;

    [Header("Botones de Acción (Aparecen al hacer Hover en la imagen)")]
    public GameObject actionButtonsGroup;

    [Header("Botón Esteroide (Sprites Dinámicos)")]
    public Button steroidButton;
    public Sprite steroidEquippedSprite;
    public Sprite steroidUnequippedSprite;

    [Header("Botón Herradura (Sprites Dinámicos)")]
    public Button horseshoeButton;
    public Sprite horseshoeEquippedSprite;
    public Sprite horseshoeUnequippedSprite;
}

public class BlackMarketUI : MonoBehaviour
{
    [Header("Casa de Apuestas Activa")]
    public BettingHouseUI currentHouseUI;

    [Header("Control del Jugador (FPSController)")]
    public FPSController playerController;

    [Header("Botón de Salir del Mercado")]
    public Button exitButton;

    [Header("Configuración de Variación de Precios por Casa (Inspector)")]
    public float defaultPriceMultiplier = 1.0f;
    public List<HousePriceMultiplier> housePriceMultipliers = new List<HousePriceMultiplier>();

    [Header("Lista Manual de 8 Caballos (Inspector)")]
    public List<BlackMarketHorseUIElement> marketHorses = new List<BlackMarketHorseUIElement>(8);

    [Header("Color para Caballos No Apostados (Inspector)")]
    [Tooltip("Color tintado para la imagen de los caballos a los que NO se ha apostado")]
    public Color unbetHorseColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);

    [Header("UI General de Inventario y Estado")]
    public TextMeshProUGUI statusText;

    [Header("--- TIENDA GENERAL (BOTONES) ---")]
    public Button buyPolvoMagicoButton;
    public Button buyVipButton;
    public Button buyMagicClockButton;
    public Button buySteroidButton;
    public Button buyHorseshoeButton;

    [Header("--- TIENDA GENERAL (IMÁGENES DE ITEMS PARA HOVER) ---")]
    public Image imagePolvoMagico;
    public Image imageVip;
    public Image imageMagicClock;
    public Image imageSteroid;
    public Image imageHorseshoe;

    [Header("Textos de Cantidad Comprada (Tienda)")]
    public TextMeshProUGUI countPolvoMagicoText;
    public TextMeshProUGUI countVipText;
    public TextMeshProUGUI countMagicClockText;
    public TextMeshProUGUI countSteroidText;
    public TextMeshProUGUI countHorseshoeText;

    [Header("Control de Progresión / Desbloqueo (Inspector / Código)")]
    [Tooltip("Activa o desactiva la presencia del objeto en la tienda según la progresión del juego")]
    public bool isPolvoMagicoAvailable = true;
    public bool isVipAvailable = true;
    public bool isMagicClockAvailable = true;
    public bool isSteroidAvailable = true;
    public bool isHorseshoeAvailable = true;

    [Header("Colores de los Botones de Tienda")]
    public Color availableButtonColor = Color.green;
    public Color unavailableButtonColor = Color.red;

    [Header("Panel de Descripción por Hover (Tienda)")]
    public GameObject shopDescriptionPanel;
    public TextMeshProUGUI descriptionTitleText;
    public Image descriptionItemImage;
    public TextMeshProUGUI descriptionItemText;

    [Header("Precios Base de los Artículos")]
    public int basePricePolvoMagico = 300;
    public int basePriceVip = 1000;
    public int basePriceMagicClock = 750;
    public int basePriceSteroid = 200;
    public int basePriceHorseshoe = 250;

    [Header("Nombres de los Artículos (Inspector)")]
    public string namePolvoMagico = "Polvo Mágico";
    public string nameVip = "Entrada VIP";
    public string nameMagicClock = "Reloj Mágico";
    public string nameSteroid = "Esteroides";
    public string nameHorseshoe = "Herradura";

    [Header("Descripciones de los Artículos")]
    [TextArea] public string descPolvoMagico = "Aumenta el tiempo máximo que puedes contener la respiración con el rifle de francotirador.";
    [TextArea] public string descVip = "Te otorga una posición elevada y privilegiada en las gradas VIP durante las carreras.";
    [TextArea] public string descMagicClock = "Permite ralentizar temporalmente el tiempo en la carrera manteniendo pulsado Ctrl.";
    [TextArea] public string descSteroid = "Suministra esteroides a un caballo apostado para aumentar su probabilidad de victoria.";
    [TextArea] public string descHorseshoe = "Equipa herraduras a un caballo apostado para reducir drásticamente el riesgo de caída.";

    [Header("Sprites de los Artículos (Para el panel de descripción)")]
    public Sprite spritePolvoMagico;
    public Sprite spriteVip;
    public Sprite spriteMagicClock;
    public Sprite spriteSteroid;
    public Sprite spriteHorseshoe;

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.ToggleControls(false);
        }

        if (shopDescriptionPanel != null) shopDescriptionPanel.SetActive(false);

        SyncWithBettingHouse();
        SetupUIEvents();
        RefreshMarketUI();
    }

    private void OnDisable()
    {
        CloseMarket();
    }

    public void CloseMarket()
    {
        if (playerController != null)
        {
            playerController.ToggleControls(true);
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    public void SyncWithBettingHouse()
    {
        if (currentHouseUI == null) return;

        for (int i = 0; i < marketHorses.Count && i < currentHouseUI.horses.Count; i++)
        {
            marketHorses[i].horseName = currentHouseUI.horses[i].horseName;

            if (marketHorses[i].nameText != null)
            {
                marketHorses[i].nameText.text = currentHouseUI.horses[i].horseName;
            }

            if (marketHorses[i].horseSpriteImage != null && currentHouseUI.horses[i].horsePortrait != null)
            {
                marketHorses[i].horseSpriteImage.sprite = currentHouseUI.horses[i].horsePortrait;
            }
        }
    }

    private float GetCurrentHouseMultiplier()
    {
        if (BlackMarketManager.Instance == null) return defaultPriceMultiplier;

        string currentHouseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        var match = housePriceMultipliers.Find(h => h.houseName.Equals(currentHouseKey, StringComparison.OrdinalIgnoreCase));

        return match != null ? match.priceMultiplier : defaultPriceMultiplier;
    }

    private void SetupUIEvents()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(CloseMarket);
        }

        for (int i = 0; i < marketHorses.Count; i++)
        {
            int index = i;
            var horseSlot = marketHorses[i];

            if (horseSlot.actionButtonsGroup != null)
            {
                horseSlot.actionButtonsGroup.SetActive(false);
            }

            if (horseSlot.horseSpriteImage != null)
            {
                horseSlot.horseSpriteImage.raycastTarget = true;

                EventTrigger trigger = horseSlot.horseSpriteImage.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = horseSlot.horseSpriteImage.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((data) => { ShowHorseActions(index, true); });
                trigger.triggers.Add(entryEnter);

                EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((data) => { ShowHorseActions(index, false); });
                trigger.triggers.Add(entryExit);
            }

            if (horseSlot.steroidButton != null)
            {
                horseSlot.steroidButton.onClick.RemoveAllListeners();
                horseSlot.steroidButton.onClick.AddListener(() => ToggleSteroid(index));
            }

            if (horseSlot.horseshoeButton != null)
            {
                horseSlot.horseshoeButton.onClick.RemoveAllListeners();
                horseSlot.horseshoeButton.onClick.AddListener(() => ToggleHorseshoe(index));
            }
        }

        SetupShopItemEvents(buyPolvoMagicoButton, imagePolvoMagico, namePolvoMagico, spritePolvoMagico, descPolvoMagico, () => BuyItem("PolvoMagico", GetCalculatedPrice(basePricePolvoMagico)));
        SetupShopItemEvents(buyVipButton, imageVip, nameVip, spriteVip, descVip, () => BuyItem("EntradaVIP", GetCalculatedPrice(basePriceVip)));
        SetupShopItemEvents(buyMagicClockButton, imageMagicClock, nameMagicClock, spriteMagicClock, descMagicClock, () => BuyItem("RelojMagico", GetCalculatedPrice(basePriceMagicClock)));
        SetupShopItemEvents(buySteroidButton, imageSteroid, nameSteroid, spriteSteroid, descSteroid, () => BuyItem("Esteroides", GetCalculatedPrice(basePriceSteroid)));
        SetupShopItemEvents(buyHorseshoeButton, imageHorseshoe, nameHorseshoe, spriteHorseshoe, descHorseshoe, () => BuyItem("Herradura", GetCalculatedPrice(basePriceHorseshoe)));
    }

    private void SetupShopItemEvents(Button button, Image itemImage, string itemName, Sprite itemSprite, string description, UnityEngine.Events.UnityAction buyAction)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(buyAction);

            AddHoverTrigger(button.gameObject, itemName, itemSprite, description);
        }

        if (itemImage != null)
        {
            itemImage.raycastTarget = true;
            AddHoverTrigger(itemImage.gameObject, itemName, itemSprite, description);
        }
    }

    private void AddHoverTrigger(GameObject targetObj, string itemName, Sprite itemSprite, string description)
    {
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => { ShowShopDescription(itemName, itemSprite, description, true); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => { ShowShopDescription("", null, "", false); });
        trigger.triggers.Add(entryExit);
    }

    private void ShowShopDescription(string title, Sprite sprite, string description, bool show)
    {
        if (shopDescriptionPanel != null)
        {
            shopDescriptionPanel.SetActive(show);
        }

        if (descriptionTitleText != null)
        {
            descriptionTitleText.text = show ? title : "";
        }

        if (descriptionItemImage != null)
        {
            descriptionItemImage.gameObject.SetActive(show && sprite != null);
            descriptionItemImage.sprite = sprite;
        }

        if (descriptionItemText != null)
        {
            descriptionItemText.text = show ? description : "";
        }
    }

    private int GetCalculatedPrice(int basePrice)
    {
        return Mathf.RoundToInt(basePrice * GetCurrentHouseMultiplier());
    }

    private void ShowHorseActions(int index, bool show)
    {
        if (index >= 0 && index < marketHorses.Count && currentHouseUI != null && index < currentHouseUI.horses.Count)
        {
            if (currentHouseUI.horses[index].currentBet > 0)
            {
                if (marketHorses[index].actionButtonsGroup != null)
                {
                    marketHorses[index].actionButtonsGroup.SetActive(show);
                }
            }
        }
    }

    private void BuyItem(string itemType, int finalPrice)
    {
        if (BlackMarketManager.Instance != null)
        {
            bool success = BlackMarketManager.Instance.BuyItem(itemType, finalPrice);
            if (success)
            {
                RefreshMarketUI();
            }
        }
    }

    public void ToggleSteroid(int index)
    {
        if (currentHouseUI == null || BlackMarketManager.Instance == null) return;

        string houseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        string hName = marketHorses[index].horseName;

        int currentAssigned = BlackMarketManager.Instance.assignedSteroidsToHorses.ContainsKey(hName)
            ? BlackMarketManager.Instance.assignedSteroidsToHorses[hName] : 0;

        if (currentAssigned > 0)
        {
            BlackMarketManager.Instance.assignedSteroidsToHorses[hName] = currentAssigned - 1;
        }
        else
        {
            int ownedSteroids = BlackMarketManager.Instance.GetSteroidsOwned(houseKey);
            int totalAssigned = GetTotalAssigned(BlackMarketManager.Instance.assignedSteroidsToHorses);
            int freeSteroids = ownedSteroids - totalAssigned;

            if (freeSteroids > 0)
            {
                BlackMarketManager.Instance.assignedSteroidsToHorses[hName] = 1;
            }
        }

        RefreshMarketUI();
    }

    public void ToggleHorseshoe(int index)
    {
        if (currentHouseUI == null || BlackMarketManager.Instance == null) return;

        string houseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
        string hName = marketHorses[index].horseName;

        int currentAssigned = BlackMarketManager.Instance.assignedHorseshoesToHorses.ContainsKey(hName)
            ? BlackMarketManager.Instance.assignedHorseshoesToHorses[hName] : 0;

        if (currentAssigned > 0)
        {
            BlackMarketManager.Instance.assignedHorseshoesToHorses[hName] = currentAssigned - 1;
        }
        else
        {
            int ownedHorseshoes = BlackMarketManager.Instance.GetHorseshoesOwned(houseKey);
            int totalAssigned = GetTotalAssigned(BlackMarketManager.Instance.assignedHorseshoesToHorses);
            int freeHorseshoes = ownedHorseshoes - totalAssigned;

            if (freeHorseshoes > 0)
            {
                BlackMarketManager.Instance.assignedHorseshoesToHorses[hName] = 1;
            }
        }

        RefreshMarketUI();
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

        bool hasPolvo = BlackMarketManager.Instance.GetPolvoMagicoOwned(houseKey);
        bool hasVip = BlackMarketManager.Instance.GetVipOwned(houseKey);
        bool hasClock = BlackMarketManager.Instance.GetMagicClockOwned(houseKey);
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

        if (countPolvoMagicoText != null) countPolvoMagicoText.text = hasPolvo ? "1/1" : "0/1";
        if (countVipText != null) countVipText.text = hasVip ? "1/1" : "0/1";
        if (countMagicClockText != null) countMagicClockText.text = hasClock ? "1/1" : "0/1";
        if (countSteroidText != null) countSteroidText.text = $"{ownedSteroids}/3";
        if (countHorseshoeText != null) countHorseshoeText.text = $"{ownedHorseshoes}/3";

        bool canBuyPolvo = isPolvoMagicoAvailable && !hasPolvo;
        bool canBuyVip = isVipAvailable && !hasVip;
        bool canBuyClock = isMagicClockAvailable && !hasClock;
        bool canBuySteroid = isSteroidAvailable && (ownedSteroids < 3);
        bool canBuyHorseshoe = isHorseshoeAvailable && (ownedHorseshoes < 3);

        UpdateShopButton(buyPolvoMagicoButton, canBuyPolvo, hasPolvo, GetCalculatedPrice(basePricePolvoMagico));
        UpdateShopButton(buyVipButton, canBuyVip, hasVip, GetCalculatedPrice(basePriceVip));
        UpdateShopButton(buyMagicClockButton, canBuyClock, hasClock, GetCalculatedPrice(basePriceMagicClock));
        UpdateShopButton(buySteroidButton, canBuySteroid, ownedSteroids >= 3, GetCalculatedPrice(basePriceSteroid));
        UpdateShopButton(buyHorseshoeButton, canBuyHorseshoe, ownedHorseshoes >= 3, GetCalculatedPrice(basePriceHorseshoe));

        for (int i = 0; i < marketHorses.Count; i++)
        {
            BlackMarketHorseUIElement slot = marketHorses[i];

            if (i < currentHouseUI.horses.Count)
            {
                HorseUIElement horseData = currentHouseUI.horses[i];
                slot.horseName = horseData.horseName;

                if (slot.panelRoot != null) slot.panelRoot.SetActive(true);

                if (slot.nameText != null) slot.nameText.text = horseData.horseName;
                if (slot.horseSpriteImage != null && horseData.horsePortrait != null)
                {
                    slot.horseSpriteImage.sprite = horseData.horsePortrait;
                }

                bool isBetPlaced = horseData.currentBet > 0;

                if (slot.horseSpriteImage != null)
                {
                    slot.horseSpriteImage.color = isBetPlaced ? Color.white : unbetHorseColor;
                }

                // 💥 CALCULAR CAÍDA BASE DESDE HORSE DATA O DESDE LA CASA DE APUESTAS
                float baseFall = horseData.baseFallProbability > 0 ? horseData.baseFallProbability : currentHouseUI.houseBaseFallProbability;

                if (!isBetPlaced)
                {
                    if (slot.actionButtonsGroup != null) slot.actionButtonsGroup.SetActive(false);
                    if (slot.winStatText != null) slot.winStatText.text = $"Win: {horseData.winProbability * 100f:F1}%";
                    if (slot.fallStatText != null) slot.fallStatText.text = $"Caída: {baseFall:F1}%";
                    continue;
                }

                float baseWin = horseData.winProbability;

                int assignedSteroids = BlackMarketManager.Instance.assignedSteroidsToHorses.ContainsKey(slot.horseName)
                    ? BlackMarketManager.Instance.assignedSteroidsToHorses[slot.horseName] : 0;

                int assignedHorseshoes = BlackMarketManager.Instance.assignedHorseshoesToHorses.ContainsKey(slot.horseName)
                    ? BlackMarketManager.Instance.assignedHorseshoesToHorses[slot.horseName] : 0;

                float modifiedWin = baseWin + (assignedSteroids * BlackMarketManager.Instance.steroidWinBoost);
                float modifiedFall = Mathf.Max(0f, baseFall - (assignedHorseshoes * BlackMarketManager.Instance.horseshoeFallReduction));

                // Guardar los modificadores en la estructura local del caballo
                horseData.winProbabilityModifier = assignedSteroids * BlackMarketManager.Instance.steroidWinBoost;
                horseData.fallProbabilityModifier = -(assignedHorseshoes * BlackMarketManager.Instance.horseshoeFallReduction);

                if (slot.winStatText != null)
                {
                    slot.winStatText.text = assignedSteroids > 0
                        ? $"Win: <color=#00FF00>{modifiedWin * 100f:F1}% (+)</color>"
                        : $"Win: {baseWin * 100f:F1}%";
                }

                if (slot.fallStatText != null)
                {
                    slot.fallStatText.text = assignedHorseshoes > 0
                        ? $"Caída: <color=#00FF00>{modifiedFall:F1}% (-)</color>"
                        : $"Caída: {baseFall:F1}%";
                }

                if (slot.steroidButton != null)
                {
                    bool hasSteroid = assignedSteroids > 0;
                    if (slot.steroidButton.image != null)
                    {
                        slot.steroidButton.image.sprite = hasSteroid ? slot.steroidEquippedSprite : slot.steroidUnequippedSprite;
                    }
                    slot.steroidButton.interactable = hasSteroid || (freeSteroids > 0);
                }

                if (slot.horseshoeButton != null)
                {
                    bool hasHorseshoe = assignedHorseshoes > 0;
                    if (slot.horseshoeButton.image != null)
                    {
                        slot.horseshoeButton.image.sprite = hasHorseshoe ? slot.horseshoeEquippedSprite : slot.horseshoeUnequippedSprite;
                    }
                    slot.horseshoeButton.interactable = hasHorseshoe || (freeHorseshoes > 0);
                }
            }
            else
            {
                if (slot.panelRoot != null) slot.panelRoot.SetActive(false);
            }
        }
    }

    private void UpdateShopButton(Button button, bool canBePurchased, bool isFullyStockedOut, int price)
    {
        if (button == null) return;

        bool playerHasMoney = GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.playerMoney >= price;
        bool isAvailable = canBePurchased && playerHasMoney && !isFullyStockedOut;

        button.interactable = isAvailable;

        Image btnImage = button.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.raycastTarget = !isFullyStockedOut;
            btnImage.color = isAvailable ? availableButtonColor : unavailableButtonColor;
        }

        ColorBlock colors = button.colors;
        colors.pressedColor = isAvailable ? Color.green : Color.red;
        button.colors = colors;

        TextMeshProUGUI btnText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            if (isFullyStockedOut)
            {
                btnText.color = Color.white;
                btnText.text = "Sold Out";
            }
            else if (!canBePurchased)
            {
                btnText.color = Color.white;
                btnText.text = "No Disponible";
            }
            else
            {
                btnText.color = playerHasMoney ? Color.white : Color.red;
                btnText.text = $"{price}$";
            }
        }
    }
}