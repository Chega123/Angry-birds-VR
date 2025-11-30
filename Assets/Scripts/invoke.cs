using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjectSpawner : MonoBehaviour
{
    [Tooltip("Prefab que se va a instanciar")]
    public GameObject prefab;

    [Tooltip("Punto donde aparecerá el prefab")]
    public Transform spawnPoint;

    [Header("Configuración de Resortera (para pájaros)")]
    [Tooltip("Referencia al script VRSlingshot para registrar pájaros automáticamente")]
    public VRSlingshot slingshot;

    [Tooltip("¿Este prefab es un pájaro que debe registrarse en la resortera?")]
    public bool isBird = false;

    [Tooltip("Si es true, el pájaro se spawneará directamente cargado en la resortera")]
    public bool spawnLoadedInSlingshot = false;

    public void SpawnObject()
    {
        if (prefab == null)
        {
            Debug.LogWarning("❌ No hay prefab asignado en ObjectSpawner");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("❌ No hay spawnPoint asignado en ObjectSpawner");
            return;
        }

        // Instancia el prefab en la posición exacta y con la misma rotación del spawnPoint
        GameObject newObject = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"✅ {prefab.name} instanciado en {spawnPoint.position} con rotación {spawnPoint.rotation.eulerAngles}");

        // Si es un pájaro, configurarlo para la resortera
        if (isBird)
        {
            SetupBird(newObject);
        }
    }

    void SetupBird(GameObject bird)
    {
        // Asegurarse que tiene el tag correcto
        if (!bird.CompareTag("bird"))
        {
            bird.tag = "bird";
            Debug.Log($"🏷️ Tag 'bird' asignado a {bird.name}");
        }

        // Asegurarse que tiene XRGrabInteractable
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = bird.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
        {
            grab = bird.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.smoothPosition = true;
            grab.smoothRotation = true;
            grab.tightenPosition = 0.5f;
            grab.tightenRotation = 0.5f;
            Debug.Log($"✅ XRGrabInteractable añadido a {bird.name}");
        }

        // Asegurarse que tiene Rigidbody
        Rigidbody rb = bird.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bird.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            Debug.Log($"✅ Rigidbody añadido a {bird.name}");
        }

        // Verificar que tenga física activa
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        // Asegurarse que tiene Collider
        Collider col = bird.GetComponent<Collider>();
        if (col == null)
        {
            // Intentar añadir un BoxCollider por defecto
            BoxCollider boxCol = bird.AddComponent<BoxCollider>();
            Debug.Log($"✅ BoxCollider añadido a {bird.name}");
        }

        // CRÍTICO: Registrar en la resortera
        if (slingshot != null)
        {
            // Pequeño delay para asegurar que todos los componentes están listos
            StartCoroutine(RegisterBirdDelayed(bird));
        }
        else
        {
            Debug.LogWarning("⚠️ No hay referencia a VRSlingshot. El pájaro no se registrará automáticamente.");
            Debug.LogWarning("💡 Arrastra el objeto VRSlingshot al campo 'Slingshot' en el Inspector");
        }
    }

    System.Collections.IEnumerator RegisterBirdDelayed(GameObject bird)
    {
        // Esperar un frame para que todos los componentes se inicialicen
        yield return null;

        slingshot.RegisterNewBird(bird);
        Debug.Log($"✅ Pájaro '{bird.name}' registrado en la resortera");

        // Si queremos spawnearlo directamente cargado
        if (spawnLoadedInSlingshot)
        {
            // Buscar el punto de colocación en la resortera
            Transform birdPlacement = FindBirdPlacementPoint();
            if (birdPlacement != null)
            {
                bird.transform.position = birdPlacement.position;
                bird.transform.rotation = birdPlacement.rotation;
                
                Rigidbody rb = bird.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                
                Debug.Log($"📍 Pájaro colocado directamente en la resortera");
            }
        }
    }

    Transform FindBirdPlacementPoint()
    {
        if (slingshot == null) return null;

        // Intentar encontrar el BirdPlacementPoint
        Transform[] children = slingshot.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name.Contains("BirdPlacement") || child.name.Contains("Placement"))
            {
                return child;
            }
        }

        return slingshot.transform;
    }

    // Método alternativo que permite especificar la resortera
    public void SpawnObjectWithSlingshot(VRSlingshot targetSlingshot)
    {
        slingshot = targetSlingshot;
        SpawnObject();
    }

    // Método para spawner sin verificación de punto (útil para debugging)
    public void SpawnObjectAtOrigin()
    {
        if (prefab == null)
        {
            Debug.LogWarning("❌ No hay prefab asignado");
            return;
        }

        GameObject newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Debug.Log($"✅ {prefab.name} instanciado en (0,0,0)");

        if (isBird)
        {
            SetupBird(newObject);
        }
    }
}