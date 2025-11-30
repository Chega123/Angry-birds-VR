using UnityEngine;

public class TabletSartenController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera orthographicCamera;
    [SerializeField] private Transform sarten;
    
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private bool smoothMovement = true;
    
    [Header("Área de Movimiento")]
    [SerializeField] private Vector2 movementAreaSize = new Vector2(3f, 3f); // X(horizontal) y Y(vertical) en metros
    [SerializeField] private bool useInitialSartenPosition = true;
    [SerializeField] private bool invertX = true; // Invertir movimiento horizontal (izq/der)
    [SerializeField] private bool invertY = false; // Invertir movimiento vertical (arriba/abajo)
    
    [Header("Límites de Movimiento")]
    [SerializeField] private bool useBounds = true;
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject touchIndicator;
    
    [Header("Debug")]
    [SerializeField] private bool verboseDebug = true;
    
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private VRWebSocketServer server;
    private Vector3 initialSartenPosition;
    private Vector3 movementAreaCenter;
    
    void Start()
    {
        if (sarten == null)
        {
            Debug.LogError("[Sarten] ¡Asigna el sartén en el inspector!");
            enabled = false;
            return;
        }
        
        if (orthographicCamera == null)
        {
            Debug.LogError("[Sarten] ¡Asigna la cámara ortográfica en el inspector!");
            enabled = false;
            return;
        }
        
        // Guardar la posición inicial del sartén
        initialSartenPosition = sarten.position;
        
        // Usar la posición inicial del sartén como centro del área de movimiento
        if (useInitialSartenPosition)
        {
            movementAreaCenter = initialSartenPosition;
        }
        else
        {
            movementAreaCenter = Vector3.zero;
        }
        
        // Auto-calcular bounds basados en el área de movimiento
        if (useBounds)
        {
            minX = movementAreaCenter.x - movementAreaSize.x / 2f;
            maxX = movementAreaCenter.x + movementAreaSize.x / 2f;
            minY = movementAreaCenter.y - movementAreaSize.y / 2f;
            maxY = movementAreaCenter.y + movementAreaSize.y / 2f;
        }
        
        Debug.Log($"[Sarten] Configuración:");
        Debug.Log($"  - Posición inicial sartén: {initialSartenPosition}");
        Debug.Log($"  - Centro área movimiento: {movementAreaCenter}");
        Debug.Log($"  - Tamaño área: {movementAreaSize.x}(X) x {movementAreaSize.y}(Y) metros");
        Debug.Log($"  - Límites: X[{minX:F2}, {maxX:F2}], Y[{minY:F2}, {maxY:F2}]");
        Debug.Log($"  - Inversiones: X={invertX}, Y={invertY}");
        
        // Buscar el servidor
        server = FindObjectOfType<VRWebSocketServer>();
        if (server != null)
        {
            server.RegisterSartenController(this);
            Debug.Log("[Sarten] ✓ Registrado con VRWebSocketServer");
        }
        else
        {
            Debug.LogError("[Sarten] ✗ No se encontró VRWebSocketServer!");
        }
        
        targetPosition = sarten.position;
        
        if (touchIndicator != null)
        {
            touchIndicator.SetActive(false);
        }
    }
    
    void Update()
    {
        if (hasTarget && smoothMovement)
        {
            sarten.position = Vector3.Lerp(
                sarten.position, 
                targetPosition, 
                moveSpeed * Time.deltaTime
            );
        }
    }
    
    public void OnTabletTouch(TabletInput input)
    {
        if (verboseDebug)
        {
            Debug.Log($"[Sarten] ===== TOUCH RECIBIDO =====");
            Debug.Log($"[Sarten] Input raw: screenX={input.screenX:F3}, screenY={input.screenY:F3}");
        }
        
        // Aplicar inversiones
        float normalizedX = invertX ? (1f - input.screenX) : input.screenX;
        float normalizedY = invertY ? (1f - input.screenY) : input.screenY;
        
        if (verboseDebug)
        {
            Debug.Log($"[Sarten] Normalized (post-inversion): X={normalizedX:F3}, Y={normalizedY:F3}");
        }
        
        // MAPEO CORRECTO PARA VISTA LATERAL:
        // screenX (0→1) → worldX (horizontal, izquierda→derecha)
        // screenY (0→1) → worldY (vertical en pantalla, arriba→abajo = altura real)
        
        float worldX = Mathf.Lerp(
            movementAreaCenter.x - movementAreaSize.x / 2f,
            movementAreaCenter.x + movementAreaSize.x / 2f,
            normalizedX
        );
        
        float worldY = Mathf.Lerp(
            movementAreaCenter.y - movementAreaSize.y / 2f,
            movementAreaCenter.y + movementAreaSize.y / 2f,
            normalizedY
        );
        
        // Mantener Z constante (profundidad)
        float worldZ = movementAreaCenter.z;
        
        Vector3 worldPos = new Vector3(worldX, worldY, worldZ);
        
        if (verboseDebug)
        {
            Debug.Log($"[Sarten] World pos (sin bounds): ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
            Debug.Log($"[Sarten] Delta desde centro: ΔX={worldPos.x - movementAreaCenter.x:F2}, ΔY={worldPos.y - movementAreaCenter.y:F2}");
        }
        
        // Aplicar límites
        if (useBounds)
        {
            worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
            worldPos.y = Mathf.Clamp(worldPos.y, minY, maxY);
            
            if (verboseDebug)
            {
                Debug.Log($"[Sarten] World pos (con bounds): ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
            }
        }
        
        targetPosition = worldPos;
        hasTarget = true;
        
        // Movimiento instantáneo o suave
        if (!smoothMovement)
        {
            sarten.position = targetPosition;
        }
        
        if (verboseDebug)
        {
            Debug.Log($"[Sarten] ✓ Target final: ({targetPosition.x:F2}, {targetPosition.y:F2}, {targetPosition.z:F2})");
        }
        
        // Visual feedback
        if (touchIndicator != null)
        {
            if (input.action == "Began")
            {
                touchIndicator.SetActive(true);
                touchIndicator.transform.position = worldPos;
            }
            else if (input.action == "Ended")
            {
                touchIndicator.SetActive(false);
            }
            else if (input.action == "Moved")
            {
                touchIndicator.transform.position = worldPos;
            }
        }
    }
    
    public Vector3 GetSartenPosition()
    {
        return sarten != null ? sarten.position : Vector3.zero;
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 center = useInitialSartenPosition && sarten != null 
            ? sarten.position
            : Vector3.zero;
        
        // Dibujar el área de movimiento (cyan) - ahora en plano XY
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(movementAreaSize.x, movementAreaSize.y, 0.05f);
        Gizmos.DrawWireCube(center, size);
        
        // Dibujar límites (amarillo) si está en play mode
        if (useBounds && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            float boundsMinX = center.x - movementAreaSize.x / 2f;
            float boundsMaxX = center.x + movementAreaSize.x / 2f;
            float boundsMinY = center.y - movementAreaSize.y / 2f;
            float boundsMaxY = center.y + movementAreaSize.y / 2f;
            
            Vector3 boundsCenter = new Vector3((boundsMinX + boundsMaxX) / 2f, (boundsMinY + boundsMaxY) / 2f, center.z);
            Vector3 boundsSize = new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, 0.1f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
        
        // Dibujar posición del sartén (verde)
        if (sarten != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sarten.position, 0.3f);
            
            // Dibujar ejes de referencia
            Gizmos.color = Color.red; // Eje X (horizontal)
            Gizmos.DrawLine(sarten.position, sarten.position + Vector3.right * 0.5f);
            Gizmos.color = Color.green; // Eje Y (vertical/altura)
            Gizmos.DrawLine(sarten.position, sarten.position + Vector3.up * 0.5f);
            Gizmos.color = Color.blue; // Eje Z (profundidad - constante)
            Gizmos.DrawLine(sarten.position, sarten.position + Vector3.forward * 0.5f);
        }
    }
}