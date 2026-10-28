using System.Collections.Generic;
using UnityEngine;

public class BlackMarketManager : MonoBehaviour
{
    public static BlackMarketManager Instance { get; private set; }

    [Header("Configuración de Precios Base")]
    public float basePriceMultiplier = 1.0f;

    [Header("Valores de Mejora Configurables (Testing)")]
    public float breathTimeIncrease = 4f;       // Polvo Mágico: +4s al MaxBreathTime
    public float steroidWinBoost = 0.05f;      // Esteroides: +5% probabilidad de victoria por unidad
    public float horseshoeFallReduction = 5f;  // Herradura: -5% probabilidad de caída por unidad

    // Diccionarios para aislar compras por Casa de Apuestas (Key: Nombre de la Casa)
    private Dictionary<string, int> purchasedSteroidsPerHouse = new Dictionary<string, int>();
    private Dictionary<string, int> purchasedHorseshoesPerHouse = new Dictionary<string, int>();
    private Dictionary<string, bool> purchasedMagicClockPerHouse = new Dictionary<string, bool>();
    private Dictionary<string, bool> purchasedPolvoMagicoPerHouse = new Dictionary<string, bool>();
    private Dictionary<string, bool> purchasedVipTicketPerHouse = new Dictionary<string, bool>();

    // Añadir estos diccionarios junto a los demás en BlackMarketManager.cs
    private Dictionary<string, int> totalPurchasedSteroidsCountPerHouse = new Dictionary<string, int>();
    private Dictionary<string, int> totalPurchasedHorseshoesCountPerHouse = new Dictionary<string, int>();

    // Métodos públicos para consultar cuántos se han comprado en total en la casa actual
    public int GetTotalPurchasedSteroids(string houseKey) => totalPurchasedSteroidsCountPerHouse.ContainsKey(houseKey) ? totalPurchasedSteroidsCountPerHouse[houseKey] : 0;
    public int GetTotalPurchasedHorseshoes(string houseKey) => totalPurchasedHorseshoesCountPerHouse.ContainsKey(houseKey) ? totalPurchasedHorseshoesCountPerHouse[houseKey] : 0;

    private Dictionary<string, int> sessionPurchasedSteroids = new Dictionary<string, int>();
    private Dictionary<string, int> sessionPurchasedHorseshoes = new Dictionary<string, int>();

    public int GetSessionPurchasedSteroids(string houseKey) => sessionPurchasedSteroids.ContainsKey(houseKey) ? sessionPurchasedSteroids[houseKey] : 0;
    public int GetSessionPurchasedHorseshoes(string houseKey) => sessionPurchasedHorseshoes.ContainsKey(houseKey) ? sessionPurchasedHorseshoes[houseKey] : 0;

    // Asignación activa por caballo apostado para la carrera en curso (Key: Nombre del caballo -> Cantidad)
    public Dictionary<string, int> assignedSteroidsToHorses = new Dictionary<string, int>();
    public Dictionary<string, int> assignedHorseshoesToHorses = new Dictionary<string, int>();

    // Estado activo consumible en la escena de carreras
    public bool hasActivePolvoMagico = false;
    public bool hasActiveVipTicket = false;
    public bool hasActiveMagicClock = false;

    // 🔑 Caché para recordar la última casa activa al cambiar de escena
    private string cachedHouseKey = "DefaultHouse";

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

    public string GetCurrentHouseKey()
    {
        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.currentHouse != null)
        {
            cachedHouseKey = GlobalBettingManager.Instance.currentHouse.houseName;
            return cachedHouseKey;
        }
        // Si estamos en la escena de carrera y currentHouse es null, devolvemos la última casa guardada
        return !string.IsNullOrEmpty(cachedHouseKey) ? cachedHouseKey : "DefaultHouse";
    }

    public bool BuyItem(string itemType, int baseCost)
    {
        string houseKey = GetCurrentHouseKey();
        int finalCost = Mathf.RoundToInt(baseCost * basePriceMultiplier);

        if (GlobalBettingManager.Instance == null || GlobalBettingManager.Instance.playerMoney < finalCost)
        {
            Debug.Log("[BlackMarket] Fondos insuficientes en GlobalBettingManager.");
            return false;
        }

        switch (itemType)
        {
            case "PolvoMagico":
                if (GetPolvoMagicoOwned(houseKey)) return false;
                GlobalBettingManager.Instance.playerMoney -= finalCost;
                purchasedPolvoMagicoPerHouse[houseKey] = true;
                hasActivePolvoMagico = true;
                break;

            case "Esteroides":
                {
                    int currentSteroids = GetSteroidsOwned(houseKey);
                    int assignedSteroids = GetTotalAssigned(assignedSteroidsToHorses);
                    int freeSteroids = currentSteroids - assignedSteroids;
                    int sessionBought = GetSessionPurchasedSteroids(houseKey);

                    // REQUISITO: Inventario libre lleno (>= 3) o límite de 3 compras en esta ronda alcanzado
                    if (freeSteroids >= 3 || sessionBought >= 3) return false;

                    GlobalBettingManager.Instance.playerMoney -= finalCost;
                    purchasedSteroidsPerHouse[houseKey] = currentSteroids + 1;
                    sessionPurchasedSteroids[houseKey] = sessionBought + 1;
                    break;
                }

            case "Herradura":
                {
                    int currentHorseshoes = GetHorseshoesOwned(houseKey);
                    int assignedHorseshoes = GetTotalAssigned(assignedHorseshoesToHorses);
                    int freeHorseshoes = currentHorseshoes - assignedHorseshoes;
                    int sessionBought = GetSessionPurchasedHorseshoes(houseKey);

                    // REQUISITO: Inventario libre lleno (>= 3) o límite de 3 compras en esta ronda alcanzado
                    if (freeHorseshoes >= 3 || sessionBought >= 3) return false;

                    GlobalBettingManager.Instance.playerMoney -= finalCost;
                    purchasedHorseshoesPerHouse[houseKey] = currentHorseshoes + 1;
                    sessionPurchasedHorseshoes[houseKey] = sessionBought + 1;
                    break;
                }

            case "EntradaVIP":
                if (GetVipOwned(houseKey)) return false;
                GlobalBettingManager.Instance.playerMoney -= finalCost;
                purchasedVipTicketPerHouse[houseKey] = true;
                hasActiveVipTicket = true;
                break;

            case "RelojMagico":
                if (GetMagicClockOwned(houseKey)) return false; // Máximo 1 unidad
                GlobalBettingManager.Instance.playerMoney -= finalCost;
                purchasedMagicClockPerHouse[houseKey] = true;
                hasActiveMagicClock = true;
                break;
        }

        Debug.Log($"[BlackMarket] Comprado {itemType} en {houseKey}. Saldo restante: {GlobalBettingManager.Instance.playerMoney}");
        return true;
    }

    public bool GetPolvoMagicoOwned(string houseKey) => purchasedPolvoMagicoPerHouse.ContainsKey(houseKey) && purchasedPolvoMagicoPerHouse[houseKey];
    public bool GetVipOwned(string houseKey) => purchasedVipTicketPerHouse.ContainsKey(houseKey) && purchasedVipTicketPerHouse[houseKey];
    public bool GetMagicClockOwned(string houseKey) => purchasedMagicClockPerHouse.ContainsKey(houseKey) && purchasedMagicClockPerHouse[houseKey];
    public int GetSteroidsOwned(string houseKey) => purchasedSteroidsPerHouse.ContainsKey(houseKey) ? purchasedSteroidsPerHouse[houseKey] : 0;
    public int GetHorseshoesOwned(string houseKey) => purchasedHorseshoesPerHouse.ContainsKey(houseKey) ? purchasedHorseshoesPerHouse[houseKey] : 0;

    public void NotifyRaceFinished(bool clockWasUsed)
    {
        string houseKey = GetCurrentHouseKey();
        Debug.Log($"[BlackMarket] Procesando fin de carrera para la casa: {houseKey}");

        // 1. RELOJ MÁGICO: Se consume y se resetea SOLO si fue utilizado durante la carrera. Si no se usó, se mantiene comprado.
        if (clockWasUsed)
        {
            purchasedMagicClockPerHouse[houseKey] = false;
            hasActiveMagicClock = false;
            Debug.Log("[BlackMarket] Reloj Mágico consumido tras ser usado en la carrera.");
        }
        else
        {
            Debug.Log("[BlackMarket] Reloj Mágico NO fue usado. Permanece comprado para la próxima vez.");
        }

        // 2. POLVO MÁGICO & ENTRADA VIP: Se resetean SIEMPRE tras cada carrera
        purchasedPolvoMagicoPerHouse[houseKey] = false;
        purchasedVipTicketPerHouse[houseKey] = false;
        hasActivePolvoMagico = false;
        hasActiveVipTicket = false;

        int totalAssignedSteroids = GetTotalAssigned(assignedSteroidsToHorses);
        int currentSteroidsOwned = GetSteroidsOwned(houseKey);
        int remainingSteroids = Mathf.Max(0, currentSteroidsOwned - totalAssignedSteroids);
        purchasedSteroidsPerHouse[houseKey] = remainingSteroids;

        int totalAssignedHorseshoes = GetTotalAssigned(assignedHorseshoesToHorses);
        int currentHorseshoesOwned = GetHorseshoesOwned(houseKey);
        int remainingHorseshoes = Mathf.Max(0, currentHorseshoesOwned - totalAssignedHorseshoes);
        purchasedHorseshoesPerHouse[houseKey] = remainingHorseshoes;

        // 🎯 IMPORTANTE: Si ya se han gastado/consumido las unidades en la carrera, 
        // reseteamos el contador de compra total de la ronda para permitir comprar nuevos lotes si se desea.
        if (remainingSteroids == 0) totalPurchasedSteroidsCountPerHouse[houseKey] = 0;
        if (remainingHorseshoes == 0) totalPurchasedHorseshoesCountPerHouse[houseKey] = 0;

        assignedSteroidsToHorses.Clear();
        assignedHorseshoesToHorses.Clear();

        sessionPurchasedSteroids[houseKey] = 0;
        sessionPurchasedHorseshoes[houseKey] = 0;
    }

    private int GetTotalAssigned(Dictionary<string, int> dict)
    {
        int sum = 0;
        foreach (var kvp in dict) sum += kvp.Value;
        return sum;
    }
}

