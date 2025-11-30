using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TabletConnectionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject gamePanel;
    
    [Header("Video Display")]
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private TMP_Text debugText;
    
    [Header("Game Mode Selection")]
    [SerializeField] private Button chefButton;
    [SerializeField] private Button artilleroButton;
    [SerializeField] private TMP_Text modeIndicatorText;

    [Header("Touch Feedback")]
    [SerializeField] private Image touchIndicator;
    [SerializeField] private float indicatorSize = 50f;
    [SerializeField] private Color touchColor = new Color(1f, 1f, 0f, 0.5f);

    [Header("Score Display")]
    [SerializeField] private TabletScoreDisplay scoreDisplay;
    [SerializeField] private TabletWinnerDisplay winnerDisplay;  // ← NUEVO


    [Header("Settings")]
    [SerializeField] private string defaultIP = "192.168.1.100";
    [SerializeField] private int defaultPort = 8080;
    [SerializeField] private int connectionTimeout = 10;

    private ClientWebSocket webSocket;
    private CancellationTokenSource cts;
    private bool isConnected = false;
    private bool isReceiving = false;
    
    private Texture2D receivedTexture;
    private int frameCount = 0;
    private float fpsTimer = 0f;
    private int totalFramesReceived = 0;
    
    private const string IP_PREF_KEY = "LastServerIP";
    private const string PORT_PREF_KEY = "LastServerPort";
    private const string MODE_PREF_KEY = "LastGameMode";
    
    void Start()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
        
        if (UnityMainThreadDispatcher.Instance == null)
        {
            GameObject dispatcher = new GameObject("UnityMainThreadDispatcher");
            dispatcher.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(dispatcher);
            Debug.Log("UnityMainThreadDispatcher creado automáticamente");
        }
        
        LoadLastConnection();
        LoadLastMode();
        SetupUI();
        SetupModeButtons();
        InitializeVideoDisplay();
        SetupTouchIndicator();
        
        connectionPanel.SetActive(true);
        gamePanel.SetActive(false);
        
        LogDebug("Sistema iniciado con nuevo Input System");
    }
    
    private void SetupModeButtons()
    {
        if (chefButton != null)
        {
            chefButton.onClick.AddListener(() => SelectModeAndConnect(GameMode.Chef));
        }
        
        if (artilleroButton != null)
        {
            artilleroButton.onClick.AddListener(() => SelectModeAndConnect(GameMode.Soldado));
        }
        
        UpdateModeUI();
    }

    private async void SelectModeAndConnect(GameMode mode)
    {
        // 1. Seleccionar el modo
        GameModeManager.CurrentMode = mode;
        
        // 2. Guardar preferencia
        PlayerPrefs.SetInt(MODE_PREF_KEY, (int)mode);
        PlayerPrefs.Save();
        
        LogDebug($"Modo seleccionado: {mode}");
        
        // 3. Actualizar UI
        UpdateModeUI();
        
        // 4. Obtener IP y Puerto
        string ip = ipInputField.text.Trim();
        string portStr = portInputField.text.Trim();
        
        // 5. Validar
        if (string.IsNullOrEmpty(ip))
        {
            UpdateStatusText("Por favor ingresa una IP válida", Color.red);
            return;
        }
        
        if (!int.TryParse(portStr, out int port))
        {
            UpdateStatusText("Puerto inválido", Color.red);
            return;
        }
        
        if (!System.Net.IPAddress.TryParse(ip, out _))
        {
            UpdateStatusText("Formato de IP inválido", Color.red);
            return;
        }
        
        // 6. Guardar IP y Puerto
        PlayerPrefs.SetString(IP_PREF_KEY, ip);
        PlayerPrefs.SetInt(PORT_PREF_KEY, port);
        PlayerPrefs.Save();
        
        // 7. Deshabilitar botones mientras conecta
        if (chefButton != null) chefButton.interactable = false;
        if (artilleroButton != null) artilleroButton.interactable = false;
        
        UpdateStatusText($"Conectando en modo {mode}...", Color.yellow);
        LogDebug($"Intentando conectar a {ip}:{port} en modo {mode}");
        
        // 8. Conectar
        bool success = await ConnectToServer(ip, port);
        
        if (success)
        {
            // 9. IMPORTANTE: Enviar el modo al VR
            await SendModeToServer(mode);
            
            UpdateStatusText("¡Conectado!", Color.green);
            LogDebug($"Conexión exitosa en modo {mode}");
            await Task.Delay(1000);
            
            // 10. Cambiar a pantalla de juego
            connectionPanel.SetActive(false);
            gamePanel.SetActive(true);
        }
        else
        {
            UpdateStatusText("Error de conexión", Color.red);
            
            // Re-habilitar botones
            if (chefButton != null) chefButton.interactable = true;
            if (artilleroButton != null) artilleroButton.interactable = true;
            
            LogDebug("Conexión fallida");
        }
    }
    
    // NUEVO: Enviar el modo al servidor VR
    private async Task SendModeToServer(GameMode mode)
    {
        try
        {
            string modeMessage = $"{{\"type\":\"mode\",\"mode\":\"{mode}\"}}";
            LogDebug($"Enviando modo al servidor: {modeMessage}");
            
            byte[] buffer = Encoding.UTF8.GetBytes(modeMessage);
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            
            LogDebug("✓ Modo enviado al servidor");
            
            // Pequeño delay para que el servidor procese el modo
            await Task.Delay(100);
        }
        catch (Exception e)
        {
            LogDebug($"Error enviando modo: {e.Message}");
        }
    }
    
    private void UpdateModeUI()
    {
        GameMode currentMode = GameModeManager.CurrentMode;
        
        // Resaltar botón seleccionado
        if (chefButton != null)
        {
            ColorBlock colors = chefButton.colors;
            colors.normalColor = currentMode == GameMode.Chef ? Color.green : Color.white;
            chefButton.colors = colors;
        }
        
        if (artilleroButton != null)
        {
            ColorBlock colors = artilleroButton.colors;
            colors.normalColor = currentMode == GameMode.Soldado ? Color.green : Color.white;
            artilleroButton.colors = colors;
        }
        
        // Actualizar indicador de texto (opcional)
        if (modeIndicatorText != null)
        {
            if (currentMode == GameMode.None)
            {
                modeIndicatorText.text = "Selecciona un modo";
                modeIndicatorText.color = Color.yellow;
            }
            else
            {
                modeIndicatorText.text = $"Modo: {currentMode}";
                modeIndicatorText.color = Color.green;
            }
        }
    }
    
    private void LoadLastMode()
    {
        int savedMode = PlayerPrefs.GetInt(MODE_PREF_KEY, (int)GameMode.Chef);
        GameModeManager.CurrentMode = (GameMode)savedMode;
        
        LogDebug($"Último modo cargado: {GameModeManager.CurrentMode}");
    }

    private void SetupTouchIndicator()
    {
        if (touchIndicator != null)
        {
            touchIndicator.gameObject.SetActive(false);
            touchIndicator.color = touchColor;
            touchIndicator.rectTransform.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        }
    }
    
    private void InitializeVideoDisplay()
    {
        receivedTexture = new Texture2D(2, 2);
        
        if (videoDisplay != null)
        {
            videoDisplay.texture = receivedTexture;
            videoDisplay.GetComponent<AspectRatioFitter>()?.gameObject.SetActive(false);
        }
    }
    
    private void LoadLastConnection()
    {
        string lastIP = PlayerPrefs.GetString(IP_PREF_KEY, defaultIP);
        int lastPort = PlayerPrefs.GetInt(PORT_PREF_KEY, defaultPort);
        
        ipInputField.text = lastIP;
        portInputField.text = lastPort.ToString();
    }
    
    private void SetupUI()
    {
        UpdateStatusText("Selecciona un modo para conectar", Color.white);
    }
    
    private void LogDebug(string message)
    {
        Debug.Log($"[Tablet] {message}");
        if (debugText != null)
        {
            debugText.text = $"{System.DateTime.Now:HH:mm:ss} - {message}\n{debugText.text}";
            string[] lines = debugText.text.Split('\n');
            if (lines.Length > 10)
            {
                debugText.text = string.Join("\n", lines, 0, 10);
            }
        }
    }
    
    private async Task<bool> ConnectToServer(string ip, int port)
    {
        try
        {
            if (webSocket != null)
            {
                try { webSocket.Dispose(); } catch { }
                webSocket = null;
            }
            
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
            
            webSocket = new ClientWebSocket();
            cts = new CancellationTokenSource();
            
            webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            
            Uri serverUri = new Uri($"ws://{ip}:{port}");
            LogDebug($"URI: {serverUri}");
            
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(connectionTimeout));
            var connectTask = webSocket.ConnectAsync(serverUri, cts.Token);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                LogDebug($"Timeout después de {connectionTimeout}s");
                webSocket.Abort();
                return false;
            }
            
            await connectTask;
            
            if (webSocket.State == WebSocketState.Open)
            {
                LogDebug($"WebSocket abierto: {webSocket.State}");
                isConnected = true;
                
                _ = ReceiveMessages(cts.Token);
                
                return true;
            }
            else
            {
                LogDebug($"Estado inesperado: {webSocket.State}");
                return false;
            }
        }
        catch (Exception e)
        {
            LogDebug($"Excepción: {e.GetType().Name}");
            LogDebug($"Mensaje: {e.Message}");
            Debug.LogError($"Error completo: {e}");
            return false;
        }
    }
    
    private void UpdateStatusText(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
    
    void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            LogDebug($"Estado: isConnected={isConnected}, Touches={Touch.activeTouches.Count}, Mode={GameModeManager.CurrentMode}");
        }
        
        if (isConnected)
        {
            HandleTouchInput();
            UpdateFPS();
        }
    }
    
    private void UpdateFPS()
    {
        if (fpsText != null)
        {
            fpsTimer += Time.deltaTime;
            frameCount++;
            
            if (fpsTimer >= 1f)
            {
                float fps = frameCount / fpsTimer;
                fpsText.text = $"FPS: {fps:F1} | Total: {totalFramesReceived} | Modo: {GameModeManager.CurrentMode}";
                frameCount = 0;
                fpsTimer = 0f;
            }
        }
    }
    
    private void HandleTouchInput()
    {
        if (Touch.activeTouches.Count > 0)
        {
            Touch touch = Touch.activeTouches[0];
            
            LogDebug($"Touch detectado: Pos={touch.screenPosition}, Phase={touch.phase}");
            
            bool isOverVideo = IsTouchOverVideoDisplay(touch.screenPosition);
            LogDebug($"Touch sobre video: {isOverVideo}");
            
            if (isOverVideo)
            {
                ShowTouchIndicator(touch.screenPosition, touch.phase);
                
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    videoDisplay.rectTransform,
                    touch.screenPosition,
                    null,
                    out localPoint
                );
                
                Rect rect = videoDisplay.rectTransform.rect;
                float normalizedX = (localPoint.x - rect.xMin) / rect.width;
                float normalizedY = (localPoint.y - rect.yMin) / rect.height;
                
                normalizedX = Mathf.Clamp01(normalizedX);
                normalizedY = Mathf.Clamp01(normalizedY);
                
                LogDebug($"Coordenadas normalizadas: ({normalizedX:F3}, {normalizedY:F3})");
                
                TabletInput input = new TabletInput
                {
                    screenX = normalizedX,
                    screenY = normalizedY,
                    touchId = touch.touchId,
                    action = ConvertTouchPhaseToAction(touch.phase)
                };
                
                string json = JsonUtility.ToJson(input);
                LogDebug($"Enviando al servidor: {json}");
                SendToServer(json);
            }
        }
        else
        {
            if (touchIndicator != null && touchIndicator.gameObject.activeSelf)
            {
                touchIndicator.gameObject.SetActive(false);
            }
        }
    }
    
    private string ConvertTouchPhaseToAction(UnityEngine.InputSystem.TouchPhase phase)
    {
        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                return "Began";
            case UnityEngine.InputSystem.TouchPhase.Moved:
                return "Moved";
            case UnityEngine.InputSystem.TouchPhase.Ended:
                return "Ended";
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                return "Canceled";
            default:
                return "Moved";
        }
    }
    
    private void ShowTouchIndicator(Vector2 position, UnityEngine.InputSystem.TouchPhase phase)
    {
        if (touchIndicator == null) return;
        
        touchIndicator.gameObject.SetActive(true);
        touchIndicator.rectTransform.position = position;
        
        float size = indicatorSize;
        if (phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            size = indicatorSize * 1.5f;
        }
        else if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
        {
            size = indicatorSize * 0.5f;
        }
        
        touchIndicator.rectTransform.sizeDelta = new Vector2(size, size);
    }
    
    private bool IsTouchOverVideoDisplay(Vector2 position)
    {
        if (videoDisplay == null) return false;
        
        RectTransform rt = videoDisplay.rectTransform;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, position, null);
    }
    
    private async void SendToServer(string message)
    {
        if (webSocket?.State != WebSocketState.Open)
        {
            LogDebug($"No se puede enviar, estado: {webSocket?.State}");
            return;
        }
        
        try
        {
            LogDebug($"Intentando enviar mensaje...");
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            LogDebug($"Mensaje enviado exitosamente!");
        }
        catch (Exception e)
        {
            LogDebug($"Error enviando: {e.Message}");
            OnConnectionLost();
        }
    }
    
    private async Task ReceiveMessages(CancellationToken token)
    {
        if (isReceiving)
        {
            LogDebug("Ya hay un receiver activo");
            return;
        }
        
        isReceiving = true;
        LogDebug("Iniciando recepción de mensajes");
        
        byte[] buffer = new byte[1024 * 1024 * 2];
        int consecutiveErrors = 0;
        const int MAX_CONSECUTIVE_ERRORS = 10;
        
        while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), token);
                
                consecutiveErrors = 0;
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    LogDebug("Servidor cerró la conexión");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);
                    OnConnectionLost();
                    break;
                }
                
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    if (result.Count > 0 && result.Count < buffer.Length)
                    {
                        byte[] frameData = new byte[result.Count];
                        Array.Copy(buffer, frameData, result.Count);
                        
                        if (frameData.Length >= 2)
                        {
                            bool isJPEG = (frameData[0] == 0xFF && frameData[1] == 0xD8);
                            
                            if (!isJPEG)
                            {
                                continue;
                            }
                        }
                        
                        UnityMainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            try
                            {
                                if (receivedTexture != null && receivedTexture.LoadImage(frameData))
                                {
                                    frameCount++;
                                    totalFramesReceived++;
                                    
                                    if (videoDisplay != null && videoDisplay.texture != receivedTexture)
                                    {
                                        videoDisplay.texture = receivedTexture;
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"[Tablet] Error cargando imagen (size: {frameData.Length} bytes)");
                                }
                            }
                            catch (Exception ex)
                            {
                               Debug.LogError($"[Tablet] Excepción procesando frame: {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"[Tablet] Frame con tamaño inválido: {result.Count} bytes");
                    }
                }
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    LogDebug($"Mensaje texto: {message}");
                    ProcessTextMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                LogDebug("Recepción cancelada");
                break;
            }
            catch (WebSocketException wsEx)
            {
                consecutiveErrors++;
                LogDebug($"WebSocket error (#{consecutiveErrors}): {wsEx.Message}");
                
                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS || 
                    webSocket.State != WebSocketState.Open)
                {
                    LogDebug($"Estado del WebSocket: {webSocket.State}");
                    OnConnectionLost();
                    break;
                }
                
                await Task.Delay(100, token);
            }
            catch (Exception e)
            {
                consecutiveErrors++;
                LogDebug($"Error recibiendo (#{consecutiveErrors}): {e.GetType().Name} - {e.Message}");
                
                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    OnConnectionLost();
                    break;
                }
                
                await Task.Delay(100, token);
            }
        }
        
        isReceiving = false;
        LogDebug($"Recepción de mensajes terminada. Estado final: {webSocket?.State}");
    }
    private void ProcessTextMessage(string message)
    {
        try
        {
            if (message.Contains("\"type\""))
            {
                MessageType msgType = JsonUtility.FromJson<MessageType>(message);
                
                if (msgType.type == "score")
                {
                    ScoreMessage scoreMsg = JsonUtility.FromJson<ScoreMessage>(message);
                    
                    LogDebug($"[Tablet] Puntaje: Chef={scoreMsg.chefScore}, Soldado={scoreMsg.soldadoScore}");
                    
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (scoreDisplay != null)
                        {
                            scoreDisplay.UpdateScore(scoreMsg);
                        }
                    });
                }
                else if (msgType.type == "winner")  // ← NUEVO
                {
                    WinnerMessage winnerMsg = JsonUtility.FromJson<WinnerMessage>(message);
                    
                    LogDebug($"[Tablet] Ganador recibido: {winnerMsg.winner}");
                    
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (winnerDisplay != null)
                        {
                            winnerDisplay.ShowWinner(winnerMsg);
                        }
                        else
                        {
                            LogDebug("[Tablet] WinnerDisplay es NULL");
                        }
                    });
                }
            }
        }
        catch (Exception e)
        {
            LogDebug($"[Tablet] Error procesando mensaje: {e.Message}");
        }
    }
    private void OnConnectionLost()
    {
        isConnected = false;
        isReceiving = false;
        
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            connectionPanel.SetActive(true);
            gamePanel.SetActive(false);
            UpdateStatusText("Conexión perdida. Intenta reconectar", Color.red);
            
            if (chefButton != null) chefButton.interactable = true;
            if (artilleroButton != null) artilleroButton.interactable = true;
            
            LogDebug("Conexión perdida");
        });
    }
    
    public void DisconnectButton()
    {
        LogDebug("Desconexión manual");
        
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            try
            {
                cts?.Cancel();
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Usuario desconectó", CancellationToken.None);
            }
            catch { }
        }
        
        isConnected = false;
        isReceiving = false;
        connectionPanel.SetActive(true);
        gamePanel.SetActive(false);
        UpdateStatusText("Desconectado", Color.white);
        
        if (chefButton != null) chefButton.interactable = true;
        if (artilleroButton != null) artilleroButton.interactable = true;
    }
    
    void OnDestroy()
    {
        isConnected = false;
        isReceiving = false;
        
        EnhancedTouchSupport.Disable();
        TouchSimulation.Disable();
        
        cts?.Cancel();
        cts?.Dispose();
        
        if (webSocket != null)
        {
            try
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                webSocket.Dispose();
            }
            catch { }
        }
        
        if (receivedTexture != null)
        {
            Destroy(receivedTexture);
        }
        
        LogDebug("Cliente destruido");
    }
}
