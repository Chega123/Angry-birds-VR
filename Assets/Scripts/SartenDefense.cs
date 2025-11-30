using UnityEngine;

public class SartenDefense : MonoBehaviour
{
    [Header("Configuración de Defensa")]
    [SerializeField] private string projectileTag = "Projectile";
    [SerializeField] private bool destroyOnBlock = false;
    [SerializeField] private bool deflectProjectile = true;
    [SerializeField] private float deflectForce = 15f;
    [SerializeField] private Vector3 deflectDirection = new Vector3(0, 0.3f, -1f);
    
    [Header("Sistema de Puntos")]
    [SerializeField] private int pointsPerBlock = 10; // ⭐ Puntos por bloqueo
    [SerializeField] private bool givePointsOnBlock = true;
    
    [Header("Efectos Visuales")]
    [SerializeField] private ParticleSystem blockEffect;
    [SerializeField] private AudioClip blockSound;
    [SerializeField] private float hitFlashDuration = 0.1f;
    
    [Header("Feedback Visual")]
    [SerializeField] private Renderer sartenRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.yellow;
    
    private AudioSource audioSource; 
    private Material sartenMaterial;
    private float flashTimer = 0f;
    private int blockedCount = 0;
    
    void Start()
    {
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && blockSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Obtener material del sartén
        if (sartenRenderer != null)
        {
            sartenMaterial = sartenRenderer.material;
            sartenMaterial.color = normalColor;
        }
        
        Debug.Log("[SartenDefense] Inicializado - Puntos por bloqueo: " + pointsPerBlock);
    }
    
    void Update()
    {
        // Restaurar color después del flash
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && sartenMaterial != null)
            {
                sartenMaterial.color = normalColor;
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Verificar si es un proyectil
        if (collision.gameObject.CompareTag(projectileTag))
        {
            HandleProjectileBlock(collision);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // También soportar triggers
        if (other.CompareTag(projectileTag))
        {
            HandleProjectileBlockTrigger(other.gameObject);
        }
    }
    
    private void HandleProjectileBlock(Collision collision)
    {
        blockedCount++;
        Debug.Log($"[SartenDefense] ¡Proyectil bloqueado! Total: {blockedCount}");
        
        // ⭐ SUMAR PUNTOS
        if (givePointsOnBlock && GameScoreSystem.Instance != null)
        {
            GameScoreSystem.Instance.AddChefScore(pointsPerBlock);
            Debug.Log($"[SartenDefense] +{pointsPerBlock} puntos! 🍳");
        }
        
        // Efectos visuales y sonoros
        PlayBlockEffects(collision.contacts[0].point);
        
        if (deflectProjectile)
        {
            // Rebotar el proyectil
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 deflectDirection = Vector3.Reflect(
                    collision.relativeVelocity.normalized, 
                    collision.contacts[0].normal
                );
                rb.linearVelocity = deflectDirection * deflectForce;
            }
        }
        else if (destroyOnBlock)
        {
            // Destruir el proyectil
            Destroy(collision.gameObject);
        }
    }
    
    private void HandleProjectileBlockTrigger(GameObject projectile)
    {
        blockedCount++;
        Debug.Log($"[SartenDefense] ¡Proyectil bloqueado (Trigger)! Total: {blockedCount}");
        
        // ⭐ SUMAR PUNTOS
        if (givePointsOnBlock && GameScoreSystem.Instance != null)
        {
            GameScoreSystem.Instance.AddChefScore(pointsPerBlock);
            Debug.Log($"[SartenDefense] +{pointsPerBlock} puntos! 🍳");
        }
        
        // Efectos visuales y sonoros
        PlayBlockEffects(projectile.transform.position);
        
        if (destroyOnBlock)
        {
            Destroy(projectile);
        }
    }
    
    private void PlayBlockEffects(Vector3 position)
    {
        // Efecto de partículas
        if (blockEffect != null)
        {
            ParticleSystem effect = Instantiate(blockEffect, position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }
        
        // Sonido
        if (audioSource != null && blockSound != null)
        {
            audioSource.PlayOneShot(blockSound);
        }
        
        // Flash visual
        if (sartenMaterial != null)
        {
            sartenMaterial.color = hitColor;
            flashTimer = hitFlashDuration;
        }
    }
    
    public int GetBlockedCount()
    {
        return blockedCount;
    }
    
    public void ResetBlockedCount()
    {
        blockedCount = 0;
    }
    
    void OnDestroy()
    {
        // Limpiar material para evitar memory leaks
        if (sartenMaterial != null)
        {
            Destroy(sartenMaterial);
        }
    }
}