using UnityEngine;

public class TabletInteractable : MonoBehaviour
{
    [SerializeField] private bool lockZAxis = true;
    [SerializeField] private Color highlightColor = Color.yellow;
    
    private Color originalColor;
    private Renderer objectRenderer;
    private Vector3 originalZPosition;
    
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
        
        originalZPosition = transform.position;
    }
    
    public void OnTabletInteract(TabletInput input)
    {
        switch (input.action)
        {
            case "Began":
                OnTouchStart(input);
                break;
            case "Moved":
                OnDrag(input);
                break;
            case "Ended":
            case "Canceled":
                OnTouchEnd(input);
                break;
        }
    }
    
    private void OnTouchStart(TabletInput input)
    {
        Debug.Log($"Objeto {gameObject.name} tocado desde tablet");
        
        // Highlight visual
        if (objectRenderer != null)
        {
            objectRenderer.material.color = highlightColor;
        }
    }
    
    private void OnDrag(TabletInput input)
    {
        // Aquí puedes implementar lógica de arrastre
        // Por ahora solo mostramos que está siendo arrastrado
        
        if (lockZAxis)
        {
            // Mantener Z fijo
            Vector3 newPos = transform.position;
            newPos.z = originalZPosition.z;
            transform.position = newPos;
        }
    }
    
    private void OnTouchEnd(TabletInput input)
    {
        Debug.Log($"Touch liberado en {gameObject.name}");
        
        // Restaurar color original
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }
    }
}

