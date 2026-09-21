using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CarInteractable : MonoBehaviour, IInteractable
{
    private Outline outline;
    private bool isLoading = false;

    private void Start()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    public void OnHovered()
    {
        if (isLoading) return;

        if (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet)
        {
            if (outline != null) outline.enabled = true;
        }
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (isLoading) return;

        if (GlobalBettingManager.Instance == null || !GlobalBettingManager.Instance.hasActiveBet)
        {
            Debug.LogWarning("[Coche] Debes confirmar una apuesta en alguna casa antes de ir a la pista.");
            return;
        }

        if (GlobalBettingManager.Instance.activeBettingHouse == null)
        {
            Debug.LogError("[Coche] No se ha encontrado la casa de apuestas activa.");
            return;
        }

        string targetScene = GlobalBettingManager.Instance.activeBettingHouse.trackSceneName;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[Coche] La casa activa no tiene una escena de pista asignada.");
            return;
        }

        StartCoroutine(LoadTrackSceneAsync(targetScene));
    }

    private IEnumerator LoadTrackSceneAsync(string sceneName)
    {
        isLoading = true;
        if (outline != null) outline.enabled = false;

        // 1. Fundido a negro (ScreenFader es hijo persistente)
        if (ScreenFade.Instance != null)
        {
            yield return StartCoroutine(ScreenFade.Instance.FadeOutCoroutine());
        }

        // 2. Carga asíncrona en segundo plano
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 3. Activación de la nueva escena de la pista
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. Aclarar pantalla tras completar el montaje de la pista
        if (ScreenFade.Instance != null)
        {
            yield return StartCoroutine(ScreenFade.Instance.FadeInCoroutine());
        }

        isLoading = false;
    }
}
