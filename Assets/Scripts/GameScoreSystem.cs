using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Sistema de puntuación SOLO para enviar al jugador de la tablet
/// VR NO muestra puntos, solo los envía
/// </summary>
public class GameScoreSystem : MonoBehaviour
{
    [Header("Referencias VR")]
    [SerializeField] private VRWebSocketServer server;
    
    [Header("Configuración")]
    [SerializeField] private bool autoFindServer = true;
    [SerializeField] private bool showDebugLogs = true;
    
    // Puntuaciones por modo (SOLO para enviar a tablet)
    private int chefScore = 0;  // Puntos del Chef (bloqueando proyectiles)
    private int soldadoScore = 0; // Puntos del Soldado (matando pájaros)
    
    // Singleton
    public static GameScoreSystem Instance { get; private set; }
    
    // Eventos (por si acaso los necesitas)
    public static event Action<int> OnChefScoreChanged;
    public static event Action<int> OnSoldadoScoreChanged;
    
    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (showDebugLogs)
            Debug.Log("[ScoreSystem] Sistema de puntuación inicializado");
    }
    
    void Start()
    {
        // Buscar servidor automáticamente
        if (autoFindServer && server == null)
        {
            server = FindObjectOfType<VRWebSocketServer>();
            
            if (server != null && showDebugLogs)
                Debug.Log("[ScoreSystem] ✓ VRWebSocketServer encontrado automáticamente");
        }
        
        UpdateScoreDisplay();
    }
    
    /// <summary>
    /// Añade puntos al Chef (bloquear proyectiles)
    /// </summary>
    public void AddChefScore(int points)
    {
        chefScore += points;
        
        if (showDebugLogs)
            Debug.Log($"[ScoreSystem] 🍳 Chef +{points} puntos! Total: {chefScore}");
        
        OnChefScoreChanged?.Invoke(chefScore);
        UpdateScoreDisplay();
        SendScoreToTablet();
    }
    
    /// <summary>
    /// Añade puntos al Soldado (matar pájaros)
    /// </summary>
    public void AddSoldadoScore(int points)
    {
        soldadoScore += points;
        
        if (showDebugLogs)
            Debug.Log($"[ScoreSystem] 🎯 Soldado +{points} puntos! Total: {soldadoScore}");
        
        OnSoldadoScoreChanged?.Invoke(soldadoScore);
        UpdateScoreDisplay();
        SendScoreToTablet();
    }
    
    /// <summary>
    /// Obtiene el puntaje del modo actual
    /// </summary>
    public int GetCurrentScore()
    {
        return GameModeManager.IsChefMode ? chefScore : soldadoScore;
    }
    
    /// <summary>
    /// Obtiene el puntaje del Chef
    /// </summary>
    public int GetChefScore()
    {
        return chefScore;
    }
    
    /// <summary>
    /// Obtiene el puntaje del Soldado
    /// </summary>
    public int GetSoldadoScore()
    {
        return soldadoScore;
    }
    
    /// <summary>
    /// Resetea el puntaje del modo actual
    /// </summary>
    public void ResetCurrentScore()
    {
        if (GameModeManager.IsChefMode)
        {
            ResetChefScore();
        }
        else if (GameModeManager.IsSoldadoMode)
        {
            ResetSoldadoScore();
        }
    }
    
    /// <summary>
    /// Resetea el puntaje del Chef
    /// </summary>
    public void ResetChefScore()
    {
        chefScore = 0;
        
        if (showDebugLogs)
            Debug.Log("[ScoreSystem] 🍳 Puntaje Chef reseteado");
        
        OnChefScoreChanged?.Invoke(chefScore);
        UpdateScoreDisplay();
        SendScoreToTablet();
    }
    
    /// <summary>
    /// Resetea el puntaje del Soldado
    /// </summary>
    public void ResetSoldadoScore()
    {
        soldadoScore = 0;
        
        if (showDebugLogs)
            Debug.Log("[ScoreSystem] 🎯 Puntaje Soldado reseteado");
        
        OnSoldadoScoreChanged?.Invoke(soldadoScore);
        UpdateScoreDisplay();
        SendScoreToTablet();
    }
    
    /// <summary>
    /// Resetea ambos puntajes
    /// </summary>
    public void ResetAllScores()
    {
        chefScore = 0;
        soldadoScore = 0;
        
        if (showDebugLogs)
            Debug.Log("[ScoreSystem] Todos los puntajes reseteados");
        
        OnChefScoreChanged?.Invoke(chefScore);
        OnSoldadoScoreChanged?.Invoke(soldadoScore);
        UpdateScoreDisplay();
        SendScoreToTablet();
    }
    
    /// <summary>
    /// NO SE USA - VR no muestra puntos
    /// Los puntos solo se envían a la tablet
    /// </summary>
    private void UpdateScoreDisplay()
    {
        // VR no muestra puntos, solo los envía a la tablet
        // Este método ya no hace nada
    }
    
    /// <summary>
    /// Envía el puntaje actual a la tablet
    /// </summary>
    private void SendScoreToTablet()
    {
        if (server == null || !server.IsClientConnected)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ScoreSystem] No hay tablet conectada para enviar puntaje");
            return;
        }
        
        try
        {
            GameMode currentMode = GameModeManager.CurrentMode;
            int currentScore = GetCurrentScore();
            
            // Crear mensaje JSON
            ScoreMessage message = new ScoreMessage
            {
                type = "score",
                mode = currentMode.ToString(),
                chefScore = chefScore,
                soldadoScore = soldadoScore,
                currentScore = currentScore
            };
            
            string json = JsonUtility.ToJson(message);
            
            if (showDebugLogs)
                Debug.Log($"[ScoreSystem] Enviando puntaje a tablet: {json}");
            
            // Enviar a través del servidor
            server.SendMessageToClient(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScoreSystem] Error enviando puntaje: {e.Message}");
        }
    }
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

// NOTA: La clase ScoreMessage está en SharedMessages.cs
// No la definas aquí para evitar duplicados