using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Maneja el final del juego y determina el ganador
/// ESTE SCRIPT VA EN VR
/// </summary>
public class GameEndManager : MonoBehaviour
{
    [Header("Referencias VR")]
    [SerializeField] private VRWebSocketServer server;
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private ScoreManager scoreManager;  // ← NUEVO
    
    [Header("UI VR")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    
    [Header("Configuración")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Textos de Victoria")]
    [SerializeField] private string vrWinText = "🐷 ¡VR GANA!";
    [SerializeField] private string tabletWinText = "📱 ¡TABLET GANA!";
    [SerializeField] private string tieText = "🤝 ¡EMPATE!";
    
    [Header("Colores")]
    [SerializeField] private Color vrWinColor = new Color(1f, 0.3f, 0.3f); // Rojo
    [SerializeField] private Color tabletWinColor = new Color(0.2f, 0.8f, 1f); // Azul
    [SerializeField] private Color tieColor = Color.yellow;
    
    private bool gameEnded = false;
    
    void Start()
    {
        if (autoFindReferences)
        {
            if (server == null)
                server = FindObjectOfType<VRWebSocketServer>();
            
            if (levelTimer == null)
                levelTimer = FindObjectOfType<LevelTimer>();
            
            if (scoreManager == null)
                scoreManager = ScoreManager.instance;
        }
        
        // Ocultar panel de ganador al inicio
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }
        
        if (showDebugLogs)
            Debug.Log("[GameEndManager] Sistema de final de juego inicializado");
    }
    
    void Update()
    {
        // Detectar cuando el tiempo llegue a 0
        if (!gameEnded && levelTimer != null)
        {
            // Verificar si el timer llegó a 0 (aproximadamente)
            if (levelTimer.GetType().GetField("timeRemaining", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance) != null)
            {
                float timeRemaining = (float)levelTimer.GetType()
                    .GetField("timeRemaining", 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Instance)
                    .GetValue(levelTimer);
                
                if (timeRemaining <= 0.1f && !gameEnded)
                {
                    EndGame();
                }
            }
        }
    }
    
    /// <summary>
    /// Llama a este método cuando el tiempo se acabe
    /// </summary>
    public void EndGame()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        
        if (showDebugLogs)
            Debug.Log("[GameEndManager] ¡Juego terminado! Determinando ganador...");
        
        // Obtener puntaje del jugador VR (cerdos eliminados)
        int vrScore = 0;
        if (scoreManager != null)
        {
            vrScore = scoreManager.GetCurrentScore();
        }
        else if (ScoreManager.instance != null)
        {
            vrScore = ScoreManager.instance.GetCurrentScore();
        }
        
        // Obtener puntaje del jugador Tablet (Chef o Soldado)
        int tabletScore = 0;
        GameMode tabletMode = GameModeManager.CurrentMode;
        
        if (GameScoreSystem.Instance != null)
        {
            if (tabletMode == GameMode.Chef)
            {
                tabletScore = GameScoreSystem.Instance.GetChefScore();
            }
            else if (tabletMode == GameMode.Soldado)
            {
                tabletScore = GameScoreSystem.Instance.GetSoldadoScore();
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[GameEndManager] Puntajes finales - VR: {vrScore}, Tablet ({tabletMode}): {tabletScore}");
        
        // Determinar ganador
        string winner = DetermineWinner(vrScore, tabletScore);
        
        // Mostrar resultado en VR
        ShowWinnerInVR(winner, vrScore, tabletScore);
        
        // Enviar resultado a Tablet
        SendWinnerToTablet(winner, vrScore, tabletScore);
    }
    
    /// <summary>
    /// Determina quién ganó: "VR" o "Tablet"
    /// </summary>
    private string DetermineWinner(int vrScore, int tabletScore)
    {
        if (vrScore > tabletScore)
        {
            return "VR";
        }
        else if (tabletScore > vrScore)
        {
            return "Tablet";
        }
        else
        {
            return "Tie"; // Empate
        }
    }
    
    /// <summary>
    /// Muestra el ganador en VR
    /// </summary>
    private void ShowWinnerInVR(string winner, int vrScore, int tabletScore)
    {
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
        }
        
        if (winnerText != null)
        {
            switch (winner)
            {
                case "VR":
                    winnerText.text = vrWinText;
                    winnerText.color = vrWinColor;
                    break;
                
                case "Tablet":
                    winnerText.text = tabletWinText;
                    winnerText.color = tabletWinColor;
                    break;
                
                case "Tie":
                    winnerText.text = tieText;
                    winnerText.color = tieColor;
                    break;
            }
            
            if (showDebugLogs)
                Debug.Log($"[GameEndManager] Mostrando ganador en VR: {winnerText.text}");
        }
        
        if (finalScoreText != null)
        {
            finalScoreText.text = $"VR: {vrScore} pts\nTablet: {tabletScore} pts";
        }
        
        // Opcional: Animar el texto
        if (winnerText != null)
        {
            StartCoroutine(AnimateWinnerText());
        }
    }
    
    /// <summary>
    /// Envía el resultado del ganador a la Tablet
    /// </summary>
    private void SendWinnerToTablet(string winner, int vrScore, int tabletScore)
    {
        if (server == null || !server.IsClientConnected)
        {
            if (showDebugLogs)
                Debug.LogWarning("[GameEndManager] No hay tablet conectada");
            return;
        }
        
        try
        {
            // Crear mensaje de ganador
            WinnerMessage message = new WinnerMessage
            {
                type = "winner",
                winner = winner,  // "VR", "Tablet", o "Tie"
                vrScore = vrScore,
                tabletScore = tabletScore
            };
            
            string json = JsonUtility.ToJson(message);
            
            if (showDebugLogs)
                Debug.Log($"[GameEndManager] Enviando ganador a tablet: {json}");
            
            server.SendMessageToClient(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameEndManager] Error enviando ganador: {e.Message}");
        }
    }
    
    /// <summary>
    /// Animación del texto de ganador
    /// </summary>
    private IEnumerator AnimateWinnerText()
    {
        Vector3 originalScale = winnerText.transform.localScale;
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Crecer
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / duration);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // Pequeño rebote
        elapsed = 0f;
        duration = 0.2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsed / duration);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        winnerText.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Resetea el sistema para una nueva partida
    /// </summary>
    public void ResetGame()
    {
        gameEnded = false;
        
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }
        
        if (showDebugLogs)
            Debug.Log("[GameEndManager] Sistema reseteado para nueva partida");
    }
}

/// <summary>
/// Mensaje que se envía a la tablet cuando hay un ganador
/// </summary>
[System.Serializable]
public class WinnerMessage
{
    public string type;        // "winner"
    public string winner;      // "VR", "Tablet", o "Tie"
    public int vrScore;        // Puntaje del jugador VR (cerdos)
    public int tabletScore;    // Puntaje del jugador Tablet (Chef o Soldado)
}