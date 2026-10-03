using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Método público para cambiar de escena con fundido completo desde cualquier script.
    /// </summary>
    public void LoadSceneWithFade(string sceneName, float duration = -1f)
    {
        StartCoroutine(FadeAndLoadSceneRoutine(sceneName, duration));
    }

    private IEnumerator FadeAndLoadSceneRoutine(string sceneName, float duration)
    {
        // 1. Restaurar tiempo y cursor inmediatamente al iniciar el cambio
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Fundido a negro
        yield return StartCoroutine(FadeOutCoroutine(duration));

        // 3. Carga asíncrona de la escena
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activación de la nueva escena
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. Aclarar pantalla en la nueva escena
        yield return StartCoroutine(FadeInCoroutine(duration));
    }

    public IEnumerator FadeOutCoroutine(float duration = -1f)
    {
        float dur = duration > 0 ? duration : defaultFadeDuration;
        canvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < dur)
        {
            // 👈 Se usa unscaledDeltaTime por si el juego está pausado al pulsar volver
            elapsed += Time.unscaledDeltaTime;
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
            // 👈 Se usa unscaledDeltaTime por consistencia
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / dur));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
