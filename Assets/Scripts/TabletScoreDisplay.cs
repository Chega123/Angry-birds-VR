using UnityEngine;
using TMPro;

/// <summary>
/// Muestra los puntajes recibidos del VR en la tablet
/// </summary>
public class TabletScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI chefScoreText;
    [SerializeField] private TextMeshProUGUI soldadoScoreText;
    
    [Header("Formato")]
    [SerializeField] private string scoreFormat = "Score: {0}";
    [SerializeField] private string chefFormat = "🍳 Chef: {0}";
    [SerializeField] private string soldadoFormat = "🎯 Soldado: {0}";
    
    [Header("Colores")]
    [SerializeField] private Color chefColor = new Color(1f, 0.8f, 0.2f); // Amarillo/dorado
    [SerializeField] private Color soldadoColor = new Color(0.2f, 0.8f, 0.3f); // Verde
    
    private int currentChefScore = 0;
    private int currentSoldadoScore = 0;
    private GameMode currentMode = GameMode.None;
    
    void Start()
    {
        UpdateDisplay();
    }
    
    /// <summary>
    /// Actualiza el puntaje desde un mensaje recibido
    /// Llamar esto desde TabletConnectionManager cuando reciba un mensaje de score
    /// </summary>
    public void UpdateScore(ScoreMessage scoreMessage)
    {
        if (scoreMessage == null) return;
        
        currentChefScore = scoreMessage.chefScore;
        currentSoldadoScore = scoreMessage.soldadoScore;
        
        // Parsear el modo
        if (System.Enum.TryParse<GameMode>(scoreMessage.mode, out GameMode mode))
        {
            currentMode = mode;
        }
        
        Debug.Log($"[TabletScore] Puntaje actualizado - Chef: {currentChefScore}, Soldado: {currentSoldadoScore}, Modo: {currentMode}");
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Actualiza el puntaje desde valores individuales
    /// </summary>
    public void UpdateScore(int chefScore, int soldadoScore, GameMode mode)
    {
        currentChefScore = chefScore;
        currentSoldadoScore = soldadoScore;
        currentMode = mode;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Actualiza la visualización de los puntajes
    /// </summary>
    private void UpdateDisplay()
    {
        // Actualizar puntaje principal (el del modo actual)
        if (scoreText != null)
        {
            int currentScore = currentMode == GameMode.Chef ? currentChefScore : currentSoldadoScore;
            scoreText.text = string.Format(scoreFormat, currentScore);
            
            // Cambiar color según el modo
            if (currentMode == GameMode.Chef)
            {
                scoreText.color = chefColor;
            }
            else if (currentMode == GameMode.Soldado)
            {
                scoreText.color = soldadoColor;
            }
        }
        
        // Actualizar puntajes individuales
        if (chefScoreText != null)
        {
            chefScoreText.text = string.Format(chefFormat, currentChefScore);
            chefScoreText.color = chefColor;
        }
        
        if (soldadoScoreText != null)
        {
            soldadoScoreText.text = string.Format(soldadoFormat, currentSoldadoScore);
            soldadoScoreText.color = soldadoColor;
        }
    }
    
    /// <summary>
    /// Resetea los puntajes
    /// </summary>
    public void ResetScores()
    {
        currentChefScore = 0;
        currentSoldadoScore = 0;
        UpdateDisplay();
    }
    
    /// <summary>
    /// Obtiene el puntaje actual del modo activo
    /// </summary>
    public int GetCurrentScore()
    {
        return currentMode == GameMode.Chef ? currentChefScore : currentSoldadoScore;
    }
}

// NOTA: La clase ScoreMessage está definida en otro archivo (GameScoreSystem.cs)
// No la definas aquí para evitar duplicados