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

    [Header("Configuración de Tienda Avanzada")]
    [Tooltip("Marca esto si esta pantalla corresponde a la tienda del sniper mejorado.")]
    [SerializeField] private bool isAdvancedShop = false;

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

        // 🔒 Si es la tienda avanzada y el sniper mejorado NO está comprado, no mostramos el outline
        if (isAdvancedShop && (WeaponShopManager.Instance == null || !WeaponShopManager.Instance.hasAdvancedSniperUnlocked))
        {
            return;
        }

        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (isLocked) return;

        // 🔒 Si es la tienda avanzada y el sniper mejorado NO está comprado, bloqueamos la interacción por completo
        if (isAdvancedShop && (WeaponShopManager.Instance == null || !WeaponShopManager.Instance.hasAdvancedSniperUnlocked))
        {
            Debug.Log("[WeaponShopScreen] Acceso denegado: Debes comprar el sniper mejorado primero.");
            return;
        }

        if (GlobalBettingManager.Instance != null)
        {
            GlobalBettingManager.Instance.DisableAllRaycasters();
        }

        if (player != null) player.ToggleControls(false);
        if (outline != null) outline.enabled = false;

        // Activar el canvas de la tienda si estuviera desactivado
        if (shopCanvas != null) shopCanvas.SetActive(true);

        // Guardar la posición, rotación y FOV actuales de la cámara principal antes de moverla
        if (Camera.main != null)
        {
            originalCamPos = Camera.main.transform.position;
            originalCamRot = Camera.main.transform.rotation;
            originalFOV = Camera.main.fieldOfView;

            if (cameraTargetPose != null)
            {
                // Iniciar transición suave hacia la tienda
                StartCoroutine(TransitionToScreen(Camera.main.transform, cameraTargetPose.position, cameraTargetPose.rotation, originalFOV, targetFOV, transitionDuration, () =>
                {
                    // Al terminar de llegar, activamos el raycaster de la tienda
                    if (graphicRaycaster != null) graphicRaycaster.enabled = true;
                }));
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
}
