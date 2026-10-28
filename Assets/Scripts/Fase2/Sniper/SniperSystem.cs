using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SniperSystem : MonoBehaviour
{
    [Header("Referencias Directas de Input (Arrastrar desde Asset)")]
    public InputActionReference lookAction;
    public InputActionReference aimAction;
    public InputActionReference shootAction;
    public InputActionReference zoomAction;
    public InputActionReference sprintAction;
    public InputActionReference slowTimeAction;

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
    public float standardPitchLimit = 45f;
    public float standardYawLimit = 60f;

    [Header("Límites de Rotación en Gradas VIP (Opcional)")]
    public float vipPitchLimit = 60f; // Límites más amplios o ajustados para la posición VIP
    public float vipYawLimit = 90f;

    private float activePitchLimit;
    private float activeYawLimit;

    [Header("Sway (Movimiento Errático de la Mirilla)")]
    public float swayIntensity = 1.5f;
    public float swaySpeed = 2f;
    public float swayDamping = 8f;

    [Header("Sistema de Contener la Respiración (Sprint)")]
    public float maxBreathTime = 4f;
    public float breathRecoveryRate = 2f;
    private float currentBreath;
    private bool isHoldingBreath = false;
    private bool breathDepleted = false;
    private float currentSwayWeight = 1f;

    [Header("UI de Respiración (Barra Horizontal)")]
    public Image breathBarImage;
    public float blinkSpeed = 10f;

    [Header("Retroceso (Recoil)")]
    public float baseRecoilAngle = 5f;
    public float recoilSpeed = 25f;
    public float recoilRecoverySpeed = 12f;
    public float recoilDuration = 0.25f;

    [Header("Animación y Recarga")]
    public Animator playerAnimator;
    public string reloadAnimTrigger = "Reload";
    public float reloadDuration = 2.0f;

    [Header("UI del Sniper & Punteros (Crosshairs)")]
    public GameObject sniperScopeUI;
    [Tooltip("Imagen del puntero estándar (activa por defecto al apuntar)")]
    public GameObject defaultCrosshairImage;
    [Tooltip("Imagen del puntero alternativo (se activa al apuntar a un caballo)")]
    public GameObject targetCrosshairImage;
    [Tooltip("Distancia máxima del raycast para detectar un caballo con la mira")]
    public float targetDetectionRange = 500f;

    [Header("--- MODIFICADORES / DESBLOQUEABLES ---")]
    public bool hasZoomMod = false;
    public float minZoomFOV = 5f;
    public float maxZoomFOV = 30f;
    public float zoomStep = 5f;

    public bool hasStockMod = false;
    public bool hasIdentifierMod = false;
    public bool hasBipodMod = false;

    [Header("Referencias de Reloj Mágico & VIP (Mercado Negro)")]
    public Transform vipCameraTransform;         // Transform del puesto VIP en las gradas
    public Image magicClockBarImage;             // Barra HUD (Image Type: Filled) para el Reloj Mágico
    public float maxMagicClockTime = 5f;         // Segundos totales de uso del reloj
    private float currentMagicClockTime;
    private bool isUsingMagicClock = false;
    private bool clockWasUsedInRace = false;

    // Estados internos
    private bool isAiming = false;
    private bool isShootingOrReloading = false;
    private bool isRecounding = false;
    private float currentRecoilOffset = 0f;
    private float targetRecoilOffset = 0f;

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

        // 🪄 1. POLVO MÁGICO: Aumentar MaxBreathTime si se compró
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActivePolvoMagico)
        {
            maxBreathTime += BlackMarketManager.Instance.breathTimeIncrease;
            Debug.Log($"[SniperSystem] Polvo Mágico activo. Nuevo MaxBreathTime: {maxBreathTime}s");
        }

        currentBreath = maxBreathTime;

        // ⏱️ 2. RELOJ MÁGICO: Inicializar tiempo del Reloj Mágico
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock)
        {
            currentMagicClockTime = maxMagicClockTime;
        }
    }

    private void OnEnable()
    {
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

        if (sprintAction != null)
        {
            sprintAction.action.Enable();
            sprintAction.action.started += OnSprintStarted;
            sprintAction.action.canceled += OnSprintCanceled;
        }

        if (slowTimeAction != null)
        {
            slowTimeAction.action.Enable();
            slowTimeAction.action.started += OnSlowTimeStarted;
            slowTimeAction.action.canceled += OnSlowTimeCanceled;
        }
    }

    private void OnDisable()
    {
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

        if (sprintAction != null)
        {
            sprintAction.action.started -= OnSprintStarted;
            sprintAction.action.canceled -= OnSprintCanceled;
            sprintAction.action.Disable();
        }

        if (slowTimeAction != null)
        {
            slowTimeAction.action.started -= OnSlowTimeStarted;
            slowTimeAction.action.canceled -= OnSlowTimeCanceled;
            slowTimeAction.action.Disable();
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 🎟️ 3. ENTRADA VIP: Teletransportar cámara y ajustar límites si se compró el pase
        bool isVipActive = BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveVipTicket;

        activePitchLimit = isVipActive ? vipPitchLimit : standardPitchLimit;
        activeYawLimit = isVipActive ? vipYawLimit : standardYawLimit;

        if (isVipActive && vipCameraTransform != null)
        {
            transform.position = vipCameraTransform.position;
            transform.rotation = vipCameraTransform.rotation;
            Debug.Log("[SniperSystem] Entrada VIP activa: Cámara posicionada en las gradas VIP y límites ajustados.");
        }

        Vector3 initialEuler = transform.eulerAngles;
        initialPitch = initialEuler.x > 180f ? initialEuler.x - 360f : initialEuler.x;
        initialYaw = initialEuler.y;

        pitch = initialPitch;
        yaw = initialYaw;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);
        if (breathBarImage != null) breathBarImage.gameObject.SetActive(false);
        if (magicClockBarImage != null) magicClockBarImage.gameObject.SetActive(false);
        UpdateCrosshairsUI(false, false);
    }

    private void Update()
    {
        HandleBreathAndSwayState();

        if (isAiming)
        {
            CheckTargetingHorse();
        }

        if (isShootingOrReloading)
        {
            HandleRecoilOnly();
            ApplyCameraFOV();
            return;
        }

        HandleRotation();
        HandleSway();
        HandleRecoilOnly();
        ApplyCameraFOV();
        HandleMagicClock();
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

    private void OnSprintStarted(InputAction.CallbackContext ctx)
    {
        if (isAiming && !breathDepleted)
        {
            isHoldingBreath = true;
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        isHoldingBreath = false;
    }

    private void OnSlowTimeStarted(InputAction.CallbackContext ctx)
    {
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock && currentMagicClockTime > 0f)
        {
            isUsingMagicClock = true;
            clockWasUsedInRace = true;
        }
    }

    private void OnSlowTimeCanceled(InputAction.CallbackContext ctx)
    {
        isUsingMagicClock = false;
    }

    // ==========================================
    // LÓGICA DE RESPIRACIÓN Y SWAY
    // ==========================================

    private void HandleBreathAndSwayState()
    {
        if (!isAiming)
        {
            isHoldingBreath = false;
            currentBreath = Mathf.MoveTowards(currentBreath, maxBreathTime, breathRecoveryRate * Time.deltaTime);
            if (currentBreath >= maxBreathTime) breathDepleted = false;

            if (breathBarImage != null) breathBarImage.gameObject.SetActive(false);
            currentSwayWeight = Mathf.MoveTowards(currentSwayWeight, 1f, swayDamping * Time.deltaTime);
            return;
        }

        if (breathBarImage != null && !breathBarImage.gameObject.activeSelf)
        {
            breathBarImage.gameObject.SetActive(true);
        }

        if (isHoldingBreath && !breathDepleted)
        {
            currentBreath -= Time.deltaTime;
            if (currentBreath <= 0f)
            {
                currentBreath = 0f;
                breathDepleted = true;
                isHoldingBreath = false;
            }
        }
        else
        {
            currentBreath += breathRecoveryRate * Time.deltaTime;
            currentBreath = Mathf.Clamp(currentBreath, 0f, maxBreathTime);

            if (breathDepleted && currentBreath >= maxBreathTime)
            {
                breathDepleted = false;
            }
        }

        float targetSwayWeight = (isHoldingBreath && !hasStockMod) ? 0f : 1f;
        currentSwayWeight = Mathf.MoveTowards(currentSwayWeight, targetSwayWeight, swayDamping * Time.deltaTime);

        UpdateBreathUI();
    }

    private void UpdateBreathUI()
    {
        if (breathBarImage == null) return;

        float fillRatio = currentBreath / maxBreathTime;

        RectTransform rect = breathBarImage.rectTransform;
        rect.localScale = new Vector3(fillRatio, rect.localScale.y, rect.localScale.z);

        if (breathDepleted)
        {
            float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            breathBarImage.color = new Color(1f, 0f, 0f, alpha > 0.5f ? 1f : 0.3f);
        }
        else
        {
            breathBarImage.color = Color.white;
        }
    }

    // ==========================================
    // LÓGICA DE ROTACIÓN, SWAY Y RECOIL
    // ==========================================

    private void HandleRotation()
    {
        yaw += lookInput.x * mouseSensitivity;
        pitch += lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, initialPitch - activePitchLimit, initialPitch + activePitchLimit);
        yaw = Mathf.Clamp(yaw, initialYaw - activeYawLimit, initialYaw + activeYawLimit);

        float finalPitch = pitch + (swayOffset.x * currentSwayWeight) + currentRecoilOffset;
        float finalYaw = yaw + (swayOffset.y * currentSwayWeight);

        transform.eulerAngles = new Vector3(finalPitch, finalYaw, 0f);
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

    private void HandleRecoilOnly()
    {
        float speed = isRecounding ? recoilSpeed : recoilRecoverySpeed;
        currentRecoilOffset = Mathf.MoveTowards(currentRecoilOffset, targetRecoilOffset, speed * Time.deltaTime);

        if (Mathf.Approximately(currentRecoilOffset, targetRecoilOffset) && targetRecoilOffset != 0f)
        {
            targetRecoilOffset = 0f;
            isRecounding = false;
        }

        if (isShootingOrReloading)
        {
            float finalPitch = pitch + currentRecoilOffset;
            transform.eulerAngles = new Vector3(finalPitch, yaw, 0f);
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
        UpdateCrosshairsUI(true, false); // Activar mira estándar por defecto

        OnAimStateChanged?.Invoke(true, hasIdentifierMod);
    }

    public void StopAiming()
    {
        isAiming = false;
        isHoldingBreath = false;
        currentFOV = normalFOV;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);
        UpdateCrosshairsUI(false, false); // Apagar ambas

        OnAimStateChanged?.Invoke(false, hasIdentifierMod);
    }

    // ==========================================
    // DETECCIÓN DE CABALLO CON LA MIRA
    // ==========================================

    private void CheckTargetingHorse()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool aimingAtHorse = false;

        if (Physics.Raycast(ray, out RaycastHit hit, targetDetectionRange))
        {
            HorseRunner targetHorse = hit.collider.GetComponentInParent<HorseRunner>();
            if (targetHorse != null && !targetHorse.hasFallen && !targetHorse.hasBeenShot && !targetHorse.hasFinished)
            {
                aimingAtHorse = true;
            }
        }

        // Alternar entre el puntero estándar y el puntero de objetivo fijado
        UpdateCrosshairsUI(true, aimingAtHorse);
    }

    private void UpdateCrosshairsUI(bool scopeActive, bool targetingHorse)
    {
        if (!scopeActive)
        {
            if (defaultCrosshairImage != null) defaultCrosshairImage.SetActive(false);
            if (targetCrosshairImage != null) targetCrosshairImage.SetActive(false);
            return;
        }

        if (defaultCrosshairImage != null)
        {
            defaultCrosshairImage.SetActive(!targetingHorse);
        }

        if (targetCrosshairImage != null)
        {
            targetCrosshairImage.SetActive(targetingHorse);
        }
    }

    // ==========================================
    // RUTINA DE DISPARO Y RECARGA SEPARADAS
    // ==========================================

    private IEnumerator ShootRoutine()
    {
        isShootingOrReloading = true;
        currentAmmo--;

        // ⏱️ Cancelar inmediatamente la habilidad del Reloj Mágico y restaurar velocidad normal
        isUsingMagicClock = false;
        Time.timeScale = 1f;

        // 1. Raycast de impacto
        PerformRaycastShoot();

        // 2. FASE 1: Recoil
        float effectiveRecoil = hasBipodMod ? (baseRecoilAngle * 0.35f) : baseRecoilAngle;
        targetRecoilOffset = -effectiveRecoil;
        isRecounding = true;

        yield return new WaitForSeconds(recoilDuration);

        // 3. FASE 2: Fin de la mira y comienzo de la Recarga
        StopAiming();

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(reloadAnimTrigger);
        }

        yield return new WaitForSeconds(reloadDuration);

        isShootingOrReloading = false;

        if (aimAction != null && aimAction.action.IsPressed())
        {
            StartAiming();
        }
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

    // ==========================================
    // LÓGICA Y BARRA REGRESIVA DEL RELOJ MÁGICO
    // ==========================================

    private void HandleMagicClock()
    {
        bool hasClock = BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock;

        if (!hasClock)
        {
            if (magicClockBarImage != null) magicClockBarImage.gameObject.SetActive(false);
            Time.timeScale = 1f;
            return;
        }

        if (magicClockBarImage != null && !magicClockBarImage.gameObject.activeSelf)
        {
            magicClockBarImage.gameObject.SetActive(true);
        }

        // 🎯 REQUISITO: Estar apuntando, mantener Ctrl/habilidad Y NO estar disparando o recargando
        bool inputPressed = slowTimeAction != null && slowTimeAction.action.IsPressed();
        bool canUseClock = isAiming && !isShootingOrReloading && inputPressed && currentMagicClockTime > 0f;

        if (canUseClock)
        {
            isUsingMagicClock = true;
            clockWasUsedInRace = true;
            currentMagicClockTime -= Time.unscaledDeltaTime; // Descuenta tiempo real ignorando timeScale
            Time.timeScale = 0.3f; // Ralentiza la escena
        }
        else
        {
            isUsingMagicClock = false;
            // Solo restaurar timeScale si no hay otro sistema modificándolo externamente
            if (!canUseClock && Time.timeScale < 1f)
            {
                Time.timeScale = 1f;
            }
        }

        if (currentMagicClockTime <= 0f)
        {
            currentMagicClockTime = 0f;
            isUsingMagicClock = false;
            Time.timeScale = 1f;
        }

        // 📊 ACTUALIZAR BARRA HUD
        if (magicClockBarImage != null)
        {
            float fillRatio = currentMagicClockTime / maxMagicClockTime;

            if (magicClockBarImage.type == Image.Type.Filled)
            {
                magicClockBarImage.fillAmount = fillRatio;
            }
            else
            {
                RectTransform rect = magicClockBarImage.rectTransform;
                rect.localScale = new Vector3(fillRatio, rect.localScale.y, rect.localScale.z);
            }
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f; // Garantizar restablecimiento de la velocidad del juego al cambiar de escena
        if (BlackMarketManager.Instance != null)
        {
            BlackMarketManager.Instance.NotifyRaceFinished(clockWasUsedInRace);
        }
    }
}
