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
    public bool hasAdvancedSniper = false;

    // 🎯 AIM BOT (Mercado Negro)
    [Header("--- AIM BOT ---")]
    public bool hasAimBot = false;
    private Transform lockedTargetHorseTransform = null; // Caballo al que está fijado el Aim Bot

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

    // Entrada del Input System
    private Vector2 lookInput;

    private HorseRunner currentLookedAtHorse = null;

    public delegate void AimStateChanged(bool isAiming, bool hasIdentifierMod);
    public static event AimStateChanged OnAimStateChanged;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        currentFOV = normalFOV;

        // Sincronizar mejoras y munición desde el WeaponShopManager
        if (WeaponShopManager.Instance != null)
        {
            WeaponShopManager.Instance.ApplyUpgradesToSniper(this);
        }
        else
        {
            currentAmmo = maxAmmo;
        }

        // 🪄 1. POLVO MÁGICO
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActivePolvoMagico)
        {
            maxBreathTime += BlackMarketManager.Instance.breathTimeIncrease;
        }

        currentBreath = maxBreathTime;

        // ⏱️ 2. RELOJ MÁGICO
        if (BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveMagicClock)
        {
            currentMagicClockTime = maxMagicClockTime;
        }

        // 🎯 3. AIM BOT (Comprobar si se compró en el mercado negro para esta carrera)
        if (BlackMarketManager.Instance != null)
        {
            string currentHouseKey = BlackMarketManager.Instance.GetCurrentHouseKey();
            hasAimBot = BlackMarketManager.Instance.GetAimBotOwned(currentHouseKey);
        }
        else
        {
            hasAimBot = false; // Por seguridad, si no hay manager se desactiva
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
        if (slowTimeAction != null) { slowTimeAction.action.started -= OnSlowTimeStarted; slowTimeAction.action.canceled -= OnSlowTimeCanceled; slowTimeAction.action.Disable(); }
    }

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
            lockedTargetHorseTransform = null; // Reiniciar lock al dejar de apuntar
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
            if (breathDepleted && currentBreath >= maxBreathTime) breathDepleted = false;
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
    // LÓGICA DE ROTACIÓN, AIM BOT SNAP Y RECOIL
    // ==========================================

    public float aimBotBreakThreshold = 3.0f; // Puedes subirlo o bajarlo desde el Inspector

    private void HandleRotation()
    {
        // 🎯 LÓGICA DE Z-TARGETING (AIM BOT)
        if (hasAimBot && isAiming && lockedTargetHorseTransform != null)
        {
            // Si el jugador mueve el ratón superando el umbral, rompemos el bloqueo
            if (lookInput.sqrMagnitude > aimBotBreakThreshold)
            {
                lockedTargetHorseTransform = null;
            }
            else
            {
                // 1. Posición del centro del caballo
                Vector3 targetPosition = lockedTargetHorseTransform.position + Vector3.up * 1.5f;
                Vector3 targetDirection = targetPosition - transform.position;

                if (targetDirection != Vector3.zero)
                {
                    // 2. Rotación hacia el caballo
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                    Vector3 targetEuler = targetRotation.eulerAngles;

                    // 3. Extraemos pitch y yaw del objetivo
                    float targetPitch = targetEuler.x > 180f ? targetEuler.x - 360f : targetEuler.x;
                    float targetYaw = targetEuler.y;

                    // 4. Suavizado hacia el objetivo
                    pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * 15f);
                    yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * 15f);

                    transform.eulerAngles = new Vector3(pitch, yaw, 0f);
                    return;
                }
            }
        }

        // --- CONTROL MANUAL Y SWAY (Funcionamiento normal) ---
        yaw += lookInput.x * mouseSensitivity;

        // Invertimos el signo de lookInput.y para corregir el eje vertical invertido
        pitch -= lookInput.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, initialPitch - activePitchLimit, initialPitch + activePitchLimit);
        yaw = Mathf.Clamp(yaw, initialYaw - activeYawLimit, initialYaw + activeYawLimit);

        float finalPitchVal = pitch + (swayOffset.x * currentSwayWeight) + currentRecoilOffset;
        float finalYawVal = yaw + (swayOffset.y * currentSwayWeight);

        transform.eulerAngles = new Vector3(finalPitchVal, finalYawVal, 0f);
    }

    private void HandleSway()
    {
        // 1. Gestionar la supresión del sway según el input de movimiento
        float targetInputFactor = 1f;
        if (enableInputSwaySuppression)
        {
            // Si el input de look supera el umbral, bajamos el factor del sway a 0 (lo apagamos)
            if (lookInput.sqrMagnitude > inputSwayThreshold)
            {
                targetInputFactor = 0f;
            }
        }
        // Transición suave para que no sea un corte brusco
        currentInputSwayFactor = Mathf.MoveTowards(currentInputSwayFactor, targetInputFactor, inputSwayFadeSpeed * Time.deltaTime);

        if (isAiming)
        {
            swayTimer += Time.deltaTime * swaySpeed;

            // Intensidad base (teniendo en cuenta si hay bípode o no)
            float baseIntensity = hasBipodMod ? (swayIntensity * bipodSwayMultiplier) : swayIntensity;

            // Multiplicamos por el peso de la respiración Y por el factor de input de movimiento
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

    // ==========================================
    // DETECCIÓN DE CABALLO Y GESTIÓN DE AIM BOT
    // ==========================================

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

                // 🎯 Si el Aim Bot está activo y aún no tenemos un objetivo fijado, clavamos la mira en este caballo
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

    // ==========================================
    // RUTINA DE DISPARO
    // ==========================================

    private IEnumerator ShootRoutine()
    {
        isShootingOrReloading = true;
        currentAmmo--;

        isUsingMagicClock = false;
        Time.timeScale = 1f;

        PerformRaycastShoot();

        float effectiveRecoil = hasBipodMod ? (baseRecoilAngle * 0.35f) : baseRecoilAngle;
        targetRecoilOffset = -effectiveRecoil;
        isRecounding = true;

        yield return new WaitForSeconds(recoilDuration);

        if (hasAdvancedSniper)
        {
            isShootingOrReloading = false;
        }
        else
        {
            StopAiming();

            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(reloadAnimTrigger);
            }

            // 💡 MODIFICACIÓN STOCK (CULATA): Si tiene el stock mod, la recarga dura exactamente 1 segundo.
            float currentReloadTime = hasStockMod ? 1.0f : reloadDuration;
            yield return new WaitForSeconds(currentReloadTime);

            isShootingOrReloading = false;

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

    // ==========================================
    // LÓGICA DEL RELOJ MÁGICO
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