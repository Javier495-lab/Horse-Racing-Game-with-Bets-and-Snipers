using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SniperSystem : MonoBehaviour
{
    [Header("Referencias Directas de Input (Arrastrar desde Asset)")]
    public InputActionReference lookAction;
    public InputActionReference aimAction;
    public InputActionReference shootAction;
    public InputActionReference zoomAction;

    [Header("Munición")]
    public int maxAmmo = 5;
    public int currentAmmo;

    [Header("Configuración de Apuntado y Cámara")]
    public Camera mainCamera;
    public float normalFOV = 60f;
    public float baseAimFOV = 20f;
    public float currentFOV;
    public float mouseSensitivity = 0.1f;

    [Header("Límites de Rotación de Cámara (FPS Estático)")]
    [Tooltip("Ángulo máximo hacia arriba y abajo desde la orientación inicial.")]
    public float pitchLimit = 45f;

    [Tooltip("Ángulo máximo hacia la izquierda y derecha desde la orientación inicial.")]
    public float yawLimit = 60f;

    [Header("Sway (Movimiento Errático de la Mirilla)")]
    public float swayIntensity = 1.5f;
    public float swaySpeed = 2f;

    [Header("Retroceso (Recoil)")]
    public float baseRecoilAngle = 5f;
    public float recoilRecoverySpeed = 10f;
    public float recoilDuration = 0.25f;

    [Header("Animación y Recarga")]
    public Animator playerAnimator;
    public string reloadAnimTrigger = "Reload";
    public float reloadDuration = 2.0f;

    [Header("UI del Sniper")]
    public GameObject sniperScopeUI;

    [Header("--- MODIFICADORES / DESBLOQUEABLES ---")]
    [Tooltip("Permite ajustar el zoom dinámicamente con la rueda del ratón / bumpers.")]
    public bool hasZoomMod = false;
    public float minZoomFOV = 5f;
    public float maxZoomFOV = 30f;
    public float zoomStep = 5f;

    [Tooltip("Cancela por completo el sway/movimiento errático al apuntar.")]
    public bool hasStockMod = false;

    [Tooltip("Evita que los identificadores UI sobre los caballos desaparezcan al apuntar.")]
    public bool hasIdentifierMod = false;

    [Tooltip("Reduce el impacto y la brusquedad del retroceso al disparar.")]
    public bool hasBipodMod = false;

    // Estados internos
    private bool isAiming = false;
    private bool isShootingOrReloading = false;

    // Ángulos actuales e iniciales
    private float pitch = 0f;
    private float yaw = 0f;
    private float initialPitch = 0f;
    private float initialYaw = 0f;

    // Sway interno
    private float swayTimer = 0f;
    private Vector2 swayOffset = Vector2.zero;

    // Entrada del Input System
    private Vector2 lookInput;

    public delegate void AimStateChanged(bool isAiming, bool hasIdentifierMod);
    public static event AimStateChanged OnAimStateChanged;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        currentFOV = normalFOV;
        currentAmmo = maxAmmo;
    }

    private void OnEnable()
    {
        // Suscribir y habilitar acciones del Input System de forma explícita
        if (lookAction != null)
        {
            lookAction.action.Enable();
            lookAction.action.performed += OnLookPerformed;
            lookAction.action.canceled += OnLookCanceled;
        }

        if (aimAction != null)
        {
            aimAction.action.Enable();
            aimAction.action.started += OnAimStarted;
            aimAction.action.canceled += OnAimCanceled;
        }

        if (shootAction != null)
        {
            shootAction.action.Enable();
            shootAction.action.started += OnShootStarted;
        }

        if (zoomAction != null)
        {
            zoomAction.action.Enable();
            zoomAction.action.performed += OnZoomPerformed;
        }
    }

    private void OnDisable()
    {
        // Desuscribir acciones
        if (lookAction != null)
        {
            lookAction.action.performed -= OnLookPerformed;
            lookAction.action.canceled -= OnLookCanceled;
            lookAction.action.Disable();
        }

        if (aimAction != null)
        {
            aimAction.action.started -= OnAimStarted;
            aimAction.action.canceled -= OnAimCanceled;
            aimAction.action.Disable();
        }

        if (shootAction != null)
        {
            shootAction.action.started -= OnShootStarted;
            shootAction.action.Disable();
        }

        if (zoomAction != null)
        {
            zoomAction.action.performed -= OnZoomPerformed;
            zoomAction.action.Disable();
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 initialEuler = transform.eulerAngles;
        initialPitch = initialEuler.x > 180f ? initialEuler.x - 360f : initialEuler.x;
        initialYaw = initialEuler.y;

        pitch = initialPitch;
        yaw = initialYaw;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);
    }

    private void Update()
    {
        if (isShootingOrReloading) return;

        HandleRotation();
        HandleSway();
        ApplyCameraFOV();
    }

    // ==========================================
    // CALLBACKS DE ENTRADA
    // ==========================================

    private void OnLookPerformed(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void OnAimStarted(InputAction.CallbackContext ctx)
    {
        if (!isShootingOrReloading) StartAiming();
    }

    private void OnAimCanceled(InputAction.CallbackContext ctx)
    {
        if (!isShootingOrReloading) StopAiming();
    }

    private void OnShootStarted(InputAction.CallbackContext ctx)
    {
        if (isAiming && !isShootingOrReloading)
        {
            if (currentAmmo > 0)
            {
                StartCoroutine(ShootRoutine());
            }
            else
            {
                Debug.Log("[Sniper] ¡Sin munición!");
            }
        }
    }

    private void OnZoomPerformed(InputAction.CallbackContext ctx)
    {
        if (!hasZoomMod || !isAiming || isShootingOrReloading) return;

        float zoomValue = ctx.ReadValue<float>();
        if (Mathf.Abs(zoomValue) > 0.1f)
        {
            currentFOV -= Mathf.Sign(zoomValue) * zoomStep;
            currentFOV = Mathf.Clamp(currentFOV, minZoomFOV, maxZoomFOV);
        }
    }

    // ==========================================
    // LÓGICA DE ROTACIÓN Y CÁMARA
    // ==========================================

    private void HandleRotation()
    {
        yaw += lookInput.x * mouseSensitivity;

        // CAMBIO AQUÍ: Cambiamos '-' por '+' para desinvertir el eje vertical
        pitch += lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, initialPitch - pitchLimit, initialPitch + pitchLimit);
        yaw = Mathf.Clamp(yaw, initialYaw - yawLimit, initialYaw + yawLimit);

        Vector3 targetEuler = new Vector3(pitch + swayOffset.x, yaw + swayOffset.y, 0f);
        transform.eulerAngles = targetEuler;
    }

    private void HandleSway()
    {
        if (isAiming && !hasStockMod)
        {
            swayTimer += Time.deltaTime * swaySpeed;
            float offsetX = Mathf.Sin(swayTimer) * swayIntensity;
            float offsetY = Mathf.Cos(swayTimer * 1.5f) * (swayIntensity * 0.7f);
            swayOffset = new Vector2(offsetX, offsetY);
        }
        else
        {
            swayOffset = Vector2.zero;
            swayTimer = 0f;
        }
    }

    private void ApplyCameraFOV()
    {
        if (mainCamera != null)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, currentFOV, Time.deltaTime * 12f);
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        currentFOV = baseAimFOV;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(true);

        OnAimStateChanged?.Invoke(true, hasIdentifierMod);
    }

    private void StopAiming()
    {
        isAiming = false;
        currentFOV = normalFOV;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);

        OnAimStateChanged?.Invoke(false, hasIdentifierMod);
    }

    private IEnumerator ShootRoutine()
    {
        isShootingOrReloading = true;
        currentAmmo--;

        PerformRaycastShoot();

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);
        OnAimStateChanged?.Invoke(false, hasIdentifierMod);

        float effectiveRecoil = hasBipodMod ? (baseRecoilAngle * 0.35f) : baseRecoilAngle;
        float startPitch = pitch;
        float targetPitch = Mathf.Clamp(pitch - effectiveRecoil, initialPitch - pitchLimit, initialPitch + pitchLimit);

        float elapsed = 0f;
        while (elapsed < recoilDuration)
        {
            pitch = Mathf.Lerp(startPitch, targetPitch, elapsed / recoilDuration);
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isAiming = false;
        currentFOV = normalFOV;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(reloadAnimTrigger);
        }

        yield return new WaitForSeconds(reloadDuration);

        isShootingOrReloading = false;
    }

    private void PerformRaycastShoot()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            HorseRunner targetHorse = hit.collider.GetComponentInParent<HorseRunner>();
            if (targetHorse != null)
            {
                targetHorse.TriggerShot();
            }
        }
    }
}
