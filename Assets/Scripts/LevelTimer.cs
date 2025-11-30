using UnityEngine;
using TMPro;

public class LevelTimer : MonoBehaviour
{
    [Header("Configuración del Tiempo")]
    public float levelDuration = 90;
    private float timeRemaining;
    private bool timerIsRunning = false;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    // Referencias
    [Header("Referencias")]
    public ScoreManager scoreManager;
    public GameEndManager gameEndManager;  // ← NUEVO

    void Start()
    {
        timeRemaining = levelDuration;
        DisplayTime(timeRemaining);
        
        // Buscar GameEndManager automáticamente si no está asignado
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<GameEndManager>();
            if (gameEndManager != null)
            {
                Debug.Log("[LevelTimer] GameEndManager encontrado automáticamente");
            }
        }
    }

    public void StartTimer()
    {
        timeRemaining = levelDuration;
        timerIsRunning = true;
        Debug.Log("¡Cronómetro iniciado/reiniciado!");
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("¡Tiempo agotado! El nivel ha terminado.");
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);

                // Mostrar efecto de puntaje final
                if (scoreManager != null)
                {
                    scoreManager.ShowFinalScoreEffect();
                }
                
                // ← NUEVO: Determinar y mostrar ganador
                if (gameEndManager != null)
                {
                    gameEndManager.EndGame();
                }
                else
                {
                    Debug.LogWarning("[LevelTimer] GameEndManager no encontrado!");
                }
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}