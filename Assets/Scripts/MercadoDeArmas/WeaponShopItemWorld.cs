using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public enum WeaponShopItemType
{
    ZoomScope,
    PilaLibrosBipod,
    ManosNuevasStock,
    MonoculoIdentifier,
    BalaIndividual,
    PilaDeBalas
}

public class WeaponShopItemWorld : MonoBehaviour
{
    [Header("Tipo de Artículo")]
    public WeaponShopItemType itemType;

    [Header("Clasificación de Tienda")]
    [Tooltip("False = Tienda Barata. True = Tienda Cara (Bloqueada en rojo hasta comprar el sniper avanzado).")]
    public bool isAdvancedItem = false;

    [Header("Precios")]
    public int basePrice = 100;

    [Header("Información del Componente (Tooltip)")]
    public string itemName = "Componente";
    [TextArea] public string itemDescription = "Descripción del componente...";

    [Header("Referencias 3D en la Escena")]
    public GameObject itemMeshObject;
    public GameObject soldOutMeshObject;

    [Header("UI World Space")]
    public Canvas worldSpaceCanvas;
    public Button buyButton;
    public TextMeshProUGUI priceText;
    [Tooltip("El texto asignado en el botón de compra.")]
    public TextMeshProUGUI buttonText;

    [Header("Textos de Tooltip")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public GameObject infoPanelContainer;

    [Header("UI de Munición (Opcional)")]
    public TextMeshProUGUI ammoCountText;

    private bool isSoldOut = false;
    private bool isLockedByAdvancedRequirement = false;

    // Posiciones locales originales para los textos
    private Vector3 originalButtonTextLocalPos;
    private Vector3 originalPriceTextLocalPos;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        if (buttonText != null) originalButtonTextLocalPos = buttonText.rectTransform.localPosition;
        if (priceText != null) originalPriceTextLocalPos = priceText.rectTransform.localPosition;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(TryBuyItem);
            AddHoverTriggerToGameObject(buyButton.gameObject, true);
        }

        if (itemMeshObject != null)
        {
            AddHoverTriggerToGameObject(itemMeshObject, false);
        }

        ShowItemInfo(false);
        RefreshItemState();
    }

    private void Update()
    {
        UpdateMoneyVisuals();
    }

    private void AddHoverTriggerToGameObject(GameObject targetObj, bool isButton)
    {
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => { if (!isSoldOut && !isLockedByAdvancedRequirement) ShowItemInfo(true); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => { ShowItemInfo(false); });
        trigger.triggers.Add(entryExit);
    }

    private void ShowItemInfo(bool show)
    {
        if (isSoldOut || isLockedByAdvancedRequirement) show = false;

        if (infoPanelContainer != null) infoPanelContainer.SetActive(show);

        if (nameText != null)
        {
            nameText.gameObject.SetActive(show);
            if (show) nameText.text = itemName;
        }

        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(show);
            if (show) descriptionText.text = itemDescription;
        }
    }

    public void RefreshItemState()
    {
        if (WeaponShopManager.Instance == null) return;

        bool isAdvUnlocked = WeaponShopManager.Instance.hasAdvancedSniperUnlocked;

        isLockedByAdvancedRequirement = false;
        isSoldOut = false;

        if (isAdvancedItem && !isAdvUnlocked)
        {
            isLockedByAdvancedRequirement = true;
        }
        else
        {
            switch (itemType)
            {
                case WeaponShopItemType.ZoomScope:
                    isSoldOut = !isAdvancedItem ? WeaponShopManager.Instance.baseZoomMod : WeaponShopManager.Instance.advZoomMod;
                    break;
                case WeaponShopItemType.PilaLibrosBipod:
                    isSoldOut = !isAdvancedItem ? WeaponShopManager.Instance.baseBipodMod : WeaponShopManager.Instance.advBipodMod;
                    break;
                case WeaponShopItemType.ManosNuevasStock:
                    isSoldOut = !isAdvancedItem ? WeaponShopManager.Instance.baseStockMod : WeaponShopManager.Instance.advStockMod;
                    break;
                case WeaponShopItemType.MonoculoIdentifier:
                    isSoldOut = !isAdvancedItem ? WeaponShopManager.Instance.baseIdentifierMod : WeaponShopManager.Instance.advIdentifierMod;
                    break;
                case WeaponShopItemType.BalaIndividual:
                case WeaponShopItemType.PilaDeBalas:
                    int curAmmo = !isAdvancedItem ? WeaponShopManager.Instance.baseCurrentAmmo : WeaponShopManager.Instance.advCurrentAmmo;
                    int maxAm = !isAdvancedItem ? WeaponShopManager.Instance.baseMaxAmmo : WeaponShopManager.Instance.advMaxAmmo;
                    isSoldOut = (curAmmo >= maxAm);
                    break;
            }
        }

        if (itemMeshObject != null) itemMeshObject.SetActive(!isSoldOut);
        if (soldOutMeshObject != null) soldOutMeshObject.SetActive(isSoldOut);

        if (isSoldOut)
        {
            if (buyButton != null) buyButton.gameObject.SetActive(false);
            ShowItemInfo(false);
            UpdateAmmoDisplay();
            return;
        }

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(true);
            Image btnImage = buyButton.GetComponent<Image>();

            if (isLockedByAdvancedRequirement)
            {
                buyButton.interactable = false;
                if (btnImage != null) btnImage.color = new Color(0.85f, 0.2f, 0.2f);
                if (buttonText != null)
                {
                    buttonText.text = "No disponible";
                    buttonText.color = Color.white;
                }
            }
            else
            {
                buyButton.interactable = true;
                if (btnImage != null) btnImage.color = new Color(0.2f, 0.75f, 0.2f);
                if (buttonText != null)
                {
                    buttonText.enabled = false;
                }
            }
        }

        if (priceText != null)
        {
            bool showPrice = !isLockedByAdvancedRequirement && basePrice > 0;
            priceText.gameObject.SetActive(showPrice);
            if (showPrice)
            {
                priceText.text = $"{basePrice}$";
            }
        }

        UpdateAmmoDisplay();
    }

    private void UpdateMoneyVisuals()
    {
        if (isSoldOut || isLockedByAdvancedRequirement) return;

        bool playerHasMoney = true;
        if (GlobalBettingManager.Instance != null)
        {
            playerHasMoney = GlobalBettingManager.Instance.playerMoney >= basePrice;
        }

        // El color cambia a rojo si no hay dinero, y a blanco si sí hay
        if (buttonText != null) buttonText.color = playerHasMoney ? Color.white : Color.red;
        if (priceText != null) priceText.color = playerHasMoney ? Color.white : Color.red;
    }

    public void TryBuyItem()
    {
        if (GlobalBettingManager.Instance == null || WeaponShopManager.Instance == null) return;

        int cost = basePrice;

        if (GlobalBettingManager.Instance.playerMoney < cost)
        {
            Debug.Log("[WeaponShop] Fondos insuficientes.");

            // 🫨 Disparamos el temblor rápido, violento y de medio segundo solo para este ítem
            TriggerShakeEffect();
            return;
        }

        GlobalBettingManager.Instance.playerMoney -= cost;

        switch (itemType)
        {
            case WeaponShopItemType.ZoomScope:
                if (!isAdvancedItem) WeaponShopManager.Instance.baseZoomMod = true;
                else WeaponShopManager.Instance.advZoomMod = true;
                break;

            case WeaponShopItemType.PilaLibrosBipod:
                if (!isAdvancedItem) WeaponShopManager.Instance.baseBipodMod = true;
                else WeaponShopManager.Instance.advBipodMod = true;
                break;

            case WeaponShopItemType.ManosNuevasStock:
                if (!isAdvancedItem) WeaponShopManager.Instance.baseStockMod = true;
                else WeaponShopManager.Instance.advStockMod = true;
                break;

            case WeaponShopItemType.MonoculoIdentifier:
                if (!isAdvancedItem) WeaponShopManager.Instance.baseIdentifierMod = true;
                else WeaponShopManager.Instance.advIdentifierMod = true;
                break;

            case WeaponShopItemType.BalaIndividual:
                WeaponShopManager.Instance.AddAmmo(1, isAdvancedItem);
                break;

            case WeaponShopItemType.PilaDeBalas:
                int curP = !isAdvancedItem ? WeaponShopManager.Instance.baseCurrentAmmo : WeaponShopManager.Instance.advCurrentAmmo;
                int maxP = !isAdvancedItem ? WeaponShopManager.Instance.baseMaxAmmo : WeaponShopManager.Instance.advMaxAmmo;
                WeaponShopManager.Instance.AddAmmo(maxP - curP, isAdvancedItem);
                break;
        }

        ShowItemInfo(false);
        RefreshAllShopItems();
    }

    private void TriggerShakeEffect()
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float duration = 0.5f; // Duración exacta de medio segundo
        float elapsed = 0f;
        float magnitude = 6.0f; // Mayor intensidad para que sea más violento

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Movimiento errático rápido (frecuencias altas de seno y coseno)
            float offsetX = Mathf.Sin(Time.time * 120f) * magnitude;
            float offsetY = Mathf.Cos(Time.time * 100f) * magnitude;

            if (buttonText != null)
            {
                buttonText.rectTransform.localPosition = originalButtonTextLocalPos + new Vector3(offsetX, offsetY, 0f);
            }
            if (priceText != null)
            {
                priceText.rectTransform.localPosition = originalPriceTextLocalPos + new Vector3(offsetX, offsetY, 0f);
            }

            yield return null;
        }

        // Restaurar posiciones originales al terminar los 0.5 segundos
        if (buttonText != null) buttonText.rectTransform.localPosition = originalButtonTextLocalPos;
        if (priceText != null) priceText.rectTransform.localPosition = originalPriceTextLocalPos;
    }

    private void RefreshAllShopItems()
    {
        WeaponShopItemWorld[] allItems = FindObjectsByType<WeaponShopItemWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            item.RefreshItemState();
        }
    }

    public void UpdateAmmoDisplay()
    {
        if (ammoCountText == null || WeaponShopManager.Instance == null) return;

        int currentAmmo = !isAdvancedItem ? WeaponShopManager.Instance.baseCurrentAmmo : WeaponShopManager.Instance.advCurrentAmmo;
        int maxAmmo = !isAdvancedItem ? WeaponShopManager.Instance.baseMaxAmmo : WeaponShopManager.Instance.advMaxAmmo;

        ammoCountText.text = $"Munición: {currentAmmo} / {maxAmmo}";
        ammoCountText.color = (currentAmmo >= maxAmmo) ? Color.yellow : Color.white;
    }
}