using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Maneja un List Item Button con validación de conexión
/// Específico para botones dentro de listas UI (Scroll View, etc.)
/// </summary>
public class ListItemButtonHandler : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    [SerializeField] private VRWebSocketServer serverReference;
    
    [Header("Acciones del Botón")]
    [SerializeField] private XRTeleporter teleporter;
    [SerializeField] private ScaleZone scaleZone;
    
    [Header("Componentes del List Item")]
    [SerializeField] private Button button;
    [SerializeField] private Image buttonImage; // Imagen del botón
    [SerializeField] private TextMeshProUGUI buttonText; // Texto del botón
    
    [Header("Visual Feedback")]
    [SerializeField] private Color enabledColor = new Color(0.2f, 0.8f, 0.2f); // Verde
    [SerializeField] private Color disabledColor = new Color(0.8f, 0.2f, 0.2f); // Rojo
    [SerializeField] private string enabledText = "LANZAR";
    [SerializeField] private string disabledText = "ESPERANDO TABLET...";
    
    [Header("Lock Visual (Opcional)")]
    [SerializeField] private GameObject lockIcon;
    
    [Header("Configuración")]
    [SerializeField] private float cooldownTime = 1f;
    [SerializeField] private bool showDebugLogs = true;
    
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private Color originalButtonColor;
    
    void Start()
    {
        // Buscar referencias automáticamente
        if (serverReference == null)
        {
            serverReference = FindObjectOfType<VRWebSocketServer>();
            if (serverReference != null && showDebugLogs)
                Debug.Log("[ListItemButton] ✓ Server encontrado automáticamente");
        }
        
        if (button == null)
            button = GetComponent<Button>();
        
        if (buttonImage == null && button != null)
            buttonImage = button.GetComponent<Image>();
        
        if (buttonImage != null)
            originalButtonColor = buttonImage.color;
        
        // Buscar texto en hijos si no está asignado
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        
        // IMPORTANTE: Interceptar el click del botón
        if (button != null)
        {
            // Remover listeners anteriores
            button.onClick.RemoveAllListeners();
            
            // Agregar nuestro listener con validación
            button.onClick.AddListener(OnButtonClicked);
        }
        
        UpdateVisuals();
    }
    
    void Update()
    {
        // Actualizar cooldown
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
            }
        }
        
        // Actualizar visuales
        UpdateVisuals();
    }
    
    /// <summary>
    /// Verifica si la tablet está conectada
    /// </summary>
    private bool IsTabletConnected()
    {
        if (serverReference == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ListItemButton] Server reference is null");
            return false;
        }
        
        return serverReference.IsClientConnected;
    }
    
    /// <summary>
    /// Verifica si el botón puede ejecutarse
    /// </summary>
    private bool CanExecute()
    {
        return IsTabletConnected() && !isOnCooldown;
    }
    
    /// <summary>
    /// Método que se ejecuta al hacer click en el botón
    /// Este reemplaza las funciones OnClick del Button
    /// </summary>
    private void OnButtonClicked()
    {
        if (showDebugLogs)
            Debug.Log($"[ListItemButton] Botón '{gameObject.name}' presionado");
        
        // VALIDAR PRIMERO
        if (!CanExecute())
        {
            if (showDebugLogs)
            {
                string reason = !IsTabletConnected() ? "Tablet no conectada" : "En cooldown";
                Debug.LogWarning($"[ListItemButton] ❌ Acción bloqueada: {reason}");
            }
            
            // Animación de rechazo
            StartCoroutine(ShakeAnimation());
            return;
        }
        
        // ✅ TABLET CONECTADA - Ejecutar acciones
        if (showDebugLogs)
            Debug.Log("[ListItemButton] ✓ Validación exitosa - Ejecutando...");
        
        // Iniciar cooldown
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
        
        // Ejecutar teletransporte
        if (teleporter != null)
        {
            if (showDebugLogs)
                Debug.Log("[ListItemButton] 📍 Ejecutando teletransporte");
            teleporter.Teleport();
        }
        
        // Ejecutar refresh de clones
        if (scaleZone != null)
        {
            if (showDebugLogs)
                Debug.Log("[ListItemButton] 🔄 Refrescando clones");
            scaleZone.RefreshClones();
        }
        
        // Animación de éxito
        StartCoroutine(SuccessAnimation());
    }
    
    /// <summary>
    /// Actualiza los visuales del botón
    /// </summary>
    private void UpdateVisuals()
    {
        bool canExecute = CanExecute();
        bool isConnected = IsTabletConnected();
        
        // Actualizar color del botón
        if (buttonImage != null)
        {
            Color targetColor = canExecute ? enabledColor : disabledColor;
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * 10f);
        }
        
        // Actualizar texto
        if (buttonText != null)
        {
            if (isOnCooldown)
            {
                buttonText.text = $"Espera {Mathf.Ceil(cooldownTimer)}s";
            }
            else if (!isConnected)
            {
                buttonText.text = disabledText;
            }
            else
            {
                buttonText.text = enabledText;
            }
        }
        
        // Mostrar/ocultar lock
        if (lockIcon != null)
        {
            lockIcon.SetActive(!canExecute);
        }
        
        // El botón siempre está "interactable" visualmente,
        // pero la validación se hace en OnButtonClicked
        if (button != null)
        {
            button.interactable = true;
        }
    }
    
    /// <summary>
    /// Animación de éxito al presionar
    /// </summary>
    private System.Collections.IEnumerator SuccessAnimation()
    {
        Vector3 originalScale = transform.localScale;
        
        // Encoger
        transform.localScale = originalScale * 0.9f;
        yield return new WaitForSeconds(0.1f);
        
        // Volver a tamaño normal
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// Animación de rechazo (shake)
    /// </summary>
    private System.Collections.IEnumerator ShakeAnimation()
    {
        Vector3 originalPos = transform.localPosition;
        float shakeAmount = 5f;
        
        for (int i = 0; i < 3; i++)
        {
            transform.localPosition = originalPos + Vector3.right * shakeAmount;
            yield return new WaitForSeconds(0.05f);
            transform.localPosition = originalPos - Vector3.right * shakeAmount;
            yield return new WaitForSeconds(0.05f);
        }
        
        transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// Método público para forzar la ejecución (sin validación)
    /// Úsalo solo para testing
    /// </summary>
    public void ForceExecute()
    {
        if (teleporter != null)
            teleporter.Teleport();
        
        if (scaleZone != null)
            scaleZone.RefreshClones();
    }
}