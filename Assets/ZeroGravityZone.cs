using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ZeroGravityZone : MonoBehaviour
{
    [Header("Configuración de Gravedad")]
    [Tooltip("Si está activo, cancela la gravedad de cualquier objeto con Rigidbody que entre en la zona.")]
    public bool disableGravity = true;

    [Header("Control de Activación")]
    [Tooltip("Si está activada, la zona de gravedad cero está funcionando")]
    public bool zoneActive = true;

    [Header("Visual (Opcional)")]
    [Tooltip("Renderer para cambiar color cuando se active/desactive (opcional)")]
    public Renderer zoneRenderer;
    
    [Tooltip("Color cuando la zona está activa")]
    public Color activeColor = Color.green;
    
    [Tooltip("Color cuando la zona está inactiva")]
    public Color inactiveColor = Color.red;

    // Lista de objetos dentro de la zona
    private HashSet<Rigidbody> objectsInZone = new HashSet<Rigidbody>();

    private void Start()
    {
        UpdateVisual();
    }

    // Método público para activar/desactivar la zona
    public void ToggleZone()
    {
        zoneActive = !zoneActive;
        
        // Si desactivamos la zona, restaurar gravedad a todos los objetos dentro
        if (!zoneActive)
        {
            RestoreGravityToAll();
        }
        
        UpdateVisual();
        Debug.Log($"Zona de gravedad cero: {(zoneActive ? "ACTIVADA" : "DESACTIVADA")}");
    }

    // Métodos para activar/desactivar directamente
    public void ActivateZone()
    {
        zoneActive = true;
        UpdateVisual();
    }

    public void DeactivateZone()
    {
        zoneActive = false;
        RestoreGravityToAll();
        UpdateVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            objectsInZone.Add(rb);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Solo aplicar efecto si la zona está activa
        if (!zoneActive) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            // Desactivar la gravedad
            rb.useGravity = false;

            // Cancelar cualquier velocidad hacia abajo para evitar que se "hunda"
            Vector3 vel = rb.linearVelocity;
            if (vel.y < 0) vel.y = 0;
            rb.linearVelocity = vel;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            objectsInZone.Remove(rb);
            
            // Cuando el objeto sale de la zona, vuelve a la gravedad normal
            rb.useGravity = true;
        }
    }

    // Restaurar gravedad a todos los objetos dentro de la zona
    private void RestoreGravityToAll()
    {
        foreach (Rigidbody rb in objectsInZone)
        {
            if (rb != null)
            {
                rb.useGravity = true;
            }
        }
    }

    // Actualizar el visual de la zona (opcional)
    private void UpdateVisual()
    {
        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = zoneActive ? activeColor : inactiveColor;
        }
    }

    // Método para usar con botones VR o eventos
    public bool IsZoneActive()
    {
        return zoneActive;
    }

    #if UNITY_EDITOR
    // Visualización en el editor
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = zoneActive ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            
            if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius * transform.localScale.x);
            }
            else if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
    #endif
}