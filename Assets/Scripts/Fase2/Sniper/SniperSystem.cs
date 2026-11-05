using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro; // Asegurar referencia para textos si se usa TextMeshPro en el indicador flotante

public class SniperSystem : MonoBehaviour
{
    [Header("Referencias Directas de Input (Arrastrar desde Asset)")]
    public InputActionReference lookAction;
    public InputActionReference aimAction;
    public InputActionReference shootAction;
    public InputActionReference zoomAction;
    public InputActionReference sprintAction;
    public InputActionReference slowTimeAction;

    [Header("Referencias Visuales de los Modelos (Armas)")]
    [Tooltip("El objeto 3D o GameObject del Sniper Base")]
    public GameObject baseSniperModel;
    [Tooltip("El objeto 3D o GameObject del Sniper Mejorado")]
    public GameObject advancedSniperModel;

    [Header("Munición")]
    public int standardMaxAmmo = 5;
    public int advancedMaxAmmo = 6;
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
    public float vipPitchLimit = 60f;
    public float vipYawLimit = 90f;

    private float activePitchLimit;
    private float activeYawLimit;

    [Header("Sway (Movimiento Errático de la Mirilla)")]
    public float swayIntensity = 1.5f;
    public float swaySpeed = 2f;
    public float swayDamping = 8f;

    [Header("Ajustes de Modificadores")]
    [Tooltip("Multiplicador del temblor cuando se tiene el Bípode (0.15 = 15% del sway original)")]
    public float bipodSwayMultiplier = 0.15f;

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

    [Header("Retroceso (Recoil & Easing)")]
    public float baseRecoilAngle = 5f;
    [Tooltip("Suavizado inicial del retroceso (mayor = más rápido al inicio, frena suave al llegar al pico).")]
    public float recoilEaseSpeed = 20f;
    [Tooltip("Suavizado de la recuperación hacia la posición original.")]
    public float recoveryEaseSpeed = 12f;
    public float recoilDuration = 0.25f;

    [Header("Screen Shake Dinámico")]
    public float screenShakeIntensity = 0.4f;
    public float screenShakeFrequency = 45f;
    private Vector2 currentScreenShakeOffset = Vector2.zero;



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

    [Header("--- TIEMPOS DE DISPARO (SNIPER MEJORADO) ---")]
    [Tooltip("Tiempo base de espera entre disparos para el sniper mejorado sin stock.")]
    public float baseAdvancedDelay = 2f;
    [Tooltip("Tiempo de espera reducido entre disparos cuando se tiene la mejora de Stock.")]
    public float stockAdvancedDelay = 1f;

    [Header("--- MODIFICADORES / DESBLOQUEABLES ---")]
    public bool hasZoomMod = false;
    public float minZoomFOV = 5f;
    public float maxZoomFOV = 30f;
    public float zoomStep = 5f;

    public bool hasStockMod = false;
    public bool hasIdentifierMod = false;
    public bool hasBipodMod = false;
    public bool hasAdvancedSniper = false;

    // 🎯 AIM BOT (Mercado Negro)
    [Header("--- AIM BOT ---")]
    public bool hasAimBot = false;
    private Transform lockedTargetHorseTransform = null;

    [Header("Referencias de Reloj Mágico & VIP (Mercado Negro)")]
    public Transform vipCameraTransform;
    public Image magicClockBarImage;
    public float maxMagicClockTime = 5f;
    private float currentMagicClockTime;
    private bool isUsingMagicClock = false;
    private bool clockWasUsedInRace = false;

    [Header("--- SUPRESIÓN DE SWAY POR MOVIMIENTO (INPUT) ---")]
    public bool enableInputSwaySuppression = true;
    [Tooltip("Umbral de movimiento del ratón/joystick a partir del cual el sway se desactiva.")]
    public float inputSwayThreshold = 0.5f;
    [Tooltip("Velocidad con la que el sway se desvanece al mover y reaparece al pararse.")]
    public float inputSwayFadeSpeed = 10f;
    private float currentInputSwayFactor = 1f;

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

    private Vector2 lookInput;
    private HorseRunner currentLookedAtHorse = null;
    private bool canShoot = true;

    public delegate void AimStateChanged(bool isAiming, bool hasIdentifierMod);
    public static event AimStateChanged OnAimStateChanged;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        currentFOV = normalFOV;

        // 1. Sincronizar mejoras, munición y el booleano hasAdvancedSniper desde el WeaponShopManager
        if (WeaponShopManager.Instance != null)
        {
            WeaponShopManager.Instance.ApplyUpgradesToSniper(this);
        }
        else
        {
            currentAmmo = maxAmmo;
        }

        // 🔑 2. ACTIVAR EL MODELO 3D CORRECTO SEGÚN EL RIFLE SELECCIONADO
        if (baseSniperModel != null) baseSniperModel.SetActive(!hasAdvancedSniper);
        if (advancedSniperModel != null) advancedSniperModel.SetActive(hasAdvancedSniper);

        // 3. POLVO MÁGICO
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActivePolvoMagico)
        {
            maxBreathTime += BlackMarketManager.Instance.breathTimeIncrease;
        }

        currentBreath = maxBreathTime;

        // 4. RELOJ MÁGICO
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock)
        {
            currentMagicClockTime = maxMagicClockTime;
        }

        // 5. AIM BOT
        if (BlackMarketManager.Instance != null)
        {
            string currentHouseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
            hasAimBot = BlackMarketManager.Instance.GetAimBotOwned(currentHouseKey);
        }
        else
        {
            hasAimBot = false;
        }
    }

    private void OnEnable()
    {
        if (lookAction != null) { lookAction.action.Enable(); lookAction.action.performed += OnLookPerformed; lookAction.action.canceled += OnLookCanceled; }
        if (aimAction != null) { aimAction.action.Enable(); aimAction.action.started += OnAimStarted; aimAction.action.canceled += OnAimCanceled; }
        if (shootAction != null) { shootAction.action.Enable(); shootAction.action.started += OnShootStarted; }
        if (zoomAction != null) { zoomAction.action.Enable(); zoomAction.action.performed += OnZoomPerformed; }
        if (sprintAction != null) { sprintAction.action.Enable(); sprintAction.action.started += OnSprintStarted; sprintAction.action.canceled += OnSprintCanceled; }
        if (slowTimeAction != null) { slowTimeAction.action.Enable(); slowTimeAction.action.started += OnSlowTimeStarted; slowTimeAction.action.canceled += OnSlowTimeCanceled; }
    }

    private void OnDisable()
    {
        if (lookAction != null) { lookAction.action.performed -= OnLookPerformed; lookAction.action.canceled -= OnLookCanceled; lookAction.action.Disable(); }
        if (aimAction != null) { aimAction.action.started -= OnAimStarted; aimAction.action.canceled -= OnAimCanceled; aimAction.action.Disable(); }
        if (shootAction != null) { shootAction.action.started -= OnShootStarted; shootAction.action.Disable(); }
        if (zoomAction != null) { zoomAction.action.performed -= OnZoomPerformed; zoomAction.action.Disable(); }
        if (sprintAction != null) { sprintAction.action.started -= OnSprintStarted; sprintAction.action.canceled -= OnSprintCanceled; sprintAction.action.Disable(); }
        if (slowTimeAction != null) { slowTimeAction.action.started -= OnSlowTimeStarted; slowTimeAction.action.canceled -= OnSlowTimeCanceled; serverActionDisabledCheck(); } // Mantener limpio
    }

    private void serverActionDisabledCheck() { /* Método auxiliar o vacío por seguridad si usabas otra cosa */ }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bool isVipActive = BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveVipTicket;
        activePitchLimit = isVipActive ? vipPitchLimit : standardPitchLimit;
        activeYawLimit = isVipActive ? vipYawLimit : standardYawLimit;

        if (isVipActive && vipCameraTransform != null)
        {
            transform.position = vipCameraTransform.position;
            transform.rotation = vipCameraTransform.rotation;
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
        else
        {
            ClearHorseIdentifierUI();
            lockedTargetHorseTransform = null;
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
        if (isAiming && !isShootingOrReloading && canShoot)
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
        if (isAiming && !breathDepleted) isHoldingBreath = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx) => isHoldingBreath = false;

    private void OnSlowTimeStarted(InputAction.CallbackContext ctx)
    {
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock && currentMagicClockTime > 0f)
        {
            isUsingMagicClock = true;
            clockWasUsedInRace = true;
        }
    }

    private void OnSlowTimeCanceled(InputAction.CallbackContext ctx) => isUsingMagicClock = false;

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
            if (breathDepleted && currentBreath >= maxBreathTime) breathDepleted = false;
        }

        // 🔑 CORRECCIÓN: Quitamos el filtro de !hasStockMod para que la respiración funcione siempre
        float targetSwayWeight = isHoldingBreath ? 0f : 1f;
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

    public float aimBotBreakThreshold = 3.0f;

    private void HandleRotation()
    {
        if (hasAimBot && isAiming && lockedTargetHorseTransform != null)
        {
            if (lookInput.sqrMagnitude > aimBotBreakThreshold)
            {
                lockedTargetHorseTransform = null;
            }
            else
            {
                Vector3 targetPosition = lockedTargetHorseTransform.position + Vector3.up * 1.5f;
                Vector3 targetDirection = targetPosition - transform.position;

                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                    Vector3 targetEuler = targetRotation.eulerAngles;

                    float targetPitch = targetEuler.x > 180f ? targetEuler.x - 360f : targetEuler.x;
                    float targetYaw = targetEuler.y;

                    pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * 15f);
                    yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * 15f);

                    transform.eulerAngles = new Vector3(pitch, yaw, 0f);
                    return;
                }
            }
        }

        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, initialPitch - activePitchLimit, initialPitch + activePitchLimit);
        yaw = Mathf.Clamp(yaw, initialYaw - activeYawLimit, initialYaw + activeYawLimit);

        float finalPitchVal = pitch + (swayOffset.x * currentSwayWeight) + currentRecoilOffset;
        float finalYawVal = yaw + (swayOffset.y * currentSwayWeight);

        transform.eulerAngles = new Vector3(finalPitchVal, finalYawVal, 0f);
    }

    private void HandleSway()
    {
        float targetInputFactor = 1f;
        if (enableInputSwaySuppression)
        {
            if (lookInput.sqrMagnitude > inputSwayThreshold)
            {
                targetInputFactor = 0f;
            }
        }
        currentInputSwayFactor = Mathf.MoveTowards(currentInputSwayFactor, targetInputFactor, inputSwayFadeSpeed * Time.deltaTime);

        if (isAiming)
        {
            swayTimer += Time.deltaTime * swaySpeed;
            float baseIntensity = hasBipodMod ? (swayIntensity * bipodSwayMultiplier) : swayIntensity;

            // Corregidos los operadores de multiplicación que faltaban aquí:
            float finalIntensity = baseIntensity * currentSwayWeight * currentInputSwayFactor;

            float offsetX = Mathf.Sin(swayTimer) * finalIntensity;
            float offsetY = Mathf.Cos(swayTimer * 1.5f) * (finalIntensity * 0.7f);

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
        // Velocidad variable con Lerp (suavizado al llegar al pico y al recuperar)
        float currentSpeed = isRecounding ? recoilEaseSpeed : recoveryEaseSpeed;
        currentRecoilOffset = Mathf.Lerp(currentRecoilOffset, targetRecoilOffset, currentSpeed * Time.deltaTime);

        // Si estamos en fase de retroceso y ya llegamos al pico, cambiamos a la fase de recuperación
        if (isRecounding && Mathf.Abs(currentRecoilOffset - targetRecoilOffset) < 0.05f)
        {
            targetRecoilOffset = 0f;
            isRecounding = false;
        }

        // 🫨 SCREEN SHAKE DINÁMICO: Funciona únicamente mientras el arma está retrocediendo (isRecounding)
        if (isShootingOrReloading && isRecounding)
        {
            float progress = Mathf.Abs(currentRecoilOffset) / Mathf.Max(baseRecoilAngle, 0.01f);
            float shakeIntensityMultiplier = Mathf.Clamp01(progress);

            float shakeX = Mathf.Sin(Time.time * screenShakeFrequency) * screenShakeIntensity * shakeIntensityMultiplier;
            float shakeY = Mathf.Cos(Time.time * (screenShakeFrequency * 1.3f)) * screenShakeIntensity * shakeIntensityMultiplier;
            currentScreenShakeOffset = new Vector2(shakeX, shakeY);
        }
        else
        {
            currentScreenShakeOffset = Vector2.zero;
        }

        // 🎯 ROTACIÓN FINAL COMBINADA (Pitch y Yaw con Recoil, Screen Shake y Sway)
        float finalPitch = pitch + currentRecoilOffset + currentScreenShakeOffset.y + swayOffset.y;
        float finalYaw = yaw + currentScreenShakeOffset.x + swayOffset.x;

        transform.eulerAngles = new Vector3(finalPitch, finalYaw, 0f);
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
        UpdateCrosshairsUI(true, false);

        OnAimStateChanged?.Invoke(true, hasIdentifierMod);
    }

    public void StopAiming()
    {
        isAiming = false;
        isHoldingBreath = false;
        currentFOV = normalFOV;
        lockedTargetHorseTransform = null;

        if (sniperScopeUI != null) sniperScopeUI.SetActive(false);
        UpdateCrosshairsUI(false, false);
        ClearHorseIdentifierUI();

        OnAimStateChanged?.Invoke(false, hasIdentifierMod);
    }

    private void CheckTargetingHorse()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool aimingAtHorse = false;
        HorseRunner detectedHorse = null;

        if (Physics.Raycast(ray, out RaycastHit hit, targetDetectionRange))
        {
            HorseRunner targetHorse = hit.collider.GetComponentInParent<HorseRunner>();
            if (targetHorse != null && !targetHorse.hasFallen && !targetHorse.hasBeenShot && !targetHorse.hasFinished)
            {
                aimingAtHorse = true;
                detectedHorse = targetHorse;

                if (hasAimBot && lockedTargetHorseTransform == null)
                {
                    lockedTargetHorseTransform = targetHorse.transform;
                }
            }
        }

        UpdateCrosshairsUI(true, aimingAtHorse);
        HandleHorseIdentifierUI(detectedHorse);
    }

    private void HandleHorseIdentifierUI(HorseRunner targetHorse)
    {
        if (currentLookedAtHorse != targetHorse)
        {
            ClearHorseIdentifierUI();
            currentLookedAtHorse = targetHorse;
        }

        if (currentLookedAtHorse != null && hasIdentifierMod)
        {
            HorseIdentifierUI identifierUI = currentLookedAtHorse.GetComponentInChildren<HorseIdentifierUI>(true);
            if (identifierUI != null)
            {
                identifierUI.ShowIdentifier(true, currentLookedAtHorse.currentBet > 0f);
            }
        }
    }

    private void ClearHorseIdentifierUI()
    {
        if (currentLookedAtHorse != null)
        {
            HorseIdentifierUI identifierUI = currentLookedAtHorse.GetComponentInChildren<HorseIdentifierUI>(true);
            if (identifierUI != null)
            {
                identifierUI.ShowIdentifier(false, false);
            }
            currentLookedAtHorse = null;
        }
    }

    private void UpdateCrosshairsUI(bool scopeActive, bool targetingHorse)
    {
        if (!scopeActive)
        {
            if (defaultCrosshairImage != null) defaultCrosshairImage.SetActive(false);
            if (targetCrosshairImage != null) targetCrosshairImage.SetActive(false);
            return;
        }

        if (defaultCrosshairImage != null) defaultCrosshairImage.SetActive(!targetingHorse);
        if (targetCrosshairImage != null) targetCrosshairImage.SetActive(targetingHorse);
    }

    private IEnumerator ShootRoutine()
    {
        // Bloqueamos inmediatamente la posibilidad de hacer otro disparo
        canShoot = false;
        currentAmmo--;

        isUsingMagicClock = false;
        Time.timeScale = 1f;

        PerformRaycastShoot();

        float effectiveRecoil = hasBipodMod ? (baseRecoilAngle * 0.35f) : baseRecoilAngle;
        targetRecoilOffset = -effectiveRecoil;
        isRecounding = true;

        yield return new WaitForSeconds(recoilDuration);

        // 🎯 DIFERENCIACIÓN ENTRE RIFLES TRAS EL RECOIL
        if (hasAdvancedSniper)
        {
            // ⚡ SNIPER MEJORADO: El jugador YA recupera el control total de la cámara aquí,
            // pero esperamos el delay antes de permitirle disparar otra vez.
            float currentAdvancedDelay = hasStockMod ? stockAdvancedDelay : baseAdvancedDelay;
            yield return new WaitForSeconds(currentAdvancedDelay);

            // Permitimos disparar de nuevo
            canShoot = true;
        }
        else
        {
            // 🔹 SNIPER BASE: Mantiene la animación de recarga con bloqueo total (como tenías)
            isShootingOrReloading = true; // Bloquea rotación durante la animación de cerrojo del base
            StopAiming();

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(reloadAnimTrigger);
            }

            float currentReloadTime = hasStockMod ? 1.0f : reloadDuration;
            yield return new WaitForSeconds(currentReloadTime);

            isShootingOrReloading = false;
            canShoot = true;

            if (aimAction != null && aimAction.action.IsPressed())
            {
                StartAiming();
            }
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

        bool inputPressed = slowTimeAction != null && slowTimeAction.action.IsPressed();
        bool canUseClock = isAiming && !isShootingOrReloading && inputPressed && currentMagicClockTime > 0f;

        if (canUseClock)
        {
            isUsingMagicClock = true;
            clockWasUsedInRace = true;
            currentMagicClockTime -= Time.unscaledDeltaTime;
            Time.timeScale = 0.3f;
        }
        else
        {
            isUsingMagicClock = false;
            if (!canWithClockCheck() && Time.timeScale < 1f)
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

    private bool canWithClockCheck() => false; // Seguridad interna para la línea de escala de tiempo

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (WeaponShopManager.Instance != null)
        {
            WeaponShopManager.Instance.SaveAmmoAfterRace(currentAmmo);
        }
        if (BlackMarketManager.Instance != null)
        {
            BlackMarketManager.Instance.NotifyRaceFinished(clockWasUsedInRace);
        }
    }
}