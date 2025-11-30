using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Muestra el ganador en la pantalla de la Tablet
/// ESTE SCRIPT VA EN TABLET
/// </summary>
public class TabletWinnerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI yourScoreText;
    
    [Header("Textos de Victoria")]
    [SerializeField] private string youWinText = "🎉 ¡GANASTE!";
    [SerializeField] private string youLoseText = "😔 PERDISTE";
    [SerializeField] private string tieText = "🤝 ¡EMPATE!";
    
    [Header("Colores")]
    [SerializeField] private Color winColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color loseColor = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color tieColor = Color.yellow;
    
    [Header("Configuración")]
    [SerializeField] private bool showDebugLogs = true;
    
    void Start()
    {
        // Ocultar panel al inicio
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }
        
        if (showDebugLogs)
            Debug.Log("[TabletWinnerDisplay] Sistema de ganador inicializado");
    }
    
    /// <summary>
    /// Muestra el resultado del juego
    /// Llamar esto desde TabletConnectionManager cuando reciba un mensaje de "winner"
    /// </summary>
    public void ShowWinner(WinnerMessage message)
    {
        if (message == null) return;
        
        if (showDebugLogs)
            Debug.Log($"[TabletWinnerDisplay] Ganador recibido: {message.winner}");
        
        // Determinar si el jugador de Tablet ganó
        bool tabletWon = (message.winner == "Tablet");
        bool isTie = (message.winner == "Tie");
        
        // Mostrar panel
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(true);
        }
        
        // Actualizar texto principal
        if (winnerText != null)
        {
            if (isTie)
            {
                winnerText.text = tieText;
                winnerText.color = tieColor;
            }
            else if (tabletWon)
            {
                winnerText.text = youWinText;
                winnerText.color = winColor;
            }
            else
            {
                winnerText.text = youLoseText;
                winnerText.color = loseColor;
            }
            
            // Animar texto
            StartCoroutine(AnimateWinnerText());
        }
        
        // Mostrar puntajes finales
        if (finalScoreText != null)
        {
            finalScoreText.text = $"VR: {message.vrScore} pts\nTablet: {message.tabletScore} pts";
        }
        
        // Mostrar puntaje del jugador tablet
        if (yourScoreText != null)
        {
            yourScoreText.text = $"Tu puntaje: {message.tabletScore}";
        }
    }
    
    /// <summary>
    /// Animación del texto de ganador
    /// </summary>
    private IEnumerator AnimateWinnerText()
    {
        if (winnerText == null) yield break;
        
        Vector3 originalScale = winnerText.transform.localScale;
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Crecer desde 0
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.3f, elapsed / duration);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // Rebote
        elapsed = 0f;
        duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.3f, 1f, elapsed / duration);
            winnerText.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // Pulsar ligeramente (loop infinito)
        StartCoroutine(PulseAnimation());
    }
    
    /// <summary>
    /// Animación de pulso continuo
    /// </summary>
    private IEnumerator PulseAnimation()
    {
        if (winnerText == null) yield break;
        
        Vector3 originalScale = winnerText.transform.localScale;
        
        while (winnerPanel != null && winnerPanel.activeSelf)
        {
            float elapsed = 0f;
            float duration = 1f;
            
            // Crecer
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.1f, elapsed / (duration / 2));
                winnerText.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            // Encoger
            elapsed = 0f;
            while (elapsed < duration / 2)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.1f, 1f, elapsed / (duration / 2));
                winnerText.transform.localScale = originalScale * scale;
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// Cierra el panel de ganador
    /// </summary>
    public void CloseWinnerPanel()
    {
        if (winnerPanel != null)
        {
            winnerPanel.SetActive(false);
        }
        
        StopAllCoroutines();
        
        if (showDebugLogs)
            Debug.Log("[TabletWinnerDisplay] Panel de ganador cerrado");
    }
}