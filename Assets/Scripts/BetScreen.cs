using System;
using UnityEngine;
using UnityEngine.UI;

public class BetScreen : MonoBehaviour, IInteractable
{
    [Header("Referencias de la Pantalla")]
    private Camera screenCamera;
    private Outline outline;
    [SerializeField] private FPSController player;
    [SerializeField] private GameObject bettingCanvas;      // Canvas de esta terminal
    [SerializeField] private BettingHouseUI bettingHouseUI; // Script de lógica de esta casa

    [Header("UI Controls")]
    public Button returnButton;

    [Header("Estado de Bloqueo")]
    public bool isLocked = false;

    void Start()
    {
        screenCamera = GetComponentInChildren<Camera>();
        outline = GetComponent<Outline>();

        if (screenCamera != null) screenCamera.enabled = false;
        if (outline != null) outline.enabled = false;

        // Auto-registramos esta pantalla en el Manager Central
        if (GlobalBettingManager.Instance != null)
        {
            if (!GlobalBettingManager.Instance.allBetScreens.Contains(this))
            {
                GlobalBettingManager.Instance.allBetScreens.Add(this);
            }
        }

        // Asignar el botón de volver automáticamente
        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturn);
        }
    }

    /// <summary>
    /// Bloquea o desbloquea esta pantalla cuando se confirma una apuesta global.
    /// </summary>
    public void SetInteractionLocked(bool locked)
    {
        isLocked = locked;

        // Si se confirma la apuesta mientras el jugador tenía la pantalla abierta, la cerramos
        if (isLocked && screenCamera != null && screenCamera.enabled)
        {
            OnReturn();
        }
    }

    public void OnHovered()
    {
        // Si ya hay una apuesta confirmada, no mostramos el outline de interacción
        if (isLocked || (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet))
        {
            return;
        }

        if (outline != null) outline.enabled = true;
        Debug.Log("Mirando a la terminal de apuestas. Texto: [Pulsa E para apostar]");
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
        Debug.Log("Dejando de mirar la terminal de apuestas.");
    }

    public void OnInteract()
    {
        // Cancelar interacción si está bloqueada o ya se apostó en otra casa
        if (isLocked || (GlobalBettingManager.Instance != null && GlobalBettingManager.Instance.hasActiveBet))
        {
            Debug.Log("Las apuestas para esta carrera están cerradas.");
            return;
        }

        if (player != null) player.ToggleControls(false);

        if (screenCamera != null) screenCamera.enabled = true;
        if (bettingCanvas != null) bettingCanvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (outline != null) outline.enabled = false;
    }

    public void OnReturn()
    {
        if (screenCamera != null) screenCamera.enabled = false;
        if (bettingCanvas != null) bettingCanvas.SetActive(false);

        if (player != null) player.ToggleControls(true); // Reactiva movimiento, cámara y bloquea cursor
    }
}
