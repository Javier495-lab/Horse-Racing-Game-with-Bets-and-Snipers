using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using HorseRacing.CanvasUI;

public class MarketInteractable : MonoBehaviour, IInteractable
{
    private Outline outline;

    [Header("UI del Mercado Negro")]
    public GameObject marketUIPanel;

    [Header("Animaciones del Tendero / Puerta")]
    public Animator vendorAnimator;
    public string closeAnimTrigger1 = "CloseA";
    public string closeAnimTrigger2 = "CloseB";

    private void Start()
    {
        outline = GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
        if (marketUIPanel != null) marketUIPanel.SetActive(false);
    }

    public void OnHovered()
    {
        if (outline != null) outline.enabled = true;
    }

    public void OnUnhovered()
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnInteract()
    {
        if (marketUIPanel == null) return;

        bool isOpen = marketUIPanel.activeSelf;
        marketUIPanel.SetActive(!isOpen);

        // Si se acaba de cerrar la interfaz, se disparan las dos animaciones
        if (isOpen && vendorAnimator != null)
        {
            if (!string.IsNullOrEmpty(closeAnimTrigger1)) vendorAnimator.SetTrigger(closeAnimTrigger1);
            if (!string.IsNullOrEmpty(closeAnimTrigger2)) vendorAnimator.SetTrigger(closeAnimTrigger2);
            Debug.Log("[MarketInteractable] Panel cerrado. Animaciones del tendero ejecutadas.");
        }
    }
}
