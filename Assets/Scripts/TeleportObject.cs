using UnityEngine;

public class TeleportObject : MonoBehaviour
{
    [Header("Configuración de Teletransporte")]
    [Tooltip("El objeto que se va a mover (ya existe en la escena).")]
    public GameObject currentObject;

    [Tooltip("Punto donde se teletransportará el objeto.")]
    public Transform spawnPoint;

    [Tooltip("Si es verdadero, también rotará el objeto según el spawnPoint.")]
    public bool matchRotation = true;

    [Tooltip("Tiempo de espera antes de reactivar la física (segundos).")]
    public float physicsEnableDelay = 0.5f;

    private Rigidbody[] objectRigidbodies;

    private void Start()
    {
        if (currentObject != null)
        {
            // Guardamos todos los Rigidbodies del objeto
            objectRigidbodies = currentObject.GetComponentsInChildren<Rigidbody>();
        }
    }

    /// <summary>
    /// Teletransporta el objeto al punto de spawn y lo reactiva.
    /// </summary>
    public void Teleport()
    {
        if (currentObject == null)
        {
            Debug.LogWarning("❌ No hay objeto asignado para teletransportar.");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("❌ No hay spawnPoint asignado.");
            return;
        }

        Debug.Log($"🔹 Teletransportando {currentObject.name} a {spawnPoint.position}");

        // --- Desactivar física temporalmente para evitar explosiones ---
        if (objectRigidbodies != null)
        {
            foreach (Rigidbody rb in objectRigidbodies)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // --- Teletransportar posición y rotación ---
        currentObject.transform.position = spawnPoint.position;
        if (matchRotation)
            currentObject.transform.rotation = spawnPoint.rotation;

        // Reactivar la física después de un pequeño delay
        CancelInvoke(nameof(EnablePhysics));
        Invoke(nameof(EnablePhysics), physicsEnableDelay);
    }

    private void EnablePhysics()
    {
        if (objectRigidbodies != null)
        {
            foreach (Rigidbody rb in objectRigidbodies)
            {
                if (rb != null)
                    rb.isKinematic = false;
            }
        }

        Debug.Log("⚙️ Física reactivada después de teletransporte.");
    }

    /// <summary>
    /// Cambia el objeto actual por uno nuevo.
    /// </summary>
    public void ChangeObject(GameObject newObject)
    {
        if (newObject == null)
        {
            Debug.LogWarning("❌ El nuevo objeto es nulo.");
            return;
        }

        Debug.Log($"🔄 Intercambiando {currentObject?.name} por {newObject.name}");

        // Desactivar el anterior
        if (currentObject != null)
            currentObject.SetActive(false);

        // Activar el nuevo y teletransportarlo
        currentObject = newObject;
        currentObject.SetActive(true);

        objectRigidbodies = currentObject.GetComponentsInChildren<Rigidbody>();

        Teleport();
    }
}
