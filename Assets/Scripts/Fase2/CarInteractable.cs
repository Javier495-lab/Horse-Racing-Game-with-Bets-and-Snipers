using UnityEngine;

public class CarInteractable : MonoBehaviour, IInteractable
{
    private Outline outline;
    private bool isLoading = false;
    [SerializeField] private float fadeDuration = 2f;

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

        // Leemos el nombre de la escena directamente desde la variable persistente del Manager Global
        string targetScene = GlobalBettingManager.Instance.targetTrackSceneName;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[Coche] La casa activa no tiene una escena de pista asignada en targetTrackSceneName.");
            return;
        }

        isLoading = true;
        if (outline != null) outline.enabled = false;

        if (ScreenFade.Instance != null)
        {
            ScreenFade.Instance.LoadSceneWithFade(targetScene, fadeDuration);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}
