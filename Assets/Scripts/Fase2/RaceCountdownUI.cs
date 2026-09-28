using TMPro;
using UnityEngine;
using System.Collections;

public class RaceCountdownUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownPanel;

    [Header("Configuración")]
    [SerializeField] private int startSeconds = 5;
    [SerializeField] private bool autoStartOnLoad = true;

    private bool isCountingDown = false;

    private void Start()
    {
        if (autoStartOnLoad)
        {
            Invoke(nameof(StartCountdown), 0.5f);
        }
    }

    public void StartCountdown()
    {
        if (isCountingDown) return;
        isCountingDown = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownPanel != null) countdownPanel.SetActive(true);

        int current = startSeconds;

        while (current > 0)
        {
            if (countdownText != null) countdownText.text = current.ToString();
            yield return new WaitForSeconds(1f);
            current--;
        }

        if (countdownText != null) countdownText.text = "<color=green>¡¡YA!!</color>";

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.StartRace();
        }

        yield return new WaitForSeconds(1f);

        if (countdownPanel != null) countdownPanel.SetActive(false);
    }
}
