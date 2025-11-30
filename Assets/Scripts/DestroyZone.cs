using UnityEngine;

public class DestroyZone : MonoBehaviour
{
    [Tooltip("Etiqueta de los objetos que se pueden destruir. Si está vacío, destruirá todo lo que entre.")]
    public string targetTag = "Scalable";

    [Tooltip("Si está activado, destruirá inmediatamente los objetos al entrar.")]
    public bool destroyOnEnter = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si no tiene tag definido o coincide con el objeto
        if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
        {
            if (destroyOnEnter)
            {
                Destroy(other.gameObject);
                Debug.Log($"Objeto destruido: {other.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Destruye todos los objetos que estén dentro de la zona en este momento
    /// </summary>
    public void DestroyAllInZone()
    {
        Collider[] objectsInZone = Physics.OverlapBox(
            transform.position,
            transform.localScale / 2, // Tamaño basado en la escala del objeto
            transform.rotation
        );

        foreach (Collider col in objectsInZone)
        {
            if (col.gameObject != this.gameObject) // No destruir la zona
            {
                if (string.IsNullOrEmpty(targetTag) || col.CompareTag(targetTag))
                {
                    Destroy(col.gameObject);
                    Debug.Log($"Objeto destruido: {col.gameObject.name}");
                }
            }
        }
    }

    // Debug visual para ver la zona en la escena
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
