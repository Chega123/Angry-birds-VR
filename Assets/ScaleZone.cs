using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScaleZone : MonoBehaviour
{
    [Tooltip("Zona donde aparecerán los objetos escalados (ej: en el mundo grande).")]
    public Transform targetZone;

    [Tooltip("Factor de escala entre maqueta y gigante.")]
    public float scaleMultiplier = 10f;

    [Tooltip("Tiempo en segundos antes de reactivar la física tras clonar.")]
    public float physicsEnableDelay = 0.3f;

    // Objetos detectados dentro de la mesa
    private HashSet<GameObject> objectsInZone = new();

    // Diccionarios para control interno
    private Dictionary<int, GameObject> clones = new();
    private Dictionary<int, Rigidbody> cloneRigidbodies = new();

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Scalable")) return;

        if (!objectsInZone.Contains(other.gameObject))
        {
            objectsInZone.Add(other.gameObject);
            Debug.Log($"[OnTriggerEnter] {other.gameObject.name} listo para clonarse.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Scalable")) return;

        int id = other.gameObject.GetInstanceID();
        objectsInZone.Remove(other.gameObject);

        if (clones.ContainsKey(id))
        {
            Destroy(clones[id]);
            clones.Remove(id);
            cloneRigidbodies.Remove(id);
            Debug.Log($"[OnTriggerExit] Clon eliminado para: {other.gameObject.name}");
        }
    }

    /// <summary>
    /// Elimina todos los clones actuales y vuelve a clonar
    /// los objetos que estén en la mesa actual.
    /// </summary>
    public void RefreshClones()
    {
        Debug.Log("🔄 Refrescando clones: eliminando y clonando todo...");

        // 1. Eliminar clones antiguos
        foreach (var clone in clones.Values)
        {
            if (clone != null)
                Destroy(clone);
        }
        clones.Clear();
        cloneRigidbodies.Clear();

        // 2. Clonar nuevamente
        foreach (var obj in objectsInZone)
        {
            if (obj == null) continue;

            int id = obj.GetInstanceID();

            GameObject clone = Instantiate(obj, targetZone);

            // Ajustar escala
            clone.transform.localScale = obj.transform.localScale * scaleMultiplier;

            // Asegurar Rigidbody
            Rigidbody cloneRb = clone.GetComponent<Rigidbody>();
            if (cloneRb == null)
                cloneRb = clone.AddComponent<Rigidbody>();

            // Física temporalmente desactivada para evitar explosiones
            cloneRb.isKinematic = true;
            cloneRb.useGravity = true;

            // Guardar en diccionarios
            clones[id] = clone;
            cloneRigidbodies[id] = cloneRb;

            // Posicionar correctamente
            Vector3 localPos = transform.InverseTransformPoint(obj.transform.position);
            clone.transform.position = targetZone.TransformPoint(localPos * scaleMultiplier);
            clone.transform.rotation = obj.transform.rotation;

            Debug.Log($"[RefreshClones] Clon creado para: {obj.name}");
        }

        // 3. Activar la física luego de un retraso
        StartCoroutine(EnablePhysicsAfterDelay());
    }

    /// <summary>
    /// Activa la física gradualmente después de un retraso
    /// para evitar explosiones iniciales.
    /// </summary>
    private IEnumerator EnablePhysicsAfterDelay()
    {
        yield return new WaitForSeconds(physicsEnableDelay);

        foreach (var rb in cloneRigidbodies.Values)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }

        Debug.Log("⚙️ Física activada en clones sin explosiones.");
    }
}
