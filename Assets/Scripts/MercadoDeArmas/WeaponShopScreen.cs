using UnityEngine;
using UnityEngine.UI;

public class WeaponShopScreen : MonoBehaviour, IInteractable
{
    private Camera screenCamera;
    private Outline outline;
    private Canvas canvas;
    public GraphicRaycaster graphicRaycaster;
    private GameObject shopCanvas;

    [SerializeField] private FPSController player;
    public Button returnButton;


    public bool isLocked { get; private set; } = false;

    private void Awake()
    {
        screenCamera = GetComponentInChildren<Camera>(true);
        outline = GetComponent<Outline>();
        canvas = GetComponentInChildren<Canvas>(true);
        graphicRaycaster = GetComponentInChildren<GraphicRaycaster>(true);

        if (canvas != null)
        {
            shopCanvas = canvas.gameObject;
            if (screenCamera != null)
            {
                canvas.worldCamera = screenCamera;
            }
        }
    }

    private void Start()
    {
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;
        if (screenCamera != null) screenCamera.enabled = false;
        if (outline != null) outline.enabled = false;

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturn);
        }
    }

    public void SetInteractionLocked(bool locked)
    {
        isLocked = locked;
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;

        if (screenCamera != null && screenCamera.enabled)
        {
            OnReturn();
        }
    }

    public void OnHovered()
    {
        // Quitamos la restricción de hasActiveBet para que se pueda abrir siempre que no esté bloqueado
        if (isLocked) return;

        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        // Quitamos la restricción de hasActiveBet aquí también
        if (isLocked) return;

        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.DisableAllRaycasters();
        }

        if (player != null) player.ToggleControls(false);

        if (screenCamera != null) screenCamera.enabled = true;
        if (shopCanvas != null) shopCanvas.SetActive(true);
        if (graphicRaycaster != null) graphicRaycaster.enabled = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (outline != null) outline.enabled = false;
    }

    public void OnReturn()
    {
        if (screenCamera != null) screenCamera.enabled = false;
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;

        if (player != null) player.ToggleControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
