using UnityEngine;

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

        isLoading = true;
        if (outline != null) outline.enabled = false;

        // 🚀 Delegamos la carga y el doble fundido al objeto persistente
        ScreenFade.Instance.LoadSceneWithFade(targetScene);
    }
}
