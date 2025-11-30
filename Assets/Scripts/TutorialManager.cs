using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la pantalla de tutorial con instrucciones
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject connectionPanel;  // Panel principal de conexión
    [SerializeField] private GameObject tutorialPanel;    // Panel del tutorial
    
    [Header("Tutorial Image")]
    [SerializeField] private Texture2D instructionsImage; // Tu imagen con instrucciones (con texto incluido)
    
    [Header("Settings")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Referencias internas (se obtienen automáticamente)
    private RawImage tutorialImage;
    private Button openTutorialButton;
    private Button closeTutorialButton;
    
    void Start()
    {
        // Buscar componentes automáticamente dentro de los paneles
        FindComponents();
        
        // Configurar botones
        if (openTutorialButton != null)
        {
            openTutorialButton.onClick.AddListener(OpenTutorial);
            if (showDebugLogs)
                Debug.Log("[TutorialManager] ✓ Botón 'Tutorial' configurado");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] ⚠ No se encontró botón 'TutorialButton' en ConnectionPanel");
        }
        
        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.AddListener(CloseTutorial);
            if (showDebugLogs)
                Debug.Log("[TutorialManager] ✓ Botón 'Volver' configurado");
        }
        else
        {
            Debug.LogWarning("[TutorialManager] ⚠ No se encontró botón 'BackButton' en TutorialPanel");
        }
        
        // Configurar imagen de tutorial
        if (tutorialImage != null && instructionsImage != null)
        {
            tutorialImage.texture = instructionsImage;
            if (showDebugLogs)
                Debug.Log("[TutorialManager] ✓ Imagen de instrucciones cargada");
        }
        else
        {
            if (tutorialImage == null)
                Debug.LogWarning("[TutorialManager] ⚠ No se encontró RawImage en TutorialPanel");
            if (instructionsImage == null)
                Debug.LogWarning("[TutorialManager] ⚠ Falta asignar la imagen de instrucciones en el Inspector");
        }
        
        // Asegurarse que el tutorial esté cerrado al inicio
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        if (showDebugLogs)
            Debug.Log("[TutorialManager] Sistema de tutorial inicializado");
    }
    
    /// <summary>
    /// Busca automáticamente los componentes necesarios
    /// </summary>
    private void FindComponents()
    {
        // Buscar RawImage en TutorialPanel
        if (tutorialPanel != null)
        {
            tutorialImage = tutorialPanel.GetComponentInChildren<RawImage>();
            closeTutorialButton = FindButtonByName(tutorialPanel.transform, "BackButton");
        }
        
        // Buscar botón Tutorial en ConnectionPanel
        if (connectionPanel != null)
        {
            openTutorialButton = FindButtonByName(connectionPanel.transform, "TutorialButton");
        }
    }
    
    /// <summary>
    /// Busca un botón por nombre en los hijos
    /// </summary>
    private Button FindButtonByName(Transform parent, string buttonName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == buttonName)
            {
                return child.GetComponent<Button>();
            }
            
            // Buscar recursivamente en los hijos
            Button foundButton = FindButtonByName(child, buttonName);
            if (foundButton != null)
            {
                return foundButton;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Abre el panel de tutorial
    /// </summary>
    public void OpenTutorial()
    {
        if (showDebugLogs)
            Debug.Log("[TutorialManager] Abriendo tutorial");
        
        // Ocultar panel de conexión
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(false);
        }
        
        // Mostrar panel de tutorial
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Cierra el panel de tutorial y vuelve a conexión
    /// </summary>
    public void CloseTutorial()
    {
        if (showDebugLogs)
            Debug.Log("[TutorialManager] Cerrando tutorial");
        
        // Ocultar panel de tutorial
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        // Mostrar panel de conexión
        if (connectionPanel != null)
        {
            connectionPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// Cambia la imagen del tutorial (útil si tienes múltiples páginas)
    /// </summary>
    public void SetTutorialImage(Texture2D newImage)
    {
        if (tutorialImage != null && newImage != null)
        {
            tutorialImage.texture = newImage;
            
            if (showDebugLogs)
                Debug.Log("[TutorialManager] Imagen de tutorial actualizada");
        }
    }
}