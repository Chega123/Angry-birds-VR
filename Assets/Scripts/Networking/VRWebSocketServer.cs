using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class VRWebSocketServer : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private int port = 8080;
    
    [Header("Camera Settings")]
    [SerializeField] private Camera chefCamera;      // Cámara del modo Chef
    [SerializeField] private Camera soldadoCamera;   // Cámara del modo Soldado

    private Camera orthographicCamera; // La cámara activa actual

    
    [Header("Camera Streaming - OPTIMIZACIÓN")]
    [SerializeField] private bool streamCamera = true;
    [SerializeField] private int targetFPS = 30; // Bajado de 60 a 30
    [SerializeField] private int jpegQuality = 50; // Bajado de 70 a 50
    [SerializeField] private Vector2Int streamResolution = new Vector2Int(640, 480); // Bajado de 1024x768
    private TabletSoldadoController soldadoController;
    [Header("Optimización Avanzada")]
    [SerializeField] private bool useAdaptiveQuality = true; // Ajuste dinámico
    [SerializeField] private bool skipFramesWhenBusy = true; // Saltar frames si está ocupado
    [SerializeField] private int maxQueuedFrames = 2; // Máximo de frames en cola
    
    [Header("UI Display")]
    [SerializeField] private Text ipDisplayText;
    [SerializeField] private Text performanceText; // Para stats de rendimiento
    
    private TcpListener tcpListener;
    private TcpClient connectedClient;
    private NetworkStream clientStream;
    private Thread listenerThread;
    private Thread receiverThread;
    private Thread senderThread;
    
    private RenderTexture renderTexture;
    private Texture2D screenShot;
    private float lastFrameTime;
    private bool isClientConnected = false;
    private bool isCapturing = false;
    private bool isRunning = false;
    
    // Sistema de cola para frames
    private Queue<byte[]> frameQueue = new Queue<byte[]>();
    private object queueLock = new object();
    
    // Estadísticas de rendimiento
    private int framesSent = 0;
    private int framesSkipped = 0;
    private float statsTimer = 0f;
    private int currentQuality;
    private float averageFrameSize = 0f;
    public bool IsClientConnected => isClientConnected;
    // Referencia al controlador del sartén
    private TabletSartenController sartenController;
    
    void Start()
    {
        SelectActiveCamera();
        currentQuality = jpegQuality;
        SetupCameraStreaming();
        DisplayLocalIP();
        if (GameModeManager.CurrentMode == GameMode.None)
        {
            Debug.LogWarning("[VR Server] No se seleccionó modo de juego. Usando Chef por defecto.");
            GameModeManager.CurrentMode = GameMode.Chef;
        }

        Debug.Log($"[VR Server] Modo activo: {GameModeManager.CurrentMode}");
        StartServer();
    }


    private void SelectActiveCamera()
    {
        if (GameModeManager.IsChefMode)
        {
            orthographicCamera = chefCamera;
            Debug.Log("[VR Server] Usando cámara del Chef");
        }
        else if (GameModeManager.IsSoldadoMode)
        {
            orthographicCamera = soldadoCamera;
            Debug.Log("[VR Server] Usando cámara del Soldado");
        }
        else
        {
            Debug.LogWarning("[VR Server] No hay modo seleccionado, usando cámara por defecto");
            orthographicCamera = chefCamera; // Fallback
        }
        
        if (orthographicCamera == null)
        {
            Debug.LogError("[VR Server] ¡No se encontró cámara válida!");
        }
    }

    public void RegisterSoldadoController(TabletSoldadoController controller)
    {
        soldadoController = controller;
        Debug.Log("[VR Server] Controlador de soldado registrado");
    }
    // Método para registrar el controlador del sartén
    public void RegisterSartenController(TabletSartenController controller)
    {
        sartenController = controller;
        Debug.Log("Controlador de sartén registrado");
    }
    
    private void SetupCameraStreaming()
    {
        if (streamCamera && orthographicCamera != null)
        {
            // Verificar que la cámara esté activa
            if (!orthographicCamera.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[VR Server] La cámara {orthographicCamera.name} no está activa. Activándola...");
                orthographicCamera.gameObject.SetActive(true);
            }
            
            renderTexture = new RenderTexture(streamResolution.x, streamResolution.y, 24);
            renderTexture.format = RenderTextureFormat.ARGB32;
            renderTexture.antiAliasing = 1;
            renderTexture.Create();
            
            orthographicCamera.targetTexture = renderTexture;
            
            screenShot = new Texture2D(streamResolution.x, streamResolution.y, TextureFormat.RGB24, false);
            
            Debug.Log($"[VR Server] Camera streaming configurado:");
            Debug.Log($"  - Cámara: {orthographicCamera.name}");
            Debug.Log($"  - Resolución: {streamResolution.x}x{streamResolution.y}");
        }
        else
        {
            Debug.LogError("[VR Server] No se pudo configurar streaming");
        }
    }

    
    private void DisplayLocalIP()
    {
        string localIP = GetLocalIPAddress();
        string displayMessage = $"Servidor VR\nIP: {localIP}\nPuerto: {port}\n\nEsperando conexión...";
        
        if (ipDisplayText != null)
        {
            ipDisplayText.text = displayMessage;
        }
        
        Debug.Log($"=== SERVIDOR VR INICIANDO ===");
        Debug.Log($"IP: {localIP} | Puerto: {port}");
    }
    
    private string GetLocalIPAddress()
    {
        try
        {
            string localIP = "No encontrada";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    if (!ipStr.StartsWith("127."))
                    {
                        localIP = ipStr;
                        if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10."))
                        {
                            break;
                        }
                    }
                }
            }
            return localIP;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error obteniendo IP: {e.Message}");
            return "Error";
        }
    }
    
    private void StartServer()
    {
        try
        {
            isRunning = true;
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            
            Debug.Log($"✓ Servidor TCP iniciado en puerto {port}");
            
            listenerThread = new Thread(ListenForClients);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"✗ Error iniciando servidor: {e.Message}");
        }
    }
    
    private void ListenForClients()
    {
        while (isRunning)
        {
            try
            {
                TcpClient client = tcpListener.AcceptTcpClient();
                
                // Configurar TCP para baja latencia
                client.NoDelay = true; // CRÍTICO: Desactiva algoritmo de Nagle
                client.SendBufferSize = 65536;
                client.ReceiveBufferSize = 8192;
                
                Debug.Log($"Cliente conectado desde: {client.Client.RemoteEndPoint}");
                
                if (connectedClient != null)
                {
                    connectedClient.Close();
                }
                
                connectedClient = client;
                clientStream = client.GetStream();
                
                if (PerformWebSocketHandshake(clientStream))
                {
                    isClientConnected = true;
                    Debug.Log("✓ WebSocket handshake completado!");
                    
                    // Limpiar cola de frames antiguos
                    lock (queueLock)
                    {
                        frameQueue.Clear();
                    }
                    
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (ipDisplayText != null)
                        {
                            ipDisplayText.text = $"Tablet Conectada!\nIP: {GetLocalIPAddress()}\nPuerto: {port}";
                        }
                    });
                    
                    receiverThread = new Thread(ReceiveMessages);
                    receiverThread.IsBackground = true;
                    receiverThread.Start();
                    
                    senderThread = new Thread(SendFramesThread);
                    senderThread.IsBackground = true;
                    senderThread.Start();
                }
            }
            catch (Exception e)
            {
                if (isRunning)
                {
                    Debug.LogError($"Error aceptando cliente: {e.Message}");
                }
            }
        }
    }
    
    private bool PerformWebSocketHandshake(NetworkStream stream)
    {
        try
        {
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            string key = "";
            string[] lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (line.StartsWith("Sec-WebSocket-Key:"))
                {
                    key = line.Substring("Sec-WebSocket-Key:".Length).Trim();
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("No se encontró Sec-WebSocket-Key");
                return false;
            }
            
            string acceptKey = Convert.ToBase64String(
                System.Security.Cryptography.SHA1.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
                )
            );
            
            string response = 
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en handshake: {e.Message}");
            return false;
        }
    }
    
    void Update()
    {
        if (streamCamera && isClientConnected && !isCapturing)
        {
            float timeSinceLastFrame = Time.time - lastFrameTime;
            if (timeSinceLastFrame >= 1f / targetFPS)
            {
                lastFrameTime = Time.time;
                
                // Verificar si debemos saltar este frame
                if (skipFramesWhenBusy)
                {
                    lock (queueLock)
                    {
                        if (frameQueue.Count >= maxQueuedFrames)
                        {
                            framesSkipped++;
                            return; // Saltar captura si hay muchos frames en cola
                        }
                    }
                }
                
                CaptureAndQueueFrame();
            }
        }
        
        UpdatePerformanceStats();
    }
    
    // Si el problema persiste, usa esta versión con MÁS debug:

    private void CaptureAndQueueFrame()
    {
        if (isCapturing || !isClientConnected) return;
        
        isCapturing = true;
        
        try
        {
            //Debug.Log($"[VR Server] === INICIANDO CAPTURA ===");
           // Debug.Log($"[VR Server] RenderTexture: {renderTexture.width}x{renderTexture.height}, Format: {renderTexture.format}");
           // Debug.Log($"[VR Server] ScreenShot: {screenShot.width}x{screenShot.height}, Format: {screenShot.format}");
            
            RenderTexture.active = renderTexture;
            
            screenShot.ReadPixels(new Rect(0, 0, streamResolution.x, streamResolution.y), 0, 0, false);
            screenShot.Apply(false, false);
            
            RenderTexture.active = null;
            
            //Debug.Log($"[VR Server] Píxeles leídos. Codificando a JPEG con calidad {currentQuality}...");
            
            // Intentar codificación
            byte[] imageBytes = null;
            try
            {
                imageBytes = screenShot.EncodeToJPG(currentQuality);
               // Debug.Log($"[VR Server] EncodeToJPG completado: {imageBytes?.Length ?? 0} bytes");
            }
            catch (Exception encodeEx)
            {
              //  Debug.LogError($"[VR Server] ERROR en EncodeToJPG: {encodeEx.Message}\n{encodeEx.StackTrace}");
                return;
            }
            
            if (imageBytes == null)
            {
               // Debug.LogError("[VR Server] EncodeToJPG retornó NULL");
                return;
            }
            
            if (imageBytes.Length == 0)
            {
              //  Debug.LogError("[VR Server] EncodeToJPG retornó array vacío");
                return;
            }
            
            // Verificar firma JPEG (debe empezar con FF D8)
            if (imageBytes.Length >= 2)
            {
                bool isJPEG = (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8);
               // Debug.Log($"[VR Server] ¿Es JPEG válido? {isJPEG} (primeros bytes: {imageBytes[0]:X2} {imageBytes[1]:X2})");
                
                if (!isJPEG)
                {
                 //   Debug.LogError("[VR Server] Los datos NO son un JPEG válido!");
                    return;
                }
            }
            
            float sizeKB = imageBytes.Length / 1024f;
           // Debug.Log($"[VR Server] ✓ Frame JPEG válido: {sizeKB:F1}KB");
            
            // Verificar tamaño razonable
            if (imageBytes.Length >= 2000000) // Cerca de 2MB
            {
              //  Debug.LogError($"[VR Server] Frame sospechosamente grande: {sizeKB:F1}KB - ¿No se comprimió?");
            }
            
            lock (queueLock)
            {
                if (frameQueue.Count >= maxQueuedFrames)
                {
                    frameQueue.Dequeue();
                    framesSkipped++;
                }
                frameQueue.Enqueue(imageBytes);
                //Debug.Log($"[VR Server] Frame agregado a cola. Cola actual: {frameQueue.Count}");
            }
            
            averageFrameSize = (averageFrameSize * 0.9f) + (imageBytes.Length * 0.1f);
        }
        catch (Exception e)
        {
            //Debug.LogError($"[VR Server] Error capturando frame: {e.GetType().Name}\n{e.Message}\n{e.StackTrace}");
        }
        finally
        {
            isCapturing = false;
        }
    }
    
    private void AdjustQualityBasedOnPerformance()
    {
        // Si hay muchos frames en cola, reducir calidad
        int queueSize;
        lock (queueLock)
        {
            queueSize = frameQueue.Count;
        }
        
        if (queueSize > 1 && currentQuality > 30)
        {
            currentQuality = Mathf.Max(30, currentQuality - 5);
        }
        else if (queueSize == 0 && currentQuality < jpegQuality)
        {
            currentQuality = Mathf.Min(jpegQuality, currentQuality + 2);
        }
    }
    
    private void SendFramesThread()
    {
        Debug.Log("[VR Server] Thread de envío iniciado");
        int consecutiveErrors = 0;
        const int MAX_CONSECUTIVE_ERRORS = 5;
        
        while (isClientConnected && clientStream != null)
        {
            try
            {
                byte[] frameToSend = null;
                
                lock (queueLock)
                {
                    if (frameQueue.Count > 0)
                    {
                        frameToSend = frameQueue.Dequeue();
                    }
                }
                
                if (frameToSend != null)
                {
                    //Debug.Log($"[VR Server] Enviando frame de {frameToSend.Length} bytes...");
                    SendWebSocketBinary(frameToSend);
                    //Debug.Log($"[VR Server] Frame enviado exitosamente");
                    framesSent++;
                    consecutiveErrors = 0;
                    
                    // CRÍTICO: Esperar un poco después de cada envío para evitar overflow
                    Thread.Sleep(10); // 10ms de delay entre frames
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
            catch (System.IO.IOException ioEx)
            {
                consecutiveErrors++;
                Debug.LogError($"[VR Server] IOException en envío (#{consecutiveErrors}): {ioEx.Message}");
                
                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    Debug.LogError("[VR Server] Demasiados errores consecutivos");
                    OnClientDisconnected();
                    break;
                }
                
                Thread.Sleep(100);
            }
            catch (Exception e)
            {
                consecutiveErrors++;
                Debug.LogError($"[VR Server] Error en envío (#{consecutiveErrors}): {e.Message}");
                
                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    OnClientDisconnected();
                    break;
                }
                
                Thread.Sleep(100);
            }
        }
        
        Debug.Log("[VR Server] Thread de envío terminado");
    }

    private void SendWebSocketBinary(byte[] data)
    {
        if (clientStream == null || !clientStream.CanWrite)
        {
            Debug.LogWarning("[VR Server] Stream no disponible");
            throw new System.IO.IOException("Stream no disponible");
        }
        
        if (connectedClient?.Connected != true)
        {
            Debug.LogWarning("[VR Server] Cliente desconectado");
            throw new System.IO.IOException("Cliente desconectado");
        }
        
        try
        {
            List<byte> frame = new List<byte>();
            
            // Header del WebSocket
            frame.Add(0x82); // FIN + Binary opcode
            
            // Payload length
            long payloadLength = data.Length;
            
            if (payloadLength < 126)
            {
                frame.Add((byte)payloadLength);
            }
            else if (payloadLength < 65536)
            {
                frame.Add(126);
                frame.Add((byte)((payloadLength >> 8) & 0xFF));
                frame.Add((byte)(payloadLength & 0xFF));
            }
            else
            {
                frame.Add(127);
                for (int i = 7; i >= 0; i--)
                {
                    frame.Add((byte)((payloadLength >> (8 * i)) & 0xFF));
                }
            }
            
            // Agregar el payload
            frame.AddRange(data);
            
            byte[] frameBytes = frame.ToArray();
            
            Debug.Log($"[VR Server] Enviando WebSocket frame: Header={frame.Count - data.Length} bytes, Payload={data.Length} bytes, Total={frameBytes.Length} bytes");
            
            // CRÍTICO: Enviar en una sola operación Write
            clientStream.Write(frameBytes, 0, frameBytes.Length);
            clientStream.Flush();
            
            Debug.Log($"[VR Server] ✓ Frame enviado y flushed");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VR Server] Error en SendWebSocketBinary: {e.GetType().Name} - {e.Message}");
            throw;
        }
    }
    
    private void UpdatePerformanceStats()
    {
        if (performanceText != null && isClientConnected)
        {
            statsTimer += Time.deltaTime;
            
            if (statsTimer >= 1f)
            {
                int queueSize;
                lock (queueLock)
                {
                    queueSize = frameQueue.Count;
                }
                
                performanceText.text = $"FPS: {framesSent}/s | Skip: {framesSkipped}\n" +
                                      $"Queue: {queueSize} | Quality: {currentQuality}\n" +
                                      $"Size: {(averageFrameSize / 1024f):F1}KB";
                
                framesSent = 0;
                framesSkipped = 0;
                statsTimer = 0f;
            }
        }
    }
    
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[4096];
        
        while (isClientConnected && clientStream != null)
        {
            try
            {
                if (!clientStream.DataAvailable)
                {
                    Thread.Sleep(10);
                    continue;
                }
                
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    OnClientDisconnected();
                    break;
                }
                
                byte[] message = DecodeWebSocketFrame(buffer, bytesRead);
                if (message != null && message.Length > 0)
                {
                    string jsonMessage = Encoding.UTF8.GetString(message);
                    
                    Debug.Log($"[VR Server] Mensaje recibido: {jsonMessage}");
                    
                    // NUEVO: Verificar si es un mensaje de modo
                    if (jsonMessage.Contains("\"type\":\"mode\""))
                    {
                        Debug.Log("[VR Server] Detectado mensaje de modo");
                        UnityMainThreadDispatcher.Instance.Enqueue(() => ProcessModeMessage(jsonMessage));
                    }
                    else
                    {
                        // Es un mensaje de touch normal
                        UnityMainThreadDispatcher.Instance.Enqueue(() => ProcessTabletInput(jsonMessage));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error recibiendo mensaje: {e.Message}");
                OnClientDisconnected();
                break;
            }
        }
    }

    private void ProcessModeMessage(string jsonMessage)
    {
        try
        {
            Debug.Log($"[VR Server] Procesando mensaje de modo: {jsonMessage}");
            
            // Parsear el mensaje de modo
            ModeMessage modeData = JsonUtility.FromJson<ModeMessage>(jsonMessage);
            
            if (modeData == null || string.IsNullOrEmpty(modeData.mode))
            {
                Debug.LogError("[VR Server] Mensaje de modo inválido - datos nulos");
                return;
            }
            
            Debug.Log($"[VR Server] Modo parseado: {modeData.mode}");
            
            // Convertir string a enum
            if (System.Enum.TryParse<GameMode>(modeData.mode, out GameMode mode))
            {
                GameModeManager.CurrentMode = mode;
                Debug.Log($"[VR Server] ✓ Modo actualizado a: {mode}");
                
                // Actualizar cámara según el modo
                SelectActiveCamera();
                
                // Reconfigurar streaming con la nueva cámara
                if (streamCamera && orthographicCamera != null)
                {
                    Debug.Log($"[VR Server] Reconfigurando streaming para modo {mode}");
                    SetupCameraStreaming();
                }
            }
            else
            {
                Debug.LogError($"[VR Server] Modo inválido recibido: {modeData.mode}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VR Server] Error procesando modo: {e.Message}\n{e.StackTrace}");
        }
    }

    
    private byte[] DecodeWebSocketFrame(byte[] buffer, int length)
    {
        try
        {
            int offset = 2;
            bool masked = (buffer[1] & 0x80) != 0;
            int payloadLength = buffer[1] & 0x7F;
            
            if (payloadLength == 126)
            {
                payloadLength = (buffer[2] << 8) | buffer[3];
                offset = 4;
            }
            else if (payloadLength == 127)
            {
                offset = 10;
            }
            
            byte[] decoded = new byte[payloadLength];
            
            if (masked)
            {
                byte[] mask = new byte[4];
                Array.Copy(buffer, offset, mask, 0, 4);
                offset += 4;
                
                for (int i = 0; i < payloadLength; i++)
                {
                    decoded[i] = (byte)(buffer[offset + i] ^ mask[i % 4]);
                }
            }
            else
            {
                Array.Copy(buffer, offset, decoded, 0, payloadLength);
            }
            
            return decoded;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error decodificando frame: {e.Message}");
            return null;
        }
    }
    
    
    private void ProcessTabletInput(string jsonMessage)
    {
        try
        {
            TabletInput input = JsonUtility.FromJson<TabletInput>(jsonMessage);
            
            Debug.Log($"[VR Server] Touch recibido en modo: {GameModeManager.CurrentMode}");
            Debug.Log($"[VR Server] screenX={input.screenX}, screenY={input.screenY}, action={input.action}");
            
            // Decidir qué controlador usar según el modo de juego
            if (GameModeManager.IsChefMode)
            {
                Debug.Log("[VR Server] Enrutando a Chef controller");
                if (sartenController != null)
                {
                    sartenController.OnTabletTouch(input);
                    Debug.Log($"[VR Server] ✓ Touch enviado al sartén controller");
                }
                else
                {
                    Debug.LogWarning("[VR Server] sartenController es NULL");
                }
            }
            else if (GameModeManager.IsSoldadoMode)
            {
                Debug.Log("[VR Server] Enrutando a Soldado controller");
                if (soldadoController != null)
                {
                    soldadoController.OnTabletTouch(input);
                    Debug.Log($"[VR Server] ✓ Touch enviado al soldado controller");
                }
                else
                {
                    Debug.LogWarning("[VR Server] soldadoController es NULL");
                }
            }
            else
            {
                Debug.LogWarning($"[VR Server] Modo desconocido: {GameModeManager.CurrentMode}");
            }
            
            // Raycast opcional para otros objetos interactuables
            Vector3 screenPoint = new Vector3(input.screenX, input.screenY, orthographicCamera.nearClipPlane);
            Ray ray = orthographicCamera.ScreenPointToRay(screenPoint);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Debug.Log($"[VR Server] Raycast hit: {hit.collider.gameObject.name}");
                
                var interactable = hit.collider.GetComponent<TabletInteractable>();
                if (interactable != null)
                {
                    interactable.OnTabletInteract(input);
                    Debug.Log($"[VR Server] Interacción enviada a {hit.collider.gameObject.name}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VR Server] Error procesando input: {e.Message}\n{e.StackTrace}");
        }
    }



    
    private void OnClientDisconnected()
    {
        isClientConnected = false;
        
        if (connectedClient != null)
        {
            connectedClient.Close();
            connectedClient = null;
        }
        
        clientStream = null;
        
        lock (queueLock)
        {
            frameQueue.Clear();
        }
        
        Debug.Log("✗ Tablet desconectada");
        
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            if (ipDisplayText != null)
            {
                ipDisplayText.text = $"Tablet Desconectada\nIP: {GetLocalIPAddress()}\nPuerto: {port}\n\nEsperando reconexión...";
            }
        });
    }
    
    public void SendMessageToClient(string message)
    {
        if (!isClientConnected || clientStream == null)
        {
            Debug.LogWarning("[VR Server] No hay cliente conectado");
            return;
        }
        
        try
        {
            Debug.Log($"[VR Server] Enviando: {message}");
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            SendWebSocketText(messageBytes);
            Debug.Log("[VR Server] ✓ Mensaje enviado");
        }
        catch (Exception e)
        {
            Debug.LogError($"[VR Server] Error: {e.Message}");
        }
    }

    private void SendWebSocketText(byte[] data)
    {
        if (clientStream == null || !clientStream.CanWrite)
        {
            throw new System.IO.IOException("Stream no disponible");
        }
        
        List<byte> frame = new List<byte>();
        frame.Add(0x81); // FIN + Text opcode
        
        long payloadLength = data.Length;
        if (payloadLength < 126)
        {
            frame.Add((byte)payloadLength);
        }
        else if (payloadLength < 65536)
        {
            frame.Add(126);
            frame.Add((byte)((payloadLength >> 8) & 0xFF));
            frame.Add((byte)(payloadLength & 0xFF));
        }
        else
        {
            frame.Add(127);
            for (int i = 7; i >= 0; i--)
            {
                frame.Add((byte)((payloadLength >> (8 * i)) & 0xFF));
            }
        }
        
        frame.AddRange(data);
        byte[] frameBytes = frame.ToArray();
        clientStream.Write(frameBytes, 0, frameBytes.Length);
        clientStream.Flush();
    }

    void OnDestroy()
    {
        isRunning = false;
        isClientConnected = false;
        
        if (connectedClient != null)
        {
            connectedClient.Close();
        }
        
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
        
        if (renderTexture != null)
        {
            orthographicCamera.targetTexture = null;
            Destroy(renderTexture);
        }
        
        if (screenShot != null)
        {
            Destroy(screenShot);
        }
        
        Debug.Log("Servidor VR detenido");
    }
}
[System.Serializable]
public class ModeMessage
{
    public string type;
    public string mode;
}