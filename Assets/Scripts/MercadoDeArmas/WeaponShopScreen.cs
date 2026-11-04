using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WeaponShopScreen : MonoBehaviour, IInteractable
{
    private Outline outline;
    private Canvas canvas;
    public GraphicRaycaster graphicRaycaster;
    private GameObject shopCanvas;

    [Header("Configuración de Cámara Suave (Damping)")]
    [Tooltip("El Transform (posición y rotación) donde se colocará la cámara al mirar la tienda.")]
    public Transform cameraTargetPose;
    [Tooltip("El campo de visión (FOV) que tendrá la cámara al enfocar la tienda.")]
    public float targetFOV = 30f;
    [Tooltip("Duración de la transición en segundos (ej. 0.5 para un damping suave).")]
    public float transitionDuration = 0.5f;

    [SerializeField] private FPSController player;
    public Button returnButton;

    public bool isLocked { get; private set; } = false;

    // Variables para guardar la vista original del jugador
    private Vector3 originalCamPos;
    private Quaternion originalCamRot;
    private float originalFOV;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        canvas = GetComponentInChildren<Canvas>(true);
        graphicRaycaster = GetComponentInChildren<GraphicRaycaster>(true);

        if (canvas != null)
        {
            shopCanvas = canvas.gameObject;
        }
    }

    private void Start()
    {
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;
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

        if (shopCanvas != null && shopCanvas.activeSelf)
        {
            OnReturn();
        }
    }

    public void OnHovered()
    {
        if (isLocked) return;
        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (isLocked) return;

        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.DisableAllRaycasters();
        }

        if (player != null) player.ToggleControls(false);
        if (outline != null) outline.enabled = false;

        // Guardar la posición, rotación y FOV actuales de la cámara principal antes de moverla
        if (Camera.main != null)
        {
            originalCamPos = Camera.main.transform.position;
            originalCamRot = Camera.main.transform.rotation;
            originalFOV = Camera.main.fieldOfView;

            if (cameraTargetPose != null)
            {
                // Iniciar transición suave hacia la tienda
                StartTransition(Camera.main.transform, cameraTargetPose.position, cameraTargetPose.rotation, originalFOV, targetFOV, transitionDuration, () =>
                {
                    // Al terminar de llegar, activamos la UI de la tienda
                    if (graphicRaycaster != null) graphicRaycaster.enabled = true;
                });
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnReturn()
    {
        // Ocultar la UI inmediatamente para que no estorbe durante el viaje de vuelta de la cámara
        if (graphicRaycaster != null) graphicRaycaster.enabled = false;
        if (shopCanvas != null) shopCanvas.SetActive(false);

        if (player != null) player.ToggleControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Transición de vuelta a la posición original del jugador
        if (Camera.main != null)
        {
            StartTransition(Camera.main.transform, originalCamPos, originalCamRot, targetFOV, originalFOV, transitionDuration, null);
        }
    }

    private void StartTransition(Transform camTransform, Vector3 targetPos, Quaternion targetRot, float startFOV, float endFOV, float duration, System.Action onComplete)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionRoutine(camTransform, targetPos, targetRot, startFOV, endFOV, duration, onComplete));
    }

    private IEnumerator TransitionRoutine(Transform camTransform, Vector3 targetPos, Quaternion targetRot, float startFOV, float endFOV, float duration, System.Action onComplete)
    {
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        Camera cam = camTransform.GetComponent<Camera>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep aplica una aceleración y frenado natural (efecto damping)
            t = Mathf.SmoothStep(0f, 1f, t);

            camTransform.position = Vector3.Lerp(startPos, targetPos, t);
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            }

            yield return null;
        }

        camTransform.position = targetPos;
        camTransform.rotation = targetRot;
        if (cam != null)
        {
            cam.fieldOfView = endFOV;
        }

        onComplete?.Invoke();
    }
}
