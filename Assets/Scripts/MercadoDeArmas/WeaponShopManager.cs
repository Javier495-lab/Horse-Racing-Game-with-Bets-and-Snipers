using UnityEngine;

public class WeaponShopManager : MonoBehaviour
{
    public static WeaponShopManager Instance { get; private set; }

    [Header("Componentes Permanentes (Sniper Base)")]
    public bool hasZoomMod = false;
    public bool hasBipodMod = false;   // Pila de libros
    public bool hasStockMod = false;   // Manos nuevas
    public bool hasIdentifierMod = false; // Monóculo

    [Header("Munición Actual")]
    public int currentAmmo = 5;
    public int maxAmmo = 5;

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
    /// Aplica los mods guardados al SniperSystem cuando inicia la carrera.
    /// </summary>
    public void ApplyUpgradesToSniper(SniperSystem sniper)
    {
        if (sniper == null) return;

        sniper.hasZoomMod = hasZoomMod;
        sniper.hasBipodMod = hasBipodMod;
        sniper.hasStockMod = hasStockMod;
        sniper.hasIdentifierMod = hasIdentifierMod;

        // Cargar las balas guardadas en la tienda
        sniper.maxAmmo = maxAmmo;
        sniper.currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
    }

    /// <summary>
    /// Guarda las balas restantes al terminar la carrera.
    /// </summary>
    public void SaveAmmoAfterRace(int remainingAmmo)
    {
        currentAmmo = Mathf.Clamp(remainingAmmo, 0, maxAmmo);
    }

    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(currentAmmo + amount, 0, maxAmmo);
    }
}
