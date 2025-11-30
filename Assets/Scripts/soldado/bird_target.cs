using UnityEngine;

public class BirdTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int health = 1;
    [SerializeField] private float destroyDelay = 0.5f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Score")]
    [SerializeField] private int pointsValue = 10; // ⭐ Puntos al matar
    
    private AudioSource audioSource;
    private bool isDead = false;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        Debug.Log($"[BirdTarget] {gameObject.name} inicializado - Vale {pointsValue} puntos");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return;
        
        // Verificar si es una bala
        if (other.CompareTag("Bullet") || other.GetComponent<BulletMovement>() != null)
        {
            Debug.Log($"[BirdTarget] ¡Impacto de bala en {gameObject.name}!");
            
            TakeDamage(1);
            
            // Destruir la bala
            Destroy(other.gameObject);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;
        
        // También manejar colisiones con física
        if (collision.gameObject.CompareTag("Bullet") || 
            collision.gameObject.GetComponent<BulletMovement>() != null)
        {
            Debug.Log($"[BirdTarget] ¡Colisión de bala con {gameObject.name}!");
            
            TakeDamage(1);
            
            // Destruir la bala
            Destroy(collision.gameObject);
        }
    }
    
    private void TakeDamage(int damage)
    {
        if (isDead) return;
        
        health -= damage;
        
        Debug.Log($"[BirdTarget] {gameObject.name} recibió {damage} daño. Vida: {health}");
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log($"[BirdTarget] ¡{gameObject.name} eliminado!");
        
        // ⭐ SUMAR PUNTOS AL SOLDADO
        if (GameScoreSystem.Instance != null)
        {
            GameScoreSystem.Instance.AddSoldadoScore(pointsValue);
            Debug.Log($"[BirdTarget] +{pointsValue} puntos al Soldado! 🎯");
        }
        else
        {
            Debug.LogWarning("[BirdTarget] GameScoreSystem no encontrado");
        }
        
        // Efecto de muerte
        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Sonido de muerte
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
            
            // Mantener el AudioSource vivo para que termine el sonido
            audioSource.transform.SetParent(null);
            Destroy(audioSource.gameObject, deathSound.length);
        }
        
        // Desactivar visualmente
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Destruir el pájaro
        Destroy(gameObject, destroyDelay);
    }
}