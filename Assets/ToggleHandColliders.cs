using UnityEngine;

public class ToggleHandColliders : MonoBehaviour
{
    [Header("Raíz de la mano con todos los huesos")]
    public GameObject handRoot;

    private Collider[] handColliders;
    private bool collidersEnabled = true;

    void Start()
    {
        if (handRoot == null)
        {
            Debug.LogError("No se asignó la raíz de la mano en el inspector.");
            return;
        }

        // Buscar todos los colliders en la jerarquía de la mano
        handColliders = handRoot.GetComponentsInChildren<Collider>();

        Debug.Log($"✅ Se encontraron {handColliders.Length} colliders en la mano.");
    }

    /// <summary>
    /// Activa o desactiva los colliders de la mano
    /// </summary>
    public void ToggleColliders()
    {
        collidersEnabled = !collidersEnabled;

        foreach (var col in handColliders)
        {
            col.enabled = collidersEnabled;
        }

        Debug.Log($"Colliders de la mano {(collidersEnabled ? "ACTIVADOS ✅" : "DESACTIVADOS ❌")}");
    }
}
