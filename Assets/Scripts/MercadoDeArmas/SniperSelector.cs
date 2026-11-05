using UnityEngine;

public class SniperSelector : MonoBehaviour, IInteractable
{
    [Header("Configuración del Selector")]
    [Tooltip("False = Selector del Sniper Base. True = Selector / Desbloqueador del Sniper Mejorado.")]
    public bool isAdvancedSniperObject = false;

    [Header("Precio de Compra (Solo para el Sniper Mejorado)")]
    public int purchasePrice = 500;

    [Header("Disponibilidad (Dinero Suficiente)")]
    public bool isAvailable = false;

    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    private void Start()
    {
        RefreshAllSelectors();
    }

    private void Update()
    {
        EvaluateAvailability();
    }

    private void EvaluateAvailability()
    {
        if (WeaponShopManager.Instance == null || GlobalBettingManager.Instance == null) return;

        if (isAdvancedSniperObject && !WeaponShopManager.Instance.hasAdvancedSniperUnlocked)
        {
            isAvailable = GlobalBettingManager.Instance.playerMoney >= purchasePrice;
        }
        else
        {
            isAvailable = true;
        }
    }

    /// <summary>
    /// Actualiza la visibilidad de este objeto concreto según qué arma esté equipada.
    /// </summary>
    public void RefreshVisualState()
    {
        if (WeaponShopManager.Instance == null) return;

        bool isAdvSelected = WeaponShopManager.Instance.isAdvancedSniperSelected;
        bool isAdvUnlocked = WeaponShopManager.Instance.hasAdvancedSniperUnlocked;

        if (!isAdvancedSniperObject)
        {
            // 🔹 SELECTOR DEL SNIPER BASE:
            // Aparece SOLAMENTE si el avanzado ya fue comprado Y actualmente tenemos el avanzado equipado (para poder volver al base).
            // Si tenemos el base equipado, este objeto desaparece.
            bool shouldBeActive = isAdvUnlocked && isAdvSelected;
            gameObject.SetActive(shouldBeActive);
        }
        else
        {
            // 🔹 SELECTOR DEL SNIPER MEJORADO:
            if (!isAdvUnlocked)
            {
                // Si no se ha comprado nunca, siempre es visible para poder adquirirlo.
                gameObject.SetActive(true);
            }
            else
            {
                // Si ya está comprado, aparece solo cuando el base está equipado (para poder cambiar al avanzado).
                // Si el avanzado ya está equipado, este objeto desaparece.
                gameObject.SetActive(!isAdvSelected);
            }
        }
    }

    public void OnHovered()
    {
        if (!isAvailable) return;
        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (!isAvailable)
        {
            Debug.Log("[SniperSelector] No disponible / Fondos insuficientes.");
            return;
        }

        if (WeaponShopManager.Instance == null) return;

        if (isAdvancedSniperObject && !WeaponShopManager.Instance.hasAdvancedSniperUnlocked)
        {
            // 💰 Compra única del sniper avanzado
            if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.playerMoney >= purchasePrice)
            {
                GlobalBettingManager.Instance.playerMoney -= purchasePrice;
                WeaponShopManager.Instance.hasAdvancedSniperUnlocked = true;
                WeaponShopManager.Instance.isAdvancedSniperSelected = true;

                Debug.Log("[SniperSelector] ¡Sniper Mejorado comprado y equipado con éxito!");
            }
        }
        else
        {
            // 🔄 Cambiar entre snipers ya desbloqueados
            WeaponShopManager.Instance.isAdvancedSniperSelected = isAdvancedSniperObject;
            Debug.Log($"[SniperSelector] Cambiado a: {(isAdvancedSniperObject ? "Sniper Mejorado" : "Sniper Base")}");
        }

        if (outline != null) outline.enabled = false;

        // Refrescar la tienda y la visibilidad de TODOS los selectores (incluso los inactivos)
        RefreshAllShops();
        RefreshAllSelectors();
    }

    /// <summary>
    /// Busca y actualiza el estado visual de todos los selectores de la escena (activos e inactivos).
    /// </summary>
    public static void RefreshAllSelectors()
    {
        SniperSelector[] selectors = FindObjectsByType<SniperSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var selector in selectors)
        {
            selector.RefreshVisualState();
        }
    }

    private void RefreshAllShops()
    {
        WeaponShopItemWorld[] items = FindObjectsByType<WeaponShopItemWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
        {
            item.RefreshItemState();
        }
    }
}
