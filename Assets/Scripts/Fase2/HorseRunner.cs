using UnityEngine;

public class HorseRunner : MonoBehaviour
{
    [Header("Estado del Caballo")]
    public string horseName;
    public int targetRank;
    public bool willFall;
    public float fallProgress;

    [Header("Parámetros de Movimiento")]
    public float baseSpeed = 12f;
    public float speedVariation = 2.5f;

    [Header("Estado en Tiempo Real")]
    public float distanceCovered = 0f;
    public bool isRunning = false;
    public bool hasFallen = false;
    public bool hasFinished = false;

    private float trackLength;
    private float perlinSeed;

    private void Awake()
    {
        perlinSeed = Random.Range(0f, 100f);
    }

    public void Initialize(string name, int targetRank, bool willFall, float fallProgress, float trackLength)
    {
        this.horseName = name;
        this.targetRank = targetRank;
        this.willFall = willFall;
        this.fallProgress = fallProgress;
        this.trackLength = trackLength;
    }

    public void StartRunning()
    {
        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || hasFallen || hasFinished) return;

        float normalizedProgress = distanceCovered / trackLength;

        // 1. Evento de caída pre-calculada
        if (willFall && normalizedProgress >= fallProgress)
        {
            TriggerFall();
            return;
        }

        // 2. Dinámica de movimiento con Ruido Perlin
        float noise = Mathf.PerlinNoise(Time.time * 0.8f, perlinSeed);
        float currentSpeed = baseSpeed + ((noise - 0.5f) * 2f * speedVariation);

        // 3. Ajuste de posición hacia la meta
        if (normalizedProgress > 0.65f)
        {
            float rankModifier = (8 - targetRank) * 0.35f;
            currentSpeed += rankModifier;
        }

        // 4. Avance
        float moveDistance = currentSpeed * Time.deltaTime;
        distanceCovered += moveDistance;
        transform.position += transform.forward * moveDistance;

        // 5. Cruzar meta
        if (distanceCovered >= trackLength)
        {
            hasFinished = true;
            isRunning = false;
            RaceManager.Instance.OnHorseFinished(this);
        }
    }

    public void TriggerFall()
    {
        hasFallen = true;
        isRunning = false;
        Debug.Log($"💥 ¡El caballo '{horseName}' ha sufrido una caída y queda eliminado!");
    }

    /// <summary>
    /// Punto de entrada futuro para el sistema Francotirador
    /// </summary>
    public void OnSniperHit()
    {
        if (hasFallen || hasFinished) return;
        Debug.Log($"🎯 ¡Jinete de '{horseName}' alcanzado por un disparo!");
        TriggerFall();
    }
}
