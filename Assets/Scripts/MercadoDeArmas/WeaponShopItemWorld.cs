using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    [Header("Precios")]
    public int basePrice = 100;

    [Header("Información del Componente (Tooltip)")]
    public string itemName = "Componente";
    [TextArea] public string itemDescription = "Descripción del componente...";

    [Header("Referencias 3D en la Escena")]
    public GameObject itemMeshObject;   // La malla del objeto normal
    public GameObject soldOutMeshObject;// La malla del cartel de "Sold Out"

    [Header("UI World Space")]
    public Canvas worldSpaceCanvas;
    public Button buyButton;
    public TextMeshProUGUI priceText;

    [Header("Textos de Tooltip (Nombre y Descripción)")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public GameObject infoPanelContainer; // Opcional: El panel contenedor que agrupa nombre y descripción

    [Header("UI de Munición (Opcional para este ítem)")]
public TextMeshProUGUI ammoCountText;

    private bool isSoldOut = false;

    private void Start()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(TryBuyItem);

            // Configurar EventTrigger para Hover en el Botón de Compra
            AddHoverTriggerToGameObject(buyButton.gameObject, true);
        }

        // Configurar Hover en la Malla 3D del objeto
        if (itemMeshObject != null)
        {
            // Asegurarnos de que la malla tenga un collider para detectar el raycast del ratón
            AddHoverTriggerToGameObject(itemMeshObject, false);
        }

        // Ocultar info por defecto al arrancar
        ShowItemInfo(false);
        RefreshItemState();
    }

    /// <summary>
    /// Añade eventos de PointerEnter y PointerExit mediante código a cualquier GameObject.
    /// </summary>
    private void AddHoverTriggerToGameObject(GameObject targetObj, bool isButton)
    {
        EventTrigger trigger = targetObj.GetComponent<EventTrigger>();
        if (trigger == null) trigger = targetObj.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((data) => { if (!isSoldOut) ShowItemInfo(true); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((data) => { ShowItemInfo(false); });
        trigger.triggers.Add(entryExit);
    }

    /// <summary>
    /// Muestra u oculta los textos de nombre y descripción.
    /// </summary>
    private void ShowItemInfo(bool show)
    {
        if (isSoldOut) show = false; // Si está comprado, jamás se muestra

        if (infoPanelContainer != null)
        {
            infoPanelContainer.SetActive(show);
        }

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

    /// <summary>
    /// Actualiza visualmente si el objeto está comprado, agotado o disponible.
    /// </summary>
    public void RefreshItemState()
    {
        if (WeaponShopManager.Instance == null) return;

        switch (itemType)
        {
            case WeaponShopItemType.ZoomScope:
                isSoldOut = WeaponShopManager.Instance.hasZoomMod;
                break;
            case WeaponShopItemType.PilaLibrosBipod:
                isSoldOut = WeaponShopManager.Instance.hasBipodMod;
                break;
            case WeaponShopItemType.ManosNuevasStock:
                isSoldOut = WeaponShopManager.Instance.hasStockMod;
                break;
            case WeaponShopItemType.MonoculoIdentifier:
                isSoldOut = WeaponShopManager.Instance.hasIdentifierMod;
                break;
            case WeaponShopItemType.BalaIndividual:
            case WeaponShopItemType.PilaDeBalas:
                isSoldOut = (WeaponShopManager.Instance.currentAmmo >= WeaponShopManager.Instance.maxAmmo);
                break;
        }

        // Si se acaba de poner en Sold Out, ocultamos el tooltip por seguridad
        if (isSoldOut)
        {
            ShowItemInfo(false);
        }

        // 1. Alternar Mallas 3D (Objeto normal vs Cartel de Sold Out)
        if (itemMeshObject != null) itemMeshObject.SetActive(!isSoldOut);
        if (soldOutMeshObject != null) soldOutMeshObject.SetActive(isSoldOut);

        // 2. Controlar SOLO el botón de compra y el texto del precio (sin apagar todo el Canvas)
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!isSoldOut);
        }

        if (priceText != null)
        {
            priceText.gameObject.SetActive(!isSoldOut);
            if (!isSoldOut)
            {
                int cost = basePrice;
                priceText.text = $"{cost}$";
            }
        }
        UpdateAmmoDisplay();
    }

    public void TryBuyItem()
    {
        if (GlobalBettingManager.Instance == null || WeaponShopManager.Instance == null) return;

        int cost = basePrice;

        if (GlobalBettingManager.Instance.playerMoney < cost)
        {
            Debug.Log("[WeaponShop] No tienes suficiente dinero.");
            return;
        }

        // Ejecutar compra según el tipo
        switch (itemType)
        {
            case WeaponShopItemType.ZoomScope:
                GlobalBettingManager.Instance.playerMoney -= cost;
                WeaponShopManager.Instance.hasZoomMod = true;
                break;

            case WeaponShopItemType.PilaLibrosBipod:
                GlobalBettingManager.Instance.playerMoney -= cost;
                WeaponShopManager.Instance.hasBipodMod = true;
                break;

            case WeaponShopItemType.ManosNuevasStock:
                GlobalBettingManager.Instance.playerMoney -= cost;
                WeaponShopManager.Instance.hasStockMod = true;
                break;

            case WeaponShopItemType.MonoculoIdentifier:
                GlobalBettingManager.Instance.playerMoney -= cost;
                WeaponShopManager.Instance.hasIdentifierMod = true;
                break;

            case WeaponShopItemType.BalaIndividual:
                if (WeaponShopManager.Instance.currentAmmo < WeaponShopManager.Instance.maxAmmo)
                {
                    GlobalBettingManager.Instance.playerMoney -= cost;
                    WeaponShopManager.Instance.currentAmmo++;
                }
                break;

            case WeaponShopItemType.PilaDeBalas:
                if (WeaponShopManager.Instance.currentAmmo < WeaponShopManager.Instance.maxAmmo)
                {
                    GlobalBettingManager.Instance.playerMoney -= cost;
                    WeaponShopManager.Instance.currentAmmo = WeaponShopManager.Instance.maxAmmo;
                }
                break;
        }

        // Ocultar info inmediatamente tras la compra
        ShowItemInfo(false);

        // Refrescar estados de todos los objetos en la tienda y actualizar el texto de munición
        WeaponShopItemWorld[] allItems = FindObjectsByType<WeaponShopItemWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in allItems)
        {
            item.RefreshItemState();
            item.UpdateAmmoDisplay(); // Actualiza el contador en los ítems que tengan texto asignado
        }
    }

    /// <summary>
    /// Actualiza el texto del contador de munición de forma segura (si tiene referencia asignada)
    /// </summary>
    public void UpdateAmmoDisplay()
    {
        if (ammoCountText == null || WeaponShopManager.Instance == null) return;

        int currentAmmo = WeaponShopManager.Instance.currentAmmo;
        int maxAmmo = WeaponShopManager.Instance.maxAmmo;

        ammoCountText.text = $"Munición: {currentAmmo} / {maxAmmo}";

        // Cambiar color si está al máximo
        if (currentAmmo >= maxAmmo)
        {
            ammoCountText.color = Color.yellow;
        }
        else
        {
            ammoCountText.color = Color.white;
        }
    }
}