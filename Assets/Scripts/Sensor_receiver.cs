using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SensorReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int listenPort = 9060;
    
    [Header("UI References")]
    public TextMeshProUGUI ipText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI lastMessageText;
    public TextMeshProUGUI accelText;
    public TextMeshProUGUI gyroText;
    public Image statusIndicator;

    [Header("Gravity Compensation")]
    [SerializeField] private bool removeGravity = true;
    [SerializeField] private float gravityMagnitude = 9.8f;
    [SerializeField] private bool autoCalibrate = true;
    [SerializeField] private int calibrationSamples = 30;
    
    [Header("Data Normalization")]
    [SerializeField] private bool normalizeData = true;
    [SerializeField] private float accelScaleFactor = 0.01f; // Dividir por 100 para normalizar
    [SerializeField] private float gyroScaleFactor = 0.01f; // Dividir por 100 para normalizar
    
    private UdpClient udp;
    private Thread receiveThread;
    private string lastMessage = "";
    private bool isRunning = false;
    private string localIP = "Obteniendo...";
    private float lastReceiveTime = 0f;
    private bool isReceivingData = false;

    // Datos RAW del sensor
    private Vector3 rawAccelData = Vector3.zero;
    private Vector3 rawGyroData = Vector3.zero;

    // Datos procesados (sin gravedad)
    public Vector3 accelData { get; private set; }
    public Vector3 gyroData { get; private set; }

    // Calibración automática
    private Vector3 gravityVector = new Vector3(0, 0, -9.8f);
    private List<Vector3> calibrationBuffer = new List<Vector3>();
    private bool isCalibrated = false;

    void Start()
    {
        localIP = GetLocalIPAddress();
        Debug.Log($"╔══════════════════════════════════╗");
        Debug.Log($"║ 📡 IP de las gafas: {localIP}");
        Debug.Log($"║ 🔌 Puerto: {listenPort}");
        Debug.Log($"║ ▶️  Configura tu celular para enviar a:");
        Debug.Log($"║    {localIP}:{listenPort}");
        Debug.Log($"╚══════════════════════════════════╝");
        
        UpdateUI();
        StartReceiving();
    }

    void StartReceiving()
    {
        try
        {
            udp = new UdpClient(listenPort);
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log($"✅ Receptor UDP iniciado en puerto {listenPort}");
            
            if (autoCalibrate)
            {
                Debug.Log($"🎯 Calibración automática activada. Mantén el dispositivo quieto...");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al iniciar receptor: {e.Message}");
            if (statusText != null)
                statusText.text = "❌ Error al iniciar";
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (isRunning)
        {
            try
            {
                byte[] data = udp.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);
                lastMessage = message;
                lastReceiveTime = Time.time;
                isReceivingData = true;

                string[] values = message.Split(',');
                if (values.Length == 6)
                {
                    // Guardar datos RAW y aplicar escala
                    rawAccelData = new Vector3(
                        float.Parse(values[0]) * accelScaleFactor,
                        float.Parse(values[1]) * accelScaleFactor,
                        float.Parse(values[2]) * accelScaleFactor
                    );
                    rawGyroData = new Vector3(
                        float.Parse(values[3]) * gyroScaleFactor,
                        float.Parse(values[4]) * gyroScaleFactor,
                        float.Parse(values[5]) * gyroScaleFactor
                    );

                    // Calibración automática
                    if (autoCalibrate && !isCalibrated)
                    {
                        calibrationBuffer.Add(rawAccelData);
                        
                        if (calibrationBuffer.Count >= calibrationSamples)
                        {
                            CalibrateGravity();
                        }
                    }

                    // Procesar datos (remover gravedad)
                    ProcessSensorData();
                }

                //Debug.Log($"📱 Recibido de {remoteEP}: {message}");
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogError($"❌ Error: {e.Message}");
            }
        }
    }

    void CalibrateGravity()
    {
        // Calcular el promedio de las muestras en reposo
        Vector3 sum = Vector3.zero;
        foreach (var sample in calibrationBuffer)
        {
            sum += sample;
        }
        gravityVector = sum / calibrationBuffer.Count;
        
        isCalibrated = true;
        calibrationBuffer.Clear();
        
        Debug.Log($"✅ Calibración completada!");
        Debug.Log($"📍 Vector de gravedad detectado: {gravityVector}");
        Debug.Log($"📏 Magnitud: {gravityVector.magnitude:F2} m/s²");
    }

    void ProcessSensorData()
    {
        if (removeGravity)
        {
            // Remover la gravedad del acelerómetro
            accelData = rawAccelData - gravityVector;
            
            // Opcional: Filtrar ruido pequeño (dead zone)
            if (accelData.magnitude < 0.1f)
            {
                accelData = Vector3.zero;
            }
        }
        else
        {
            accelData = rawAccelData;
        }
        
        // El giroscopio no necesita compensación de gravedad
        gyroData = rawGyroData;
    }

    void Update()
    {
        // Detectar si dejamos de recibir datos
        if (isReceivingData && Time.time - lastReceiveTime > 2f)
        {
            isReceivingData = false;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (ipText != null)
            ipText.text = $"📡 IP: {localIP}:{listenPort}";

        if (statusText != null)
        {
            if (!isCalibrated && autoCalibrate)
            {
                statusText.text = $"🎯 Calibrando... ({calibrationBuffer.Count}/{calibrationSamples})";
            }
            else if (isReceivingData)
            {
                statusText.text = "✅ Recibiendo datos";
            }
            else
            {
                statusText.text = "⏳ Esperando conexión del celular...";
            }
        }

        if (lastMessageText != null)
            lastMessageText.text = $"📱 Último: {lastMessage}";

        if (accelText != null)
        {
            accelText.text = $"📊 Accel (normalizado):\n" +
                           $"X: {accelData.x:F2}\n" +
                           $"Y: {accelData.y:F2}\n" +
                           $"Z: {accelData.z:F2}\n" +
                           $"Mag: {accelData.magnitude:F2} m/s²";
        }

        if (gyroText != null)
        {
            gyroText.text = $"🌀 Gyro:\n" +
                          $"X: {gyroData.x:F2}\n" +
                          $"Y: {gyroData.y:F2}\n" +
                          $"Z: {gyroData.z:F2}\n" +
                          $"Mag: {gyroData.magnitude:F2} rad/s";
        }

        // Indicador visual de estado
        if (statusIndicator != null)
        {
            if (!isCalibrated && autoCalibrate)
                statusIndicator.color = Color.yellow;
            else if (isReceivingData)
                statusIndicator.color = Color.green;
            else
                statusIndicator.color = Color.red;
        }
    }

    string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error obteniendo IP: {e.Message}");
        }
        return "No disponible";
    }

    // Método público para recalibrar manualmente
    public void RecalibrateGravity()
    {
        isCalibrated = false;
        calibrationBuffer.Clear();
        Debug.Log("🔄 Recalibrando gravedad... Mantén el dispositivo quieto.");
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
        udp?.Close();
    }

    void OnDestroy()
    {
        isRunning = false;
        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Abort();
        udp?.Close();
    }

    // Debug en consola
    void OnGUI()
    {
        if (!isCalibrated && autoCalibrate)
        {
            GUI.Box(new Rect(10, Screen.height - 100, 400, 80), "");
            GUI.Label(new Rect(20, Screen.height - 90, 380, 30), 
                "🎯 CALIBRANDO GRAVEDAD...");
            GUI.Label(new Rect(20, Screen.height - 60, 380, 30), 
                $"Mantén el celular quieto: {calibrationBuffer.Count}/{calibrationSamples}");
        }
    }
}