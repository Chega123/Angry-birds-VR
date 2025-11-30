using System.Collections;
using UnityEngine;

public class XRRigMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public GameObject targetObject; // El GameObject hacia donde se moverá
    public float elevationHeight = 5f; // Altura a la que se elevará
    public float elevationSpeed = 2f; // Velocidad de elevación
    public float moveSpeed = 3f; // Velocidad de movimiento horizontal
    public bool startOnAwake = false; // Si debe iniciar automáticamente
    
    [Header("XR Rig Components")]
    public Transform xrRigTransform; // El XR Rig principal
    
    private bool isMoving = false;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    
    void Start()
    {
        // Si no se asigna manualmente, buscar el XR Rig en este GameObject
        if (xrRigTransform == null)
            xrRigTransform = transform;
            
        originalPosition = xrRigTransform.position;
        
        if (startOnAwake && targetObject != null)
        {
            StartMovement();
        }
    }
    
    /// <summary>
    /// Inicia el proceso de movimiento: primero eleva, luego mueve horizontalmente
    /// </summary>
    public void StartMovement()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target Object no está asignado!");
            return;
        }
        
        if (isMoving)
        {
            Debug.LogWarning("El XR Rig ya se está moviendo!");
            return;
        }
        
        StartCoroutine(MoveToTarget());
    }
    
    /// <summary>
    /// Resetea la posición del XR Rig a su posición original
    /// </summary>
    public void ResetPosition()
    {
        if (isMoving)
        {
            StopAllCoroutines();
            isMoving = false;
        }
        
        xrRigTransform.position = originalPosition;
    }
    
    /// <summary>
    /// Corrutina que maneja el movimiento completo
    /// </summary>
    private IEnumerator MoveToTarget()
    {
        isMoving = true;
        
        // Fase 1: Elevar hacia el cielo
        Vector3 elevatedPosition = new Vector3(
            xrRigTransform.position.x, 
            xrRigTransform.position.y + elevationHeight, 
            xrRigTransform.position.z
        );
        
        yield return StartCoroutine(MoveToPosition(elevatedPosition, elevationSpeed));
        
        // Pequeña pausa entre movimientos (opcional)
        yield return new WaitForSeconds(0.5f);
        
        // Fase 2: Mover horizontalmente hacia el target (manteniendo la altura elevada)
        targetPosition = new Vector3(
            targetObject.transform.position.x,
            elevatedPosition.y, // Mantener la altura elevada
            targetObject.transform.position.z
        );
        
        yield return StartCoroutine(MoveToPosition(targetPosition, moveSpeed));
        
        // Fase 3: Bajar a la altura del target object
        Vector3 finalPosition = new Vector3(
            targetObject.transform.position.x,
            targetObject.transform.position.y,
            targetObject.transform.position.z
        );
        
        yield return StartCoroutine(MoveToPosition(finalPosition, elevationSpeed));
        
        isMoving = false;
        Debug.Log("XR Rig ha llegado al destino!");
    }
    
    /// <summary>
    /// Corrutina auxiliar para mover suavemente a una posición específica
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 destination, float speed)
    {
        Vector3 startPosition = xrRigTransform.position;
        float journey = 0f;
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * speed;
            xrRigTransform.position = Vector3.Lerp(startPosition, destination, journey);
            yield return null;
        }
        
        // Asegurar que llegue exactamente a la posición
        xrRigTransform.position = destination;
    }
    
    /// <summary>
    /// Método público para mover directamente sin elevación previa
    /// </summary>
    public void MoveDirectlyToTarget()
    {
        if (targetObject == null) return;
        
        StartCoroutine(MoveToPosition(targetObject.transform.position, moveSpeed));
    }
    
    /// <summary>
    /// Método para cambiar el target object en tiempo de ejecución
    /// </summary>
    public void SetTargetObject(GameObject newTarget)
    {
        targetObject = newTarget;
    }
}