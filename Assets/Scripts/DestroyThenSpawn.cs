using UnityEngine;
using System.Collections;

public class DestroyThenSpawn : MonoBehaviour
{
    [Header("Referencias")]
    public DestroyZone destroyZone;      // Zona donde se destruirán los objetos
    public ObjectSpawnerzeros spawner;        // Script que creará el nuevo objeto

    [Tooltip("Tiempo a esperar entre destruir y crear un nuevo objeto (segundos)")]
    public float delayBetweenActions = 0.1f;

    /// <summary>
    /// Llama a la secuencia: primero destruir, luego instanciar
    /// </summary>
    public void ExecuteSequence()
    {
        StartCoroutine(DestroyAndSpawn());
    }

    private IEnumerator DestroyAndSpawn()
    {
        // 1. Destruir todos los objetos en la zona
        if (destroyZone != null)
        {
            destroyZone.DestroyAllInZone();
            Debug.Log("Objetos destruidos en la zona.");
        }

        // Espera un pequeño tiempo para asegurarse que la destrucción se procese
        yield return new WaitForSeconds(delayBetweenActions);

        // 2. Instanciar nuevo objeto
        if (spawner != null)
        {
            spawner.SpawnObject();
            Debug.Log("Nuevo objeto instanciado.");
        }
    }
}
