using UnityEngine;
using TMPro;

public class PlayerMoneyUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI moneyText;

    [Header("Configuración de Formato")]
    public string prefix = "Saldo: ";
    public string suffix = "$";

    private void Awake()
    {
        // Si no se ha asignado en el inspector, intenta obtenerlo del mismo GameObject
        if (moneyText == null)
        {
            moneyText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        UpdateMoneyDisplay();
    }

    private void Start()
    {
        UpdateMoneyDisplay();
    }

    private void Update()
    {
        // Mantiene el texto actualizado por si el dinero cambia en tiempo real
        UpdateMoneyDisplay();
    }

    /// <summary>
    /// Consulta el saldo actual en el GlobalBettingManager y actualiza el texto de UI.
    /// </summary>
    public void UpdateMoneyDisplay()
    {
        if (GlobalBettingManager.Instance != null && moneyText != null)
        {
            moneyText.text = $"{prefix}{GlobalBettingManager.Instance.playerMoney:F0}{suffix}";
        }
        else if (moneyText != null)
        {
            moneyText.text = $"{prefix}---{suffix}";
        }
    }
}
