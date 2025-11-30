using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StaticZone : MonoBehaviour
{
    [Tooltip("Si está activo, los objetos dentro de esta zona quedan totalmente estáticos.")]
    public bool freezeObjects = true;

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null && freezeObjects)
        {
            // Desactiva la gravedad
            rb.useGravity = false;

            // Mantén la posición fija cancelando velocidad y rotación
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            // Cuando sale de la zona, vuelve a la normalidad
            rb.useGravity = true;
        }
    }
}
