using UnityEngine;
using TMPro;
using System.Collections;

public class SceneModeManager : MonoBehaviour
{
    [Header("Objetos del Modo Chef")]
    [SerializeField] private GameObject[] chefObjects; // Sartén, cámara chef, etc.
    [SerializeField] private TabletSartenController sartenController;
    
    [Header("Objetos del Modo Soldado")]
    [SerializeField] private GameObject[] soldadoObjects; // Pájaros, spawner, cámara soldado, etc.
    [SerializeField] private TabletSoldadoController soldadoController;
    
    [Header("UI")]
    [SerializeField] private TMP_Text modeText;
    
    [Header("Settings")]
    [SerializeField] private bool checkModeRepeatedly = true;
    [SerializeField] private float checkInterval = 0.5f; // Revisar cada 0.5 segundos
    
    void Start()
    {
        Debug.Log("[SceneMode] SceneModeManager iniciado");
        
        // Configurar escena inicial
        SetupScene();
        
        // Si está activado, revisar el modo repetidamente
        if (checkModeRepeatedly)
        {
            StartCoroutine(CheckModeRoutine());
        }
    }
    
    private IEnumerator CheckModeRoutine()
    {
        GameMode lastMode = GameMode.None;
        
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            GameMode currentMode = GameModeManager.CurrentMode;
            
            // Si el modo cambió, reconfigurar la escena
            if (currentMode != lastMode && currentMode != GameMode.None)
            {
                Debug.Log($"[SceneMode] Modo cambió de {lastMode} a {currentMode}");
                SetupScene();
                lastMode = currentMode;
            }
        }
    }
    
    private void SetupScene()
    {
        GameMode currentMode = GameModeManager.CurrentMode;
        
        // Si no hay modo seleccionado, usar Chef por defecto
        if (currentMode == GameMode.None)
        {
            Debug.LogWarning("[SceneMode] No se seleccionó modo. Usando Chef por defecto.");
            currentMode = GameMode.Chef;
            GameModeManager.CurrentMode = GameMode.Chef;
        }
        
        Debug.Log($"[SceneMode] ===== CONFIGURANDO ESCENA PARA MODO: {currentMode} =====");
        
        // Activar/desactivar objetos según el modo
        bool isChef = currentMode == GameMode.Chef;
        bool isSoldado = currentMode == GameMode.Soldado;
        
        // Modo Chef
        Debug.Log($"[SceneMode] Configurando objetos del Chef (activar={isChef}):");
        foreach (var obj in chefObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isChef);
                Debug.Log($"  - {obj.name}: {(isChef ? "ACTIVO" : "INACTIVO")}");
            }
            else
            {
                Debug.LogWarning("  - [NULL OBJECT en chefObjects]");
            }
        }
        
        if (sartenController != null)
        {
            sartenController.enabled = isChef;
            Debug.Log($"[SceneMode] TabletSartenController: {(isChef ? "ENABLED" : "DISABLED")}");
        }
        else
        {
            Debug.LogWarning("[SceneMode] sartenController es NULL!");
        }
        
        // Modo Soldado
        Debug.Log($"[SceneMode] Configurando objetos del Soldado (activar={isSoldado}):");
        foreach (var obj in soldadoObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isSoldado);
                Debug.Log($"  - {obj.name}: {(isSoldado ? "ACTIVO" : "INACTIVO")}");
            }
            else
            {
                Debug.LogWarning("  - [NULL OBJECT en soldadoObjects]");
            }
        }
        
        if (soldadoController != null)
        {
            soldadoController.enabled = isSoldado;
            Debug.Log($"[SceneMode] TabletSoldadoController: {(isSoldado ? "ENABLED" : "DISABLED")}");
        }
        else
        {
            Debug.LogWarning("[SceneMode] soldadoController es NULL!");
        }
        
        // Actualizar UI
        if (modeText != null)
        {
            modeText.text = $"Modo: {currentMode}";
            Debug.Log($"[SceneMode] UI actualizado: Modo {currentMode}");
        }
        
        Debug.Log($"[SceneMode] ===== CONFIGURACIÓN COMPLETADA =====");
    }
    
    // Método público para forzar actualización (útil para debug)
    public void ForceUpdateScene()
    {
        Debug.Log("[SceneMode] Forzando actualización de escena...");
        SetupScene();
    }
    
    // Método para cambiar de modo manualmente (útil para debug)
    public void SwitchToChef()
    {
        Debug.Log("[SceneMode] Cambiando a modo Chef...");
        GameModeManager.CurrentMode = GameMode.Chef;
        SetupScene();
    }
    
    public void SwitchToSoldado()
    {
        Debug.Log("[SceneMode] Cambiando a modo Soldado...");
        GameModeManager.CurrentMode = GameMode.Soldado;
        SetupScene();
    }
    
    // Debug: Mostrar estado actual
    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Modo Actual: {GameModeManager.CurrentMode}");
            
            if (GUI.Button(new Rect(10, 40, 150, 30), "Switch to Chef"))
            {
                SwitchToChef();
            }
            
            if (GUI.Button(new Rect(170, 40, 150, 30), "Switch to Soldado"))
            {
                SwitchToSoldado();
            }
            
            if (GUI.Button(new Rect(330, 40, 150, 30), "Force Update"))
            {
                ForceUpdateScene();
            }
        }
    }
}