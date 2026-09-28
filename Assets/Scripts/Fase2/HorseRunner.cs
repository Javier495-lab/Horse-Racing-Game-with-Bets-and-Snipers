using UnityEngine;
using UnityEngine.Splines;

public class HorseRunner : MonoBehaviour
{
    [Header("Datos del Caballo")]
    public string horseName = "Caballo";
    public float baseWinProbability = 0.125f;
    public float winProbabilityModifier = 0f;
    public float fallProbabilityModifier = 0f;

    [Header("Parámetros de Movimiento")]
    public float baseSpeed = 10f;
    public float speedVariationRange = 2f;
    public float currentSpeed;

    [Header("Configuración de Pista (Spline u Opcional)")]
    public SplineContainer laneSpline; // 👈 El Spline de su carril
    private float distanceTraveled = 0f; // Distancia recorrida en metros

    [Header("Mecánica de Caída")]
    [Range(0f, 100f)] public float baseFallChancePercent = 5f;
    public float fallCheckInterval = 2.0f;
    private float fallTimer = 0f;

    [Header("Estado")]
    public bool isRunning = false;
    public bool hasFinished = false;
    public bool hasFallen = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetupHorseData(HorseUIElement horseData)
    {
        if (horseData == null) return;
        horseName = horseData.horseName;
        baseWinProbability = horseData.winProbability;
        winProbabilityModifier = horseData.winProbabilityModifier;
        fallProbabilityModifier = horseData.fallProbabilityModifier;
    }

    public void ResetRunner()
    {
        isRunning = false;
        hasFinished = false;
        hasFallen = false;
        distanceTraveled = 0f;
        currentSpeed = 0f;
        fallTimer = 0f;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.Rebind(); // Vuelve la animación al estado inicial
        }
    }

    public void StartRunning()
    {
        if (hasFallen) return;

        isRunning = true;
        hasFinished = false;

        if (animator != null) animator.SetBool("IsRunning", true);
    }

    private void Update()
    {
        if (!isRunning || hasFinished || hasFallen) return;

        // 1. Calcular velocidad con la lógica de probabilidad/modificadores
        CalculateSpeed();

        // 2. Mover al caballo
        if (laneSpline != null)
        {
            // === MODO SPLINE (Pistas con curvas) ===
            MoveAlongSpline();
        }
        else
        {
            // === MODO RECTA SIMPLE (Si no hay Spline asignado) ===
            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        }

        // 3. Comprobar caídas
        HandleFallCheck();
    }

    private void MoveAlongSpline()
    {
        float splineLength = laneSpline.CalculateLength();

        // Sumar la distancia avanzada en este frame
        distanceTraveled += currentSpeed * Time.deltaTime;

        // Convertir la distancia a un porcentaje (0.0 = Salida, 1.0 = Meta)
        float progress = Mathf.Clamp01(distanceTraveled / splineLength);

        // Posición y dirección exactas en el Spline
        Vector3 newPosition = laneSpline.EvaluatePosition(progress);
        Vector3 direction = laneSpline.EvaluateTangent(progress);

        transform.position = newPosition;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Si llega al final del Spline (100%), cruza la meta automáticamente
        if (progress >= 1.0f && !hasFinished)
        {
            CrossFinishLine();
        }
    }

    private void CalculateSpeed()
    {
        float totalProb = Mathf.Max(0.01f, baseWinProbability + winProbabilityModifier);
        float probBonus = totalProb * 8f;
        float noise = Random.Range(-speedVariationRange, speedVariationRange);

        currentSpeed = baseSpeed + probBonus + noise;
    }

    private void HandleFallCheck()
    {
        fallTimer += Time.deltaTime;
        if (fallTimer >= fallCheckInterval)
        {
            fallTimer = 0f;
            float effectiveFallChance = Mathf.Clamp(baseFallChancePercent + fallProbabilityModifier, 0f, 100f);

            if (Random.Range(0f, 100f) < effectiveFallChance)
            {
                TriggerFall();
            }
        }
    }

    private void TriggerFall()
    {
        hasFallen = true;
        isRunning = false;
        currentSpeed = 0f;

        if (animator != null) animator.SetTrigger("Fall");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckRaceCompletion();
        }
    }

    private void CrossFinishLine()
    {
        hasFinished = true;
        isRunning = false;

        if (animator != null) animator.SetBool("IsRunning", false);

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnHorseFinished(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FinishLine") && isRunning && !hasFinished)
        {
            CrossFinishLine();
        }
    }
}
