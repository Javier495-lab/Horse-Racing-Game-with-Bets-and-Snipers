using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HorseUIIdentifier : MonoBehaviour
{
    [Header("Referencias del Caballo")]
    public HorseRunner horseRunner;

    [Header("Configuración de Offset (Altura del Identificador)")]
    [Tooltip("Offset aplicado en carreras estándar.")]
    public Vector3 standardOffset = new Vector3(0f, 2.5f, 0f);
    [Tooltip("Offset aplicado si se compró la Entrada VIP (vistas desde las gradas).")]
    public Vector3 vipOffset = new Vector3(0f, 4.0f, 0f);
    private Vector3 activeOffset;

    [Header("Elementos de UI")]
    public CanvasGroup canvasGroup;
    public Image horsePortraitImage;      // Imagen principal del caballo
    public TextMeshProUGUI horseNameText;  // Nombre del caballo
    public TextMeshProUGUI rankText;       // Muestra la posición actual (ej: "1º", "2º", "OUT")

    [Header("Iconos de Estado")]
    public GameObject statusIconRoot;     // Contenedor opcional de estado
    public Image statusImage;             // Componente Image para mostrar el icono
    public Sprite fallenSprite;           // 💥 Sprite para caída
    public Sprite shotSprite;             // 🎯 Sprite para disparo

    private Transform mainCameraTransform;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (horseRunner == null)
            horseRunner = GetComponentInParent<HorseRunner>();

        if (statusIconRoot != null) statusIconRoot.SetActive(false);

        // 🎟️ Comprobar si el jugador compró la Entrada VIP para aplicar su offset correspondiente
        bool isVipActive = BlackMarketManager.Instance != null && BlackMarketManager.Instance.hasActiveVipTicket;
        activeOffset = isVipActive ? vipOffset : standardOffset;

        UpdateStaticHorseData();
    }

    private void OnEnable()
    {
        //SniperSystem.OnAimStateChanged += HandleAimState;
    }

    private void OnDisable()
    {
        //SniperSystem.OnAimStateChanged -= HandleAimState;
    }

    private void UpdateStaticHorseData()
    {
        if (horseRunner == null) return;

        if (horseNameText != null)
            horseNameText.text = horseRunner.horseName;

        if (horsePortraitImage != null && horseRunner.horsePortrait != null)
            horsePortraitImage.sprite = horseRunner.horsePortrait;
    }

    private void LateUpdate()
    {
        if (horseRunner == null) return;

        // 1. Posicionar y orientar hacia la cámara usando el offset activo (Estándar o VIP)
        transform.position = horseRunner.transform.position + activeOffset;

        if (mainCameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);
        }

        // 2. Actualizar estado y posición
        UpdateStatusAndRank();
    }

    private void UpdateStatusAndRank()
    {
        // CASO 1: Caballo fuera de carrera por Disparo
        if (horseRunner.hasBeenShot)
        {
            ShowStatusIcon(shotSprite);
            if (rankText != null) rankText.text = "<color=#FF0000>¡DISPARADO!</color>";
            return;
        }

        // CASO 2: Caballo fuera de carrera por Caída
        if (horseRunner.hasFallen)
        {
            ShowStatusIcon(fallenSprite);
            if (rankText != null) rankText.text = "<color=#FF8800>CAÍDO</color>";
            return;
        }

        // CASO 3: En carrera normal
        if (statusIconRoot != null) statusIconRoot.SetActive(false);

        if (RaceManager.Instance != null && rankText != null)
        {
            int currentRank = RaceManager.Instance.GetLiveHorseRank(horseRunner);
            rankText.text = currentRank > 0 ? $"{currentRank}º" : "-";
        }
    }

    private void ShowStatusIcon(Sprite sprite)
    {
        if (statusImage != null && sprite != null)
        {
            statusImage.sprite = sprite;
            if (statusIconRoot != null) statusIconRoot.SetActive(true);
        }
    }

    /*private void HandleAimState(bool isAiming, bool hasIdentifierMod)
    {
        if (canvasGroup == null) return;

        if (isAiming)
        {
            // Ocultar si está apuntando, excepto si se posee el modificador Identificador
            canvasGroup.alpha = hasIdentifierMod ? 1f : 0f;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
    }*/
}
