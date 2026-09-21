using UnityEngine;
using UnityEngine.UI;

public class BetScreen : MonoBehaviour, IInteractable
{
    private Camera screenCamera;
    private Outline outline;
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private BettingHouseUI bettingHouseUI;
    private GameObject bettingCanvas;

    [SerializeField] private FPSController player;
    public Button returnButton;

    public bool isLocked { get; private set; } = false;

    private void Awake()
    {
        screenCamera = GetComponentInChildren<Camera>(true);
        outline = GetComponent<Outline>();
        canvas = GetComponentInChildren<Canvas>(true);
        graphicRaycaster = GetComponentInChildren<GraphicRaycaster>(true);
        bettingHouseUI = GetComponentInChildren<BettingHouseUI>(true);

        if (canvas != null)
        {
            bettingCanvas = canvas.gameObject;
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

        if (GlobalBettingManager.Instance != null && !GlobalBettingManager.Instance.allBetScreens.Contains(this))
        {
            GlobalBettingManager.Instance.allBetScreens.Add(this);
        }

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
        if (bettingHouseUI != null) bettingHouseUI.DisableAllButtons();

        if (screenCamera != null && screenCamera.enabled)
        {
            OnReturn();
        }
    }

    public void OnHovered()
    {
        if (isLocked || (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet))
            return;

        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (isLocked || (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet))
            return;

        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.DisableAllRaycasters();
        }

        if (player != null) player.ToggleControls(false);

        if (screenCamera != null) screenCamera.enabled = true;
        if (bettingCanvas != null) bettingCanvas.SetActive(true);
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
    }
}
