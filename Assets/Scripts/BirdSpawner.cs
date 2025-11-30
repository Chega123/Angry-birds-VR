using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BirdSpawner : MonoBehaviour
{
    [Header("Prefab del Pájaro")]
    [SerializeField] private GameObject birdPrefab;
    
    [Header("Resortera")]
    [SerializeField] private VRSlingshot slingshot;
    [SerializeField] private Transform spawnPoint; // birdPlacementPoint
    
    [Header("Configuración")]
    [SerializeField] private bool autoSpawnOnStart = true;
    [SerializeField] private float cooldownTime = 0.5f;
    [SerializeField] private bool autoRespawnWhenEmpty = true;
    
    private float lastSpawnTime = 0f;
    private GameObject currentBird;

    void Start()
    {
        if (autoSpawnOnStart)
        {
            SpawnBird();
        }
    }

    void Update()
    {
        // Auto-respawn cuando el pájaro fue lanzado o destruido
        if (currentBird == null && autoRespawnWhenEmpty)
        {
            if (Time.time - lastSpawnTime > cooldownTime)
            {
                SpawnBird();
            }
        }
    }

    public void SpawnBird()
    {
        // Verificar cooldown
        if (Time.time - lastSpawnTime < cooldownTime)
        {
            return;
        }
        
        if (birdPrefab == null)
        {
            Debug.LogError("❌ No hay prefab de pájaro asignado!");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("❌ No hay spawn point asignado!");
            return;
        }
        
        // Destruir el pájaro anterior si existe
        if (currentBird != null)
        {
            Destroy(currentBird);
        }
        
        // Crear nuevo pájaro
        currentBird = Instantiate(birdPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Asegurarse que tiene tag
        if (!currentBird.CompareTag("bird"))
        {
            currentBird.tag = "bird";
        }
        
        // Asegurarse que tiene XRGrabInteractable
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = currentBird.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
        {
            grab = currentBird.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.smoothPosition = true;
            grab.smoothRotation = true;
        }
        
        // Asegurarse que tiene Rigidbody
        Rigidbody rb = currentBird.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentBird.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
        }
        
        // CRÍTICO: NO congelar, NO hacer kinematic, dejar que sea agarrable
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        
        // Registrar en la resortera DESPUÉS de configurar
        if (slingshot != null)
        {
            slingshot.RegisterNewBird(currentBird);
        }
        
        lastSpawnTime = Time.time;
        
        Debug.Log($"✅ Pájaro spawneado y listo para agarrar!");
    }

    public void SpawnBirdButton()
    {
        SpawnBird();
    }
    
    public void SetBirdPrefab(GameObject newPrefab)
    {
        birdPrefab = newPrefab;
    }
    
    public bool HasActiveBird()
    {
        return currentBird != null;
    }
    
    public GameObject GetCurrentBird()
    {
        return currentBird;
    }

    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.05f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 0.2f);
        }
    }
}