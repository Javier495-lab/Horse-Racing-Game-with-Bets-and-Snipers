using System;
using UnityEngine;
using UnityEngine.UI;

public class BetScreen : MonoBehaviour, IInteractable
{
    private Camera screenCamera;
    private Outline outline;
    [SerializeField] private FPSController player;
    public Button returnButton;
    void Start()
    {
        screenCamera = GetComponentInChildren<Camera>();
        outline = GetComponent<Outline>();
        screenCamera.enabled = false;
        outline.enabled = false;
    }

    public void OnHovered()
    {
        outline.enabled = true;
        Debug.Log("Mirando a la terminal de apuestas. Texto: [Pulsa E para apostar]");
    }

    public void OnUnhovered()
    {
        outline.enabled = false;
        Debug.Log("Dejando de mirar la terminal de apuestas.");
    }

    public void OnInteract()
    {
        if (player != null) player.ToggleControls(false);
        screenCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        outline.enabled = false;
    }
    public void OnReturn()
    {
        if (player != null) player.ToggleControls(true);
        screenCamera.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (player != null) player.ToggleControls(true);
    }
}
