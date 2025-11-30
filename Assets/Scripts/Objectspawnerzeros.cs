using UnityEngine;

public class ObjectSpawnerzeros : MonoBehaviour
{
    [Tooltip("Prefab que se va a instanciar")]
    public GameObject prefab;

    [Tooltip("Punto donde aparecerá el prefab (opcional, ya no se usa en este caso)")]
    public Transform spawnPoint;

    public void SpawnObject()
    {
        if (prefab == null)
        {
            Debug.LogWarning("❌ No hay prefab asignado en ObjectSpawner");
            return;
        }

        // Instancia el prefab siempre en (0,0,0) con rotación (0,0,0)
        GameObject newStructure = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Debug.Log($"✅ {prefab.name} instanciado en (0, 0, 0) con rotación (0, 0, 0)");
    }
}
