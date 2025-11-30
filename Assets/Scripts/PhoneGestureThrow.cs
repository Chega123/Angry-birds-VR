using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;

public class PhoneGestureThrow : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SensorReceiver sensorReceiver;
    [SerializeField] private XRBaseInteractor targetInteractor; // Asigna el Near-Far Interactor aquí
    [SerializeField] private Transform vrCamera; // Asigna la cámara VR (Main Camera del XR Origin)

    [Header("Configuración de Detección de Gesto")]
    [SerializeField] private float accelThreshold = 0.5f; // Umbral de aceleración para detectar lanzamiento
    [SerializeField] private float decelThreshold = 1.0f; // Desaceleración que indica soltar
    [SerializeField] private float gestureTimeWindow = 0.3f; // Ventana de tiempo para detectar el gesto completo
    [SerializeField] private float minThrowForce = 5f;
    [SerializeField] private float maxThrowForce = 50f;
    [SerializeField] private float forceMultiplier = 20f; // Aumentado para más impacto
    [SerializeField] private bool useVelocityMode = true; // Usar velocidad directa en vez de fuerza

    [Header("Dirección de Lanzamiento")]
    [SerializeField] [Range(0f, 1f)] private float cameraInfluence = 0.7f; // Cuánto influye la mirada (0 = solo gesto, 1 = solo cámara)
    [SerializeField] private float upwardBias = 0.0f; // Elevación adicional para tiros más altos

    [Header("Filtrado de Ruido")]
    [SerializeField] private int smoothingSamples = 5; // Muestras para suavizar datos
    [SerializeField] private float gyroContributionFactor = 0.3f; // Peso del giroscopio en la dirección

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool drawTrajectoryPreview = true;

    // Estado del gesto
    private bool isHoldingObject = false;
    private XRGrabInteractable currentGrabbedObject;
    private Rigidbody currentObjectRb;

    // Detección de gesto
    private List<Vector3> accelHistory = new List<Vector3>();
    private List<Vector3> gyroHistory = new List<Vector3>();
    private float gestureStartTime = 0f;
    private bool isInThrowGesture = false;
    private Vector3 peakAcceleration = Vector3.zero;
    private Vector3 throwDirection = Vector3.forward;

    // Datos filtrados
    private Vector3 smoothedAccel = Vector3.zero;
    private Vector3 smoothedGyro = Vector3.zero;

    void Start()
    {
        if (sensorReceiver == null)
        {
            sensorReceiver = FindFirstObjectByType<SensorReceiver>();
            if (sensorReceiver == null)
            {
                Debug.LogError("❌ No se encontró SensorReceiver en la escena!");
                enabled = false;
                return;
            }
        }

        // Buscar la cámara VR automáticamente si no está asignada
        if (vrCamera == null)
        {
            vrCamera = Camera.main?.transform;
            if (vrCamera == null)
            {
                Debug.LogWarning("⚠️ No se encontró la cámara VR. Asigna 'Main Camera' en el Inspector.");
            }
            else
            {
                Debug.Log($"✅ Cámara VR encontrada: {vrCamera.name}");
            }
        }

        if (targetInteractor != null)
        {
            // Suscribirse a eventos del interactor
            targetInteractor.selectEntered.AddListener(OnObjectGrabbed);
            targetInteractor.selectExited.AddListener(OnObjectReleased);
        }
        else
        {
            Debug.LogWarning("⚠️ No se asignó targetInteractor. Asigna el Near-Far Interactor en el Inspector.");
        }

        Debug.Log("✅ PhoneGestureThrow inicializado");
    }

    void OnDestroy()
    {
        if (targetInteractor != null)
        {
            targetInteractor.selectEntered.RemoveListener(OnObjectGrabbed);
            targetInteractor.selectExited.RemoveListener(OnObjectReleased);
        }
    }

    void OnObjectGrabbed(SelectEnterEventArgs args)
    {
        currentGrabbedObject = args.interactableObject as XRGrabInteractable;

        if (currentGrabbedObject != null)
        {
            currentObjectRb = currentGrabbedObject.GetComponent<Rigidbody>();
            isHoldingObject = true;

            // Resetear estado del gesto
            accelHistory.Clear();
            gyroHistory.Clear();
            isInThrowGesture = false;
            gestureStartTime = 0f;

            // LOG DETALLADO
            Debug.Log("════════════════════════════════════════");
            Debug.Log($"✅ OBJETO AGARRADO DETECTADO");
            Debug.Log($"📦 Nombre: {currentGrabbedObject.name}");
            Debug.Log($"🎯 Tag: {currentGrabbedObject.tag}");
            Debug.Log($"📍 Posición: {currentGrabbedObject.transform.position}");
            Debug.Log($"⚙️ Tiene Rigidbody: {currentObjectRb != null}");
            Debug.Log($"👤 Interactor: {args.interactorObject.transform.name}");
            Debug.Log($"🕐 Tiempo: {Time.time:F2}s");
            Debug.Log("════════════════════════════════════════");
        }
        else
        {
            Debug.LogWarning("⚠️ OnObjectGrabbed llamado pero no se pudo obtener XRGrabInteractable");
        }
    }

    void OnObjectReleased(SelectExitEventArgs args)
    {
        Debug.Log("📤 Objeto soltado manualmente");
        ResetGestureState();
    }

    void Update()
    {
        if (!isHoldingObject || sensorReceiver == null) return;

        // Actualizar datos suavizados
        UpdateSmoothData();

        // Detectar gesto de lanzamiento
        DetectThrowGesture();

        // Debug visual
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    void UpdateSmoothData()
    {
        // Agregar nuevos datos al historial
        accelHistory.Add(sensorReceiver.accelData);
        gyroHistory.Add(sensorReceiver.gyroData);

        // Mantener solo las últimas N muestras
        if (accelHistory.Count > smoothingSamples)
        {
            accelHistory.RemoveAt(0);
        }
        if (gyroHistory.Count > smoothingSamples)
        {
            gyroHistory.RemoveAt(0);
        }

        // Calcular promedio
        smoothedAccel = Vector3.zero;
        smoothedGyro = Vector3.zero;

        foreach (var a in accelHistory)
            smoothedAccel += a;
        foreach (var g in gyroHistory)
            smoothedGyro += g;

        if (accelHistory.Count > 0)
        {
            smoothedAccel /= accelHistory.Count;
            smoothedGyro /= gyroHistory.Count;
        }
    }

    void DetectThrowGesture()
    {
        float accelMagnitude = smoothedAccel.magnitude;

        // FASE 1: Detectar inicio del gesto (aceleración fuerte)
        if (!isInThrowGesture && accelMagnitude > accelThreshold)
        {
            isInThrowGesture = true;
            gestureStartTime = Time.time;
            peakAcceleration = smoothedAccel;

            // Calcular dirección de lanzamiento basada en aceleración y giroscopio
            throwDirection = CalculateThrowDirection();

            Debug.Log($"🎯 Gesto de lanzamiento INICIADO! Accel: {accelMagnitude:F2}");
        }

        // FASE 2: Durante el gesto, actualizar pico de aceleración
        if (isInThrowGesture)
        {
            float timeSinceStart = Time.time - gestureStartTime;

            // Actualizar pico si encontramos mayor aceleración
            if (accelMagnitude > peakAcceleration.magnitude)
            {
                peakAcceleration = smoothedAccel;
                throwDirection = CalculateThrowDirection();
            }

            // FASE 3: Detectar desaceleración (cuando la aceleración baja significativamente)
            bool hasDecelerated = accelMagnitude < decelThreshold;
            bool timeExpired = timeSinceStart > gestureTimeWindow;

            if (hasDecelerated || timeExpired)
            {
                Debug.Log($"🎯 Desaceleración detectada! Accel actual: {accelMagnitude:F2}, Umbral: {decelThreshold:F2}");
                // ¡Gesto completado! Ejecutar lanzamiento
                ExecuteThrow();
            }
        }
    }

    Vector3 CalculateThrowDirection()
    {
        // Obtener la dirección hacia donde mira la cámara VR
        Vector3 cameraForward = vrCamera != null ? vrCamera.forward : Vector3.forward;
        
        // Transformar el movimiento del teléfono a espacio mundial
        Vector3 phoneWorldDir = TransformPhoneToWorld(smoothedAccel).normalized;

        // OPCIÓN 2: Mezclar la dirección de la cámara con el movimiento del teléfono
        // Esto permite cierta influencia del gesto pero principalmente va hacia donde miras
        Vector3 throwDir = Vector3.Lerp(phoneWorldDir, cameraForward, cameraInfluence).normalized;

        // Añadir elevación adicional si se configuró
        if (upwardBias > 0f)
        {
            throwDir.y += upwardBias;
            throwDir.Normalize();
        }

        if (vrCamera == null)
        {
            Debug.LogWarning("⚠️ No hay referencia a la cámara VR, usando dirección del gesto únicamente");
        }

        return throwDir;
    }

    Vector3 TransformPhoneToWorld(Vector3 phoneVector)
    {
        // Mapeo para teléfono VERTICAL pegado al antebrazo (como smartwatch)
        // El teléfono está de pie, pantalla mirando hacia ti, pegado al brazo
        
        // En esta orientación (teléfono vertical en el brazo):
        // - Aceleración en reposo: (0, 0, -1) debe convertirse a (0, -1, 0) en Unity
        // - Cuando mueves el brazo hacia adelante: Y+ del teléfono → Z+ en Unity
        // - Cuando mueves el brazo hacia los lados: X del teléfono → X en Unity
        // - Cuando levantas/bajas el brazo: Y del teléfono cambia → Y en Unity
        
        // Acelerómetro del teléfono vertical:
        // X+ = hacia afuera del brazo (lateral)
        // Y+ = hacia arriba del antebrazo (hacia el codo)
        // Z+ = hacia afuera de la pantalla (hacia ti)
        
        // Transformación a Unity para lanzamiento natural:
        // - X del teléfono → X de Unity (mantener movimiento lateral)
        // - Y del teléfono → -Y de Unity (movimiento vertical del brazo)
        // - Z del teléfono → -Z de Unity (adelante cuando empujas hacia ti)

        return new Vector3(
            phoneVector.x,    // Lateral se mantiene
            -phoneVector.y,   // Arriba/abajo del brazo → arriba/abajo Unity (invertido)
            -phoneVector.z    // Hacia ti = hacia adelante en Unity (invertido)
        );
    }

    void ExecuteThrow()
    {
        if (currentObjectRb == null || currentGrabbedObject == null)
        {
            ResetGestureState();
            return;
        }

        Debug.Log("🚀 Ejecutando LANZAMIENTO!");

        // Calcular fuerza basada en la magnitud del pico de aceleración
        float forceMagnitude = Mathf.Clamp(
            peakAcceleration.magnitude * forceMultiplier,
            minThrowForce,
            maxThrowForce
        );

        // CRÍTICO: Guardar referencias antes de soltar
        Rigidbody rbToThrow = currentObjectRb;
        Vector3 finalDirection = throwDirection;
        float finalForce = forceMagnitude;

        // Desactivar kinematic ANTES de soltar para que la física esté lista
        currentObjectRb.isKinematic = false;
        currentObjectRb.useGravity = true;

        // Forzar soltar el objeto
        if (targetInteractor != null && targetInteractor.hasSelection)
        {
            targetInteractor.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)currentGrabbedObject
            );
        }

        // Aplicar fuerza INMEDIATAMENTE después de soltar
        StartCoroutine(ApplyForceAfterDelay(rbToThrow, finalDirection, finalForce));
    }

    System.Collections.IEnumerator ApplyForceAfterDelay(Rigidbody rb, Vector3 direction, float force)
    {
        // Esperar un frame para que el sistema de interacción suelte completamente
        yield return null;

        if (rb != null)
        {
            // Asegurar que la física está completamente activa
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // NO limpiar velocidades - mantener cualquier momento que tenga
            // rb.linearVelocity = Vector3.zero; // ❌ COMENTADO
            // rb.angularVelocity = Vector3.zero; // ❌ COMENTADO

            if (useVelocityMode)
            {
                // MODO VELOCIDAD: Aplicar velocidad directa
                Vector3 velocity = direction * force;
                rb.linearVelocity = velocity;

                Debug.Log($"🚀 Velocidad aplicada: {velocity.magnitude:F1} m/s en dirección {direction}");
            }
            else
            {
                // MODO FUERZA: Aplicar impulso
                rb.AddForce(direction * force, ForceMode.VelocityChange);
                Debug.Log($"🚀 Fuerza aplicada: {force:F1} N en dirección {direction}");
            }

            // Agregar spin basado en el giroscopio
            Vector3 torque = smoothedGyro * force * 0.1f;
            rb.AddTorque(torque, ForceMode.VelocityChange);

            Debug.Log($"🎯 Objeto lanzado - Velocidad final: {rb.linearVelocity.magnitude:F2} m/s");
        }

        ResetGestureState();
    }

    void ResetGestureState()
    {
        isHoldingObject = false;
        isInThrowGesture = false;
        currentGrabbedObject = null;
        currentObjectRb = null;
        accelHistory.Clear();
        gyroHistory.Clear();
        gestureStartTime = 0f;
        peakAcceleration = Vector3.zero;
    }

    void DrawDebugInfo()
    {
        if (!isHoldingObject) return;

        // Visualizar dirección de lanzamiento
        if (currentGrabbedObject != null)
        {
            Vector3 startPos = currentGrabbedObject.transform.position;
            
            // Mostrar siempre la dirección actual del acelerómetro (CYAN)
            Vector3 currentPhoneDir = TransformPhoneToWorld(smoothedAccel).normalized;
            Debug.DrawRay(startPos, currentPhoneDir * 2f, Color.cyan, 0f);

            // Mostrar dirección de la cámara VR (AZUL)
            if (vrCamera != null)
            {
                Debug.DrawRay(startPos, vrCamera.forward * 2.5f, Color.blue, 0f);
            }

            if (isInThrowGesture)
            {
                // Mostrar la dirección de lanzamiento combinada (AMARILLO)
                Debug.DrawRay(startPos, throwDirection * 3f, Color.yellow, 0f);

                if (drawTrajectoryPreview)
                {
                    float estimatedForce = Mathf.Clamp(
                        peakAcceleration.magnitude * forceMultiplier,
                        minThrowForce,
                        maxThrowForce
                    );
                    DrawTrajectoryPreview(startPos, throwDirection * estimatedForce);
                }
            }
        }
    }

    void DrawTrajectoryPreview(Vector3 start, Vector3 velocity)
    {
        int steps = 20;
        float timeStep = 0.1f;
        Vector3 gravity = Physics.gravity;

        Vector3 prevPos = start;
        for (int i = 1; i <= steps; i++)
        {
            float t = i * timeStep;
            Vector3 nextPos = start + velocity * t + 0.5f * gravity * t * t;
            Debug.DrawLine(prevPos, nextPos, Color.green, 0.1f);
            prevPos = nextPos;
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        int y = 100;
        int lineHeight = 30;
        int labelWidth = 600;

        // Estilo para texto más visible
        GUIStyle boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontSize = 18;
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.normal.textColor = Color.white;

        GUIStyle normalStyle = new GUIStyle(GUI.skin.label);
        normalStyle.fontSize = 16;
        normalStyle.normal.textColor = Color.white;

        // Header
        GUI.Label(new Rect(20, y, labelWidth, lineHeight), "═══ PHONE GESTURE THROW DEBUG ═══", boldStyle);
        y += lineHeight + 5;

        // Estado de la cámara VR
        normalStyle.normal.textColor = vrCamera != null ? Color.green : Color.red;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"👁️ Cámara VR: {(vrCamera != null ? vrCamera.name : "NO ASIGNADA")}", normalStyle);
        y += lineHeight;

        // Estado del objeto
        Color statusColor = isHoldingObject ? Color.green : Color.red;
        normalStyle.normal.textColor = statusColor;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"🖐️ Sosteniendo Objeto: {(isHoldingObject ? "SÍ" : "NO")}", normalStyle);
        y += lineHeight;

        if (isHoldingObject && currentGrabbedObject != null)
        {
            normalStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(40, y, labelWidth, lineHeight),
                $"   📦 {currentGrabbedObject.name}", normalStyle);
            y += lineHeight;
        }

        // Estado del gesto
        normalStyle.normal.textColor = isInThrowGesture ? Color.yellow : Color.white;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"🎯 Gesto Activo: {(isInThrowGesture ? "DETECTANDO" : "Esperando...")}", normalStyle);
        y += lineHeight + 5;

        // Configuración de dirección
        normalStyle.normal.textColor = Color.magenta;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"🎮 Influencia Cámara: {cameraInfluence * 100:F0}% | Gesto: {(1 - cameraInfluence) * 100:F0}%", normalStyle);
        y += lineHeight + 5;

        // Datos de aceleración (RAW)
        Vector3 rawAccel = sensorReceiver != null ? sensorReceiver.accelData : Vector3.zero;
        float rawAccelMag = rawAccel.magnitude;

        normalStyle.normal.textColor = rawAccelMag > accelThreshold ? Color.red : Color.white;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"📊 Aceleración RAW: {rawAccelMag:F2} m/s²", normalStyle);
        y += lineHeight;

        normalStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(40, y, labelWidth, lineHeight),
            $"   X:{rawAccel.x:F2}  Y:{rawAccel.y:F2}  Z:{rawAccel.z:F2}", normalStyle);
        y += lineHeight;

        // Datos de aceleración (SUAVIZADA)
        float smoothMag = smoothedAccel.magnitude;
        normalStyle.normal.textColor = smoothMag > accelThreshold ? Color.red : Color.green;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"🔄 Aceleración SUAVIZADA: {smoothMag:F2} m/s²", normalStyle);
        y += lineHeight;

        normalStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(40, y, labelWidth, lineHeight),
            $"   X:{smoothedAccel.x:F2}  Y:{smoothedAccel.y:F2}  Z:{smoothedAccel.z:F2}", normalStyle);
        y += lineHeight + 5;

        // Umbral de lanzamiento
        normalStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"⚡ Umbral Lanzamiento: {accelThreshold:F1} m/s²", normalStyle);
        y += lineHeight;

        // Barra de progreso hacia el umbral
        float progressToThreshold = Mathf.Clamp01(smoothMag / accelThreshold);
        DrawProgressBar(new Rect(40, y, 300, 20), progressToThreshold,
            smoothMag >= accelThreshold ? Color.red : Color.green);
        y += 25;

        // Datos de giroscopio
        Vector3 rawGyro = sensorReceiver != null ? sensorReceiver.gyroData : Vector3.zero;
        normalStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(20, y, labelWidth, lineHeight),
            $"🔄 Giroscopio: {rawGyro.magnitude:F2} rad/s", normalStyle);
        y += lineHeight;

        normalStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(40, y, labelWidth, lineHeight),
            $"   X:{rawGyro.x:F2}  Y:{rawGyro.y:F2}  Z:{rawGyro.z:F2}", normalStyle);
        y += lineHeight + 10;

        // Información del gesto en progreso
        if (isInThrowGesture)
        {
            normalStyle.normal.textColor = Color.yellow;
            boldStyle.normal.textColor = Color.yellow;

            float timeSinceStart = Time.time - gestureStartTime;

            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                $"⏱️ Tiempo Gesto: {timeSinceStart:F2}s / {gestureTimeWindow:F2}s", boldStyle);
            y += lineHeight;

            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                $"💪 Pico Aceleración: {peakAcceleration.magnitude:F2} m/s²", boldStyle);
            y += lineHeight;

            // Calcular fuerza estimada
            float estimatedForce = Mathf.Clamp(
                peakAcceleration.magnitude * forceMultiplier,
                minThrowForce,
                maxThrowForce
            );

            normalStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                $"🚀 Fuerza Estimada: {estimatedForce:F1} N", normalStyle);
            y += lineHeight;

            // Dirección de lanzamiento
            normalStyle.normal.textColor = Color.magenta;
            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                $"🎯 Dirección: {throwDirection}", normalStyle);
            y += lineHeight;

            // Indicadores de rayos de debug
            normalStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                "📍 Rayos: CYAN=Gesto | AZUL=Cámara | AMARILLO=Final", normalStyle);
        }
        else
        {
            normalStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(20, y, labelWidth, lineHeight),
                $"💡 Mueve rápido para iniciar gesto (>{accelThreshold:F1} m/s²)", normalStyle);
        }
    }

    void DrawProgressBar(Rect position, float progress, Color barColor)
    {
        // Fondo
        GUI.color = Color.black;
        GUI.DrawTexture(position, Texture2D.whiteTexture);

        // Barra de progreso
        GUI.color = barColor;
        Rect fillRect = new Rect(
            position.x + 2,
            position.y + 2,
            (position.width - 4) * progress,
            position.height - 4
        );
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        // Borde
        GUI.color = Color.white;
        GUI.Box(position, "");

        GUI.color = Color.white;
    }
}