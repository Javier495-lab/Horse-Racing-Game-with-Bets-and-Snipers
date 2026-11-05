using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BetScreen : MonoBehaviour, IInteractable
{
    private Outline outline;
    private Canvas canvas;
    public GraphicRaycaster graphicRaycaster;
    private BettingHouseUI bettingHouseUI;
    private GameObject bettingCanvas;

    [Header("Configuración de Cámara Suave (Damping)")]
    [Tooltip("El Transform (posición y rotación) donde se colocará la cámara al mirar la casa de apuestas.")]
    public Transform cameraTargetPose;
    [Tooltip("El campo de visión (FOV) que tendrá la cámara al enfocar las apuestas.")]
    public float targetFOV = 30f;
    [Tooltip("Duración de la transición en segundos (ej. 0.5 para un damping suave).")]
    public float transitionDuration = 0.5f;

    [SerializeField] private FPSController player;
    public Button returnButton;

    public bool isLocked { get; private set; } = false;

    private float originalFOV;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        canvas = GetComponentInChildren<Canvas>(true);
        graphicRaycaster = GetComponentInChildren<GraphicRaycaster>(true);
        bettingHouseUI = GetComponentInChildren<BettingHouseUI>(true);

        if (canvas != null)
        {
            bettingCanvas = canvas.gameObject;
        }
    }

    private void Start()
    {
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;
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

        if (bettingCanvas != null && bettingCanvas.activeSelf)
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
        if (outline != null) outline.enabled = false;

        // Guardar la vista original del jugador e iniciar transición hacia el mostrador
        if (Camera.main != null)
        {
            originalFOV = Camera.main.fieldOfView;

            if (cameraTargetPose != null)
            {
                StartCoroutine(TransitionToScreen(
                    Camera.main.transform,
                    cameraTargetPose.position,
                    cameraTargetPose.rotation,
                    originalFOV,
                    targetFOV,
                    transitionDuration,
                    () => {
                        // Al terminar de llegar al mostrador, activamos el raycaster para poder hacer clic
                        if (graphicRaycaster != null) graphicRaycaster.enabled = true;
                    }
                ));
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnReturn()
    {
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;

        // Devolvemos el control al jugador inmediatamente para que pueda moverse durante el damping
        if (player != null) player.ToggleControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (Camera.main != null && player != null)
        {
            // 🔑 Usamos la propiedad pública CameraTransform que añadimos al FPSController
            Transform playerCamTarget = player.cameraTransform;
            StartCoroutine(DynamicReturnTransition(Camera.main.transform, playerCamTarget, targetFOV, originalFOV, transitionDuration));
        }
    }

    // Corrutina para ir hacia la pantalla de la apuesta
    private IEnumerator TransitionToScreen(Transform camTransform, Vector3 targetPos, Quaternion targetRot, float startFOV, float endFOV, float duration, System.Action onComplete)
    {
        float elapsedTime = 0f;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            camTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, endFOV, smoothT);

            yield return null;
        }

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;
        Camera.main.fieldOfView = endFOV;

        onComplete?.Invoke();
    }

    // Corrutina de retorno dinámico (persiguiendo al jugador si se mueve)
    private IEnumerator DynamicReturnTransition(Transform camTransform, Transform playerTarget, float startFOV, float endFOV, float duration)
    {
        float elapsedTime = 0f;

        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // El destino se actualiza cada frame siguiendo la posición y rotación actual de la cámara del jugador
            Vector3 dynamicTargetPos = playerTarget.position;
            Quaternion dynamicTargetRot = playerTarget.rotation;

            camTransform.position = Vector3.Lerp(startPos, dynamicTargetPos, smoothT);
            camTransform.rotation = Quaternion.Slerp(startRot, dynamicTargetRot, smoothT);
            Camera.main.fieldOfView = Mathf.Lerp(startFOV, endFOV, smoothT);

            yield return null;
        }

        // Acople final perfecto en los ojos del jugador
        camTransform.position = playerTarget.position;
        camTransform.rotation = playerTarget.rotation;
        Camera.main.fieldOfView = endFOV;
    }

    private void OnDestroy()
    {
        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.allBetScreens.Remove(this);
        }
    }
}
