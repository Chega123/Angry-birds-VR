using UnityEngine;

[System.Serializable]
public struct DamageState
{
    public Material material;
    public float minHealthPercentage;
}

public class Block : MonoBehaviour
{
    public enum MaterialType { Ice, Wood, Rock, Pig } 
    public MaterialType materialType;
    public float baseDurability = 100f;
    [Range(1, 3)] public int sizeMultiplier = 1;
    private float currentDurability;

    [Header("Efectos de destruccion")]
    public AudioClip destroySound;
    public GameObject destructionParticlesPrefab;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Sistema de Partículas")]
    [Tooltip("Duración antes de destruir el sistema de partículas")]
    public float particleLifetime = 2f;  // Tiempo que durarán las partículas
    [Tooltip("Escala de las partículas (1 = tamaño original)")]
    [Range(0.1f, 5f)]
    public float particleScale = 1f;

    [Header("Puntuación")]
    public int pointsForDamage = 10;
    public int pointsForDestroy = 100;

    [Header("Estados de Daño Visual")]
    public DamageState[] damageStates;
    private MeshRenderer meshRenderer;

    void Start()
    {
        currentDurability = baseDurability * sizeMultiplier;
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateVisualState();
    }

    public void TakeDamage(float damage)
    {
        float finalDamage = damage * GetMaterialMultiplier();
        currentDurability -= finalDamage;
        
        // --- CAMBIO: Ahora enviamos el tipo de evento "BlockHit" ---
        if (ScoreManager.instance != null)
        {
            // Solo damos puntos por daño si NO es un cerdito
            if (materialType != MaterialType.Pig)
            {
                ScoreManager.instance.AddScore(pointsForDamage, ScoreManager.ScoreEventType.BlockHit);
            }
        }

        Debug.Log($"{gameObject.name} recibió {finalDamage} de daño. Durabilidad restante: {currentDurability}");
        
        UpdateVisualState();

        if (currentDurability <= 0f)
        {
            if (ScoreManager.instance != null)
            {
                // destruyo bloque o cerdo
                if (materialType == MaterialType.Pig)
                {
                    ScoreManager.instance.AddScore(pointsForDestroy, ScoreManager.ScoreEventType.PigDestroy);
                }
                else
                {
                    ScoreManager.instance.AddScore(pointsForDestroy, ScoreManager.ScoreEventType.BlockDestroy);
                }
            }

            // --- SISTEMA DE PARTÍCULAS MEJORADO ---
            if (destructionParticlesPrefab != null)
            {
                GameObject particles = Instantiate(destructionParticlesPrefab, transform.position, transform.rotation);
                // Aplicar escala a las partículas
                particles.transform.localScale = Vector3.one * particleScale;
                // Destruir las partículas después del tiempo especificado
                Destroy(particles, particleLifetime);
            }

            if (destroySound != null)
                AudioSource.PlayClipAtPoint(destroySound, transform.position, soundVolume);

            Destroy(gameObject);
        }
    }
    
    private void UpdateVisualState()
    {
        if (meshRenderer == null || damageStates.Length == 0) return;
        float healthPercentage = currentDurability / (baseDurability * sizeMultiplier);

        foreach (var state in damageStates)
        {
            if (healthPercentage >= state.minHealthPercentage)
            {
                meshRenderer.material = state.material;
                return; 
            }
        }
    }

    private float GetMaterialMultiplier()
    {
        switch (materialType)
        {
            case MaterialType.Ice: return 2f;
            case MaterialType.Wood: return 1f;
            case MaterialType.Rock: return 0.5f;
            
            case MaterialType.Pig: return 1f; 
            default: return 1f;
        }
    }
}