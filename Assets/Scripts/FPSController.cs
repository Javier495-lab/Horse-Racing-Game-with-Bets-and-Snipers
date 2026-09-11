using UnityEngine;
using UnityEngine.InputSystem;

public class FPSController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float walkSpeed = 4.0f;
    [SerializeField] private float sprintSpeed = 7.0f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Configuración de Cámara y Mirada")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85.0f;
    [SerializeField] private float maxPitch = 85.0f;

    [Header("Efecto Cabeza / Head Bobbing")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobFrequency = 10f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobFrequency = 14f;
    [SerializeField] private float sprintBobAmount = 0.08f;

    [Header("Sistema de Interacción")]
    [Tooltip("Distancia máxima a la que el Raycast detecta interactuables")]
    [SerializeField] private float rayDistance = 3.0f;
    [SerializeField] private string interactableTag = "Interactuable";

    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;      // Vector2 (WASD)
    [SerializeField] private InputActionProperty lookAction;      // Vector2 (Mouse Delta)
    [SerializeField] private InputActionProperty sprintAction;    // Button (Shift)
    [SerializeField] private InputActionProperty interactAction;  // Button (E)

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private Vector3 defaultCameraLocalPos;
    private float bobTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            defaultCameraLocalPos = cameraTransform.localPosition;
        }
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
        lookAction.action?.Enable();
        sprintAction.action?.Enable();
        interactAction.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction.action?.Disable();
        lookAction.action?.Disable();
        sprintAction.action?.Disable();
        interactAction.action?.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool isControlEnabled = true;

    private void Update()
    {
        // Si los controles están desactivados, cancelamos todo el Update
        if (!isControlEnabled) return;

        HandleMovement();
        HandleLook();
        HandleHeadBob();
        HandleInteraction();
    }

    public void ToggleControls(bool enable)
    {
        isControlEnabled = enable;

        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            velocity = Vector3.zero;

            if (currentInteractable != null)
            {
                currentInteractable.OnUnhovered();
                currentInteractable = null;
            }
        }
    }

    private void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 inputMove = moveAction.action.ReadValue<Vector2>();
        bool isSprinting = sprintAction.action.IsPressed();

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 moveDir = transform.right * inputMove.x + transform.forward * inputMove.y;

        controller.Move(moveDir * currentSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        Vector2 inputMove = moveAction.action.ReadValue<Vector2>();
        bool isMoving = inputMove.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            bool isSprinting = sprintAction.action.IsPressed();
            float freq = isSprinting ? sprintBobFrequency : walkBobFrequency;
            float amount = isSprinting ? sprintBobAmount : walkBobAmount;

            bobTimer += Time.deltaTime * freq;
            float newY = defaultCameraLocalPos.y + Mathf.Sin(bobTimer) * amount;
            float newX = defaultCameraLocalPos.x + Mathf.Cos(bobTimer * 0.5f) * (amount * 0.5f);

            cameraTransform.localPosition = new Vector3(newX, newY, defaultCameraLocalPos.z);
        }
        else
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                defaultCameraLocalPos,
                Time.deltaTime * 8.0f
            );
        }
    }

    private IInteractable currentInteractable; // Guarda el objeto que estamos mirando actualmente

    private void HandleInteraction()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag(interactableTag))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    // Si cambiamos de un interactuable a otro directamente
                    if (currentInteractable != null && currentInteractable != interactable)
                    {
                        currentInteractable.OnUnhovered();
                    }

                    currentInteractable = interactable;

                    if (interactAction.action.WasPressedThisFrame())
                    {
                        currentInteractable.OnInteract();
                    }
                    else
                    {
                        currentInteractable.OnHovered();
                    }

                    return; // Salimos de la función porque estamos mirando a un objeto válido
                }
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnUnhovered(); // Avisamos al objeto de que dejamos de mirarlo
            currentInteractable = null;        // Limpiamos la referencia
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * rayDistance);
        }
    }
}
