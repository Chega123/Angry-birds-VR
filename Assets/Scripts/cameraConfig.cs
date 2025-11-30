using UnityEngine;

// INSTRUCCIONES:
// 1. Crea este script en tu proyecto
// 2. Agrégalo a tu cámara VR (chefCamera o soldadoCamera)
// 3. Configura los valores en el Inspector
// 4. ¡Listo!

public class CameraAspectSimple : MonoBehaviour
{
    [Header("¿Qué quieres hacer?")]
    [Tooltip("Marca esto para forzar un aspecto específico (16:9, 4:3, etc)")]
    [SerializeField] private bool forzarAspecto = false;
    
    [Header("Configuración de Aspecto")]
    [Tooltip("Aspecto deseado: 1.77=16:9, 1.33=4:3, 1.0=cuadrado")]
    [SerializeField] private float aspectoDeseado = 1.33f; // 4:3 por defecto (640x480)
    
    [Header("O elige un preset:")]
    [SerializeField] private Preset presetAspecto = Preset.Aspecto_4_3;
    
    public enum Preset
    {
        NoUsar,
        Aspecto_16_9,   // 1.77 (moderno)
        Aspecto_4_3,    // 1.33 (tu resolución 640x480)
        Aspecto_21_9,   // 2.33 (ultrawide)
        Cuadrado_1_1,   // 1.0
        Vertical_9_16   // 0.56 (móvil vertical)
    }
    
    private Camera cam;
    private Camera camaraFondo;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (!forzarAspecto)
        {
            Debug.Log("[Aspect] No se forzará aspecto, usando pantalla completa");
            return;
        }
        
        // Aplicar preset si está seleccionado
        if (presetAspecto != Preset.NoUsar)
        {
            AplicarPreset();
        }
        
        AplicarAspecto();
    }
    
    private void AplicarPreset()
    {
        switch (presetAspecto)
        {
            case Preset.Aspecto_16_9:
                aspectoDeseado = 16f / 9f; // 1.77
                break;
            case Preset.Aspecto_4_3:
                aspectoDeseado = 4f / 3f; // 1.33
                break;
            case Preset.Aspecto_21_9:
                aspectoDeseado = 21f / 9f; // 2.33
                break;
            case Preset.Cuadrado_1_1:
                aspectoDeseado = 1f;
                break;
            case Preset.Vertical_9_16:
                aspectoDeseado = 9f / 16f; // 0.56
                break;
        }
        
        Debug.Log($"[Aspect] Preset aplicado: {presetAspecto} = {aspectoDeseado:F2}");
    }
    
    private void AplicarAspecto()
    {
        float aspectoActual = (float)Screen.width / Screen.height;
        
        // Si ya coincide, no hacer nada
        if (Mathf.Abs(aspectoActual - aspectoDeseado) < 0.01f)
        {
            cam.rect = new Rect(0, 0, 1, 1);
            Debug.Log("[Aspect] Aspecto ya coincide, usando pantalla completa");
            return;
        }
        
        // Crear cámara de fondo negro (para las barras)
        CrearCamaraFondo();
        
        // Calcular viewport
        if (aspectoActual > aspectoDeseado)
        {
            // Pantalla MÁS ANCHA → barras a los LADOS
            float ancho = aspectoDeseado / aspectoActual;
            float x = (1f - ancho) / 2f;
            cam.rect = new Rect(x, 0, ancho, 1);
            Debug.Log($"[Aspect] Barras laterales: ancho={ancho:F2}");
        }
        else
        {
            // Pantalla MÁS ALTA → barras ARRIBA/ABAJO
            float alto = aspectoActual / aspectoDeseado;
            float y = (1f - alto) / 2f;
            cam.rect = new Rect(0, y, 1, alto);
            Debug.Log($"[Aspect] Barras horizontales: alto={alto:F2}");
        }
    }
    
    private void CrearCamaraFondo()
    {
        if (camaraFondo != null) return;
        
        GameObject obj = new GameObject("CameraFondo_BarrasNegras");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        
        camaraFondo = obj.AddComponent<Camera>();
        camaraFondo.depth = cam.depth - 1; // Renderizar ANTES
        camaraFondo.clearFlags = CameraClearFlags.SolidColor;
        camaraFondo.backgroundColor = Color.black;
        camaraFondo.cullingMask = 0; // No renderiza objetos
        camaraFondo.orthographic = cam.orthographic;
        
        Debug.Log("[Aspect] ✓ Cámara de fondo creada (barras negras)");
    }
    
    // Para ver cambios en tiempo real en el Editor
    void OnValidate()
    {
        if (Application.isPlaying && forzarAspecto)
        {
            if (presetAspecto != Preset.NoUsar)
            {
                AplicarPreset();
            }
            AplicarAspecto();
        }
    }
}