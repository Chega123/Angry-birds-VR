using UnityEngine;
using System.Collections;

public class TabletSoldadoController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera orthographicCamera;
    
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private float shootCooldown = 0.5f;
    [SerializeField] private float spawnDistance = 0.5f; // Qué tan lejos de la cámara spawneará
    
    [Header("Shooting Direction")]
    [SerializeField] private Vector3 shootDirection = Vector3.forward; // Dirección fija del disparo (adelante)
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private float muzzleFlashDuration = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool verboseDebug = true;
    [SerializeField] private bool drawDebugRay = true;
    [SerializeField] private float debugRayDuration = 2f;
    
    private VRWebSocketServer server;
    private float lastShootTime = -999f;
    private bool canShoot = true;
    
    void Start()
    {
        if (orthographicCamera == null)
        {
            Debug.LogError("[Soldado] ¡Asigna la cámara ortográfica en el inspector!");
            enabled = false;
            return;
        }
        
        if (bulletPrefab == null)
        {
            Debug.LogError("[Soldado] ¡Asigna el prefab de bala!");
            enabled = false;
            return;
        }
        
        // Buscar el servidor
        server = FindObjectOfType<VRWebSocketServer>();
        if (server != null)
        {
            server.RegisterSoldadoController(this);
            Debug.Log("[Soldado] ✓ Registrado con VRWebSocketServer");
        }
        else
        {
            Debug.LogError("[Soldado] ✗ No se encontró VRWebSocketServer!");
        }
        
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(false);
        }
        
        Debug.Log("[Soldado] Controlador inicializado");
        Debug.Log($"  - Cooldown: {shootCooldown}s");
        Debug.Log($"  - Velocidad bala: {bulletSpeed}");
        Debug.Log($"  - Dirección disparo: {shootDirection}");
        Debug.Log($"  - Distancia spawn: {spawnDistance}");
    }
    
    public void OnTabletTouch(TabletInput input)
    {
        // Solo disparar cuando se hace click/touch (Began)
        if (input.action != "Began")
        {
            return;
        }
        
        if (!canShoot)
        {
            if (verboseDebug)
            {
                float timeLeft = shootCooldown - (Time.time - lastShootTime);
                Debug.Log($"[Soldado] En cooldown. Espera {timeLeft:F1}s");
            }
            return;
        }
        
        if (verboseDebug)
        {
            Debug.Log($"[Soldado] ===== DISPARO =====");
            Debug.Log($"[Soldado] Touch coords normalized: ({input.screenX:F3}, {input.screenY:F3})");
        }
        
        // NUEVO: Calcular la posición en el mundo donde el usuario tocó
        // Las coordenadas están normalizadas (0-1)
        Vector3 viewportPoint = new Vector3(input.screenX, input.screenY, spawnDistance);
        
        // Convertir viewport a mundo usando la cámara ortográfica
        Vector3 spawnPosition = orthographicCamera.ViewportToWorldPoint(viewportPoint);
        
        if (verboseDebug)
        {
            Debug.Log($"[Soldado] Viewport point: {viewportPoint}");
            Debug.Log($"[Soldado] Spawn position (world): {spawnPosition}");
        }
        
        // La dirección es fija (hacia adelante en el espacio de la cámara)
        Vector3 worldShootDirection = orthographicCamera.transform.TransformDirection(shootDirection).normalized;
        
        if (verboseDebug)
        {
            Debug.Log($"[Soldado] Shoot direction (world): {worldShootDirection}");
        }
        
        // Disparar la bala en esa posición
        Shoot(spawnPosition, worldShootDirection);
    }
    
    private void Shoot(Vector3 position, Vector3 direction)
    {
        // Marcar que disparamos
        lastShootTime = Time.time;
        canShoot = false;
        
        // Crear la bala en la posición donde el usuario tocó
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
        
        // IMPORTANTE: Eliminar cualquier componente de cámara
        var cinemachineComponents = bullet.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in cinemachineComponents)
        {
            if (component.GetType().Name.Contains("Cinemachine"))
            {
                Debug.Log($"[Soldado] Eliminando componente: {component.GetType().Name}");
                Destroy(component);
            }
        }
        
        // Configurar movimiento de la bala
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Desactivar gravedad para que vaya recto
            rb.useGravity = false;
            rb.linearVelocity = direction * bulletSpeed;
            
            if (verboseDebug)
            {
                Debug.Log($"[Soldado] Bala con Rigidbody, velocidad: {rb.linearVelocity}");
            }
        }
        else
        {
            // Si no tiene Rigidbody, usar componente simple
            BulletMovement bulletMovement = bullet.GetComponent<BulletMovement>();
            if (bulletMovement == null)
            {
                bulletMovement = bullet.AddComponent<BulletMovement>();
            }
            bulletMovement.Initialize(direction, bulletSpeed);
            
            if (verboseDebug)
            {
                Debug.Log($"[Soldado] Bala con BulletMovement");
            }
        }
        
        // Destruir la bala después de un tiempo
        Destroy(bullet, bulletLifetime);
        
        // Efecto visual de disparo (opcional, en la posición de spawn)
        if (muzzleFlash != null)
        {
            StartCoroutine(ShowMuzzleFlash(position));
        }
        
        Debug.Log($"[Soldado] ✓ Bala spawneada en {position} con dirección {direction}");
        
        // Dibujar debug ray
        if (drawDebugRay)
        {
            Debug.DrawRay(position, direction * 50f, Color.red, debugRayDuration);
        }
        
        // Iniciar cooldown
        StartCoroutine(CooldownRoutine());
    }
    
    private IEnumerator ShowMuzzleFlash(Vector3 position)
    {
        if (muzzleFlash != null)
        {
            // Crear instancia temporal del muzzle flash en la posición
            GameObject flash = Instantiate(muzzleFlash, position, Quaternion.identity);
            flash.SetActive(true);
            
            yield return new WaitForSeconds(muzzleFlashDuration);
            
            Destroy(flash);
        }
    }
    
    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(shootCooldown);
        canShoot = true;
        
        if (verboseDebug)
        {
            Debug.Log("[Soldado] Cooldown terminado. Listo para disparar.");
        }
    }
    
    void OnDrawGizmos()
    {
        if (orthographicCamera != null)
        {
            // Dibujar la dirección de disparo desde el centro de la cámara
            Gizmos.color = Color.cyan;
            Vector3 center = orthographicCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, spawnDistance));
            Vector3 worldDir = orthographicCamera.transform.TransformDirection(shootDirection);
            Gizmos.DrawRay(center, worldDir * 5f);
            
            // Dibujar el plano de spawn
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.2f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (orthographicCamera != null)
        {
            // Dibujar varios puntos de ejemplo
            Gizmos.color = Color.green;
            
            // Esquinas
            Vector3[] corners = new Vector3[]
            {
                orthographicCamera.ViewportToWorldPoint(new Vector3(0, 0, spawnDistance)),      // Abajo-izq
                orthographicCamera.ViewportToWorldPoint(new Vector3(1, 0, spawnDistance)),      // Abajo-der
                orthographicCamera.ViewportToWorldPoint(new Vector3(0, 1, spawnDistance)),      // Arriba-izq
                orthographicCamera.ViewportToWorldPoint(new Vector3(1, 1, spawnDistance)),      // Arriba-der
                orthographicCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, spawnDistance)) // Centro
            };
            
            foreach (var corner in corners)
            {
                Gizmos.DrawWireSphere(corner, 0.15f);
            }
        }
    }
}

// Componente simple para mover balas sin física
public class BulletMovement : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    
    public void Initialize(Vector3 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        
        Debug.Log($"[BulletMovement] Inicializado: dir={direction}, speed={speed}");
    }
    
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
