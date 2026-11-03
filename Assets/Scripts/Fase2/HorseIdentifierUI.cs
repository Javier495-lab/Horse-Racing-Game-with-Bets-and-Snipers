using UnityEngine;
using TMPro;

public class HorseIdentifierUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject canvasContainer; // El objeto Canvas hijo ubicado sobre la cabeza del caballo
    public TextMeshProUGUI horseNameText;
    public TextMeshProUGUI betAmountText;
    public TextMeshProUGUI actionMessageText; // Texto que dirá "DISPARAR" o "NO DISPARAR"

    private HorseRunner horseRunner;

    private void Awake()
    {
        horseRunner = GetComponentInParent<HorseRunner>();
        if (canvasContainer != null) canvasContainer.SetActive(false); // Apagado por defecto
    }

    public void ShowIdentifier(bool show, bool hasBetMoney)
    {
        if (canvasContainer == null) return;

        if (!show)
        {
            canvasContainer.SetActive(false);
            return;
        }

        canvasContainer.SetActive(true);

        // Rellenar datos dinámicos del caballo
        if (horseRunner != null)
        {
            if (horseNameText != null) horseNameText.text = horseRunner.horseName;
            if (betAmountText != null) betAmountText.text = $"Apuesta: {horseRunner.currentBet}$";

            // Comprobar si se ha metido dinero a este caballo mediante su variable currentBet
            bool hasMoneyInThisHorse = horseRunner.currentBet > 0f;

            if (actionMessageText != null)
            {
                if (hasMoneyInThisHorse)
                {
                    actionMessageText.text = "DISPARAR";
                    actionMessageText.color = Color.green; // Color verde para indicar objetivo prioritario
                }
                else
                {
                    actionMessageText.text = "NO DISPARAR";
                    actionMessageText.color = Color.red;   // Color rojo de advertencia
                }
            }
        }
    }
}
