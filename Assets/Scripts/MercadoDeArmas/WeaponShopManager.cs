using UnityEngine;

public class WeaponShopManager : MonoBehaviour
{
    public static WeaponShopManager Instance { get; private set; }

    [Header("Estado General de Armas")]
    [Tooltip("Indica si el jugador ya ha comprado/desbloqueado el sniper mejorado.")]
    public bool hasAdvancedSniperUnlocked = false;

    [Tooltip("False = Sniper Base seleccionado | True = Sniper Mejorado seleccionado para la carrera.")]
    public bool isAdvancedSniperSelected = false;

    [Header("=== SNIPER BASE (Mods y Munición) ===")]
    public bool baseZoomMod = false;
    public bool baseBipodMod = false;
    public bool baseStockMod = false;
    public bool baseIdentifierMod = false;
    public int baseCurrentAmmo = 5;
    public int baseMaxAmmo = 5;

    [Header("=== SNIPER MEJORADO (Mods y Munición) ===")]
    public bool advZoomMod = false;
    public bool advBipodMod = false;
    public bool advStockMod = false;
    public bool advIdentifierMod = false;
    public int advCurrentAmmo = 5;
    public int advMaxAmmo = 6;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sincroniza los datos con el SniperSystem al iniciar la carrera según el sniper seleccionado.
    /// </summary>
    public void ApplyUpgradesToSniper(SniperSystem sniper)
    {
        if (sniper == null) return;

        sniper.hasAdvancedSniper = isAdvancedSniperSelected;

        if (!isAdvancedSniperSelected)
        {
            sniper.hasZoomMod = baseZoomMod;
            sniper.hasBipodMod = baseBipodMod;
            sniper.hasStockMod = baseStockMod;
            sniper.hasIdentifierMod = baseIdentifierMod;
            sniper.maxAmmo = baseMaxAmmo;
            sniper.currentAmmo = Mathf.Clamp(baseCurrentAmmo, 0, baseMaxAmmo);
        }
        else
        {
            sniper.hasZoomMod = advZoomMod;
            sniper.hasBipodMod = advBipodMod;
            sniper.hasStockMod = advStockMod;
            sniper.hasIdentifierMod = advIdentifierMod;
            sniper.maxAmmo = advMaxAmmo;
            sniper.currentAmmo = Mathf.Clamp(advCurrentAmmo, 0, advMaxAmmo);
        }
    }

    public void SaveAmmoAfterRace(int remainingAmmo)
    {
        if (!isAdvancedSniperSelected)
            baseCurrentAmmo = Mathf.Clamp(remainingAmmo, 0, baseMaxAmmo);
        else
            advCurrentAmmo = Mathf.Clamp(remainingAmmo, 0, advMaxAmmo);
    }

    public void AddAmmo(int amount, bool toAdvanced)
    {
        if (!toAdvanced)
            baseCurrentAmmo = Mathf.Clamp(baseCurrentAmmo + amount, 0, baseMaxAmmo);
        else
            advCurrentAmmo = Mathf.Clamp(advCurrentAmmo + amount, 0, advMaxAmmo);
    }
}
