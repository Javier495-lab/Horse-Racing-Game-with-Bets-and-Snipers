using UnityEngine;
using System.Collections;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    [Header("Configuración de Fundido")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Estado inicial: totalmente transparente y sin interceptar clics
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator FadeOutCoroutine(float duration = -1f)
    {
        float dur = duration > 0 ? duration : defaultFadeDuration;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeInCoroutine(float duration = -1f)
    {
        float dur = duration > 0 ? duration : defaultFadeDuration;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / dur));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
