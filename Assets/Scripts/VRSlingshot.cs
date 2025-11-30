using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class VRSlingshot : MonoBehaviour
{
    [Header("Referencias de la Resortera")]
    [SerializeField] private Transform slingshotCenter;
    [SerializeField] private Transform leftFork;
    [SerializeField] private Transform rightFork;
    [SerializeField] private Transform birdPlacementPoint;
    
    [Header("Ligas Visuales")]
    [SerializeField] private LineRenderer leftBand;
    [SerializeField] private LineRenderer rightBand;
    [SerializeField] private Material bandMaterial;
    [SerializeField] private float bandWidth = 0.02f;
    [SerializeField] private Color bandColor = Color.black;
    
    [Header("Configuración de Lanzamiento")]
    [SerializeField] private float maxStretchDistance = 1.5f;
    [SerializeField] private float minStretchToLaunch = 0.3f;
    [SerializeField] private float launchForceMultiplier = 20f;
    [SerializeField] private float maxLaunchForce = 60f;
    
    [Header("Configuración de Agarre")]
    [SerializeField] private float placementRadius = 0.3f;
    
    [Header("UI y Feedback Visual")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Canvas feedbackCanvas;
    
    // Estado del pájaro
    private GameObject loadedBird;
    private Rigidbody loadedBirdRb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable loadedBirdGrab;
    
    // Estado de estiramiento
    private bool isStretching = false;
    private Vector3 pullPosition;
    private Vector3 stretchStartPosition;
    
    // Pájaros siendo sostenidos
    private GameObject currentGrabbedBird;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor currentGrabbingInteractor;

    void Start()
    {
        SetupBands();
        SetupFeedbackUI();
        RegisterAllBirds();
    }

    void RegisterAllBirds()
    {
        GameObject[] allBirds = GameObject.FindGameObjectsWithTag("bird");
        Debug.Log($"🔍 Buscando pájaros... Encontrados: {allBirds.Length}");
        
        foreach (GameObject bird in allBirds)
        {
            RegisterBird(bird);
        }
    }

    void RegisterBird(GameObject bird)
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = bird.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        if (grab != null)
        {
            Debug.Log($"✅ Registrando pájaro: {bird.name}");
            
            // Remover listeners anteriores
            grab.selectEntered.RemoveAllListeners();
            grab.selectExited.RemoveAllListeners();
            
            // Cuando se agarra el pájaro
            grab.selectEntered.AddListener((args) => OnBirdGrabbed(bird, args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor));
            
            // Cuando se suelta el pájaro
            grab.selectExited.AddListener((args) => OnBirdReleased(bird, args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor));
        }
        else
        {
            Debug.LogWarning($"⚠️ El pájaro {bird.name} NO tiene XRGrabInteractable!");
        }
    }

    void OnBirdGrabbed(GameObject bird, UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        Debug.Log($"🐦 ¡Pájaro {bird.name} AGARRADO!");
        
        currentGrabbedBird = bird;
        currentGrabbingInteractor = interactor;
        
        // Si ya hay un pájaro cargado y es diferente, ignorar
        if (loadedBird != null && bird != loadedBird)
        {
            UpdateStatusText("⚠️ Ya hay un pájaro en la resortera");
            return;
        }
        
        UpdateStatusText("🖐️ ¡Pájaro agarrado! Acércalo a la resortera");
    }

    void OnBirdReleased(GameObject bird, UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        Debug.Log($"📤 Pájaro {bird.name} SOLTADO");
        
        // Si estábamos estirando, lanzar
        if (isStretching && bird == loadedBird)
        {
            LaunchBird();
        }
        else if (bird == loadedBird && !isStretching)
        {
            // Si soltamos sin estirar, descargarlo
            UnloadBird();
        }
        
        if (currentGrabbedBird == bird)
        {
            currentGrabbedBird = null;
            currentGrabbingInteractor = null;
        }
    }

    void SetupFeedbackUI()
    {
        if (feedbackCanvas == null && statusText == null)
        {
            GameObject canvasObj = new GameObject("FeedbackCanvas");
            feedbackCanvas = canvasObj.AddComponent<Canvas>();
            feedbackCanvas.renderMode = RenderMode.WorldSpace;
            
            canvasObj.transform.position = transform.position + transform.forward * 2f + Vector3.up * 1.5f;
            canvasObj.transform.rotation = Quaternion.LookRotation(canvasObj.transform.position - transform.position);
            canvasObj.transform.localScale = Vector3.one * 0.01f;
            
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(canvasObj.transform);
            textObj.transform.localPosition = Vector3.zero;
            
            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 36;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(800, 200);
        }
        
        UpdateStatusText("🖐️ ¡Agarra el pájaro con tu mano!");
    }

    void SetupBands()
    {
        if (leftBand == null)
        {
            GameObject leftBandObj = new GameObject("LeftBand");
            leftBandObj.transform.SetParent(transform);
            leftBand = leftBandObj.AddComponent<LineRenderer>();
        }
        ConfigureBand(leftBand);
        
        if (rightBand == null)
        {
            GameObject rightBandObj = new GameObject("RightBand");
            rightBandObj.transform.SetParent(transform);
            rightBand = rightBandObj.AddComponent<LineRenderer>();
        }
        ConfigureBand(rightBand);
    }

    void ConfigureBand(LineRenderer band)
    {
        band.startWidth = bandWidth;
        band.endWidth = bandWidth;
        band.positionCount = 2;
        band.useWorldSpace = true;
        
        if (bandMaterial != null)
        {
            band.material = bandMaterial;
        }
        else
        {
            band.material = new Material(Shader.Find("Sprites/Default"));
            band.startColor = bandColor;
            band.endColor = bandColor;
        }
    }

    void Update()
    {
        CheckBirdPlacement();
        
        if (isStretching)
        {
            UpdateStretch();
        }
        
        UpdateBands();
    }

    void CheckBirdPlacement()
    {
        // Si hay un pájaro agarrado y no hay ninguno cargado
        if (currentGrabbedBird != null && loadedBird == null && birdPlacementPoint != null)
        {
            float distance = Vector3.Distance(currentGrabbedBird.transform.position, birdPlacementPoint.position);
            
            if (distance < placementRadius)
            {
                LoadBirdIntoSlingshot(currentGrabbedBird);
            }
        }
    }

    void LoadBirdIntoSlingshot(GameObject bird)
    {
        Debug.Log($"✅ Cargando pájaro en la resortera: {bird.name}");
        
        loadedBird = bird;
        loadedBirdRb = bird.GetComponent<Rigidbody>();
        loadedBirdGrab = bird.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        // Posicionar en la resortera
        bird.transform.position = birdPlacementPoint.position;
        bird.transform.rotation = birdPlacementPoint.rotation;
        
        // Configurar física: mantener kinematic pero agarrable
        if (loadedBirdRb != null)
        {
            loadedBirdRb.isKinematic = true;
            loadedBirdRb.useGravity = false;
            loadedBirdRb.linearVelocity = Vector3.zero;
            loadedBirdRb.angularVelocity = Vector3.zero;
        }
        
        // MANTENER XRGrabInteractable ACTIVO para poder estirarlo
        if (loadedBirdGrab != null)
        {
            loadedBirdGrab.enabled = true;
        }
        
        currentGrabbedBird = null;
        currentGrabbingInteractor = null;
        
        // Iniciar estiramiento automáticamente si está siendo agarrado
        if (loadedBirdGrab != null && loadedBirdGrab.isSelected)
        {
            isStretching = true;
            stretchStartPosition = bird.transform.position;
            UpdateStatusText("🎯 ¡CARGADO! Estira hacia atrás y suelta para lanzar");
        }
        else
        {
            UpdateStatusText("✅ ¡CARGADO! Agárralo y estíralo hacia atrás");
        }
    }

    void UnloadBird()
    {
        Debug.Log("📤 Descargando pájaro de la resortera");
        
        if (loadedBirdRb != null)
        {
            loadedBirdRb.isKinematic = false;
            loadedBirdRb.useGravity = true;
        }
        
        loadedBird = null;
        loadedBirdRb = null;
        loadedBirdGrab = null;
        isStretching = false;
        
        UpdateStatusText("🖐️ Pájaro descargado. ¡Agarra otro!");
    }

    void UpdateStretch()
    {
        if (loadedBird == null) return;
        
        // Si el pájaro sigue siendo agarrado, está estirándose
        if (loadedBirdGrab != null && loadedBirdGrab.isSelected)
        {
            // Obtener la posición actual del pájaro (donde está la mano)
            Vector3 currentPosition = loadedBird.transform.position;
            
            // Calcular dirección desde el centro de la resortera
            Vector3 pullDirection = currentPosition - slingshotCenter.position;
            float pullDistance = pullDirection.magnitude;
            
            // Limitar el estiramiento máximo
            if (pullDistance > maxStretchDistance)
            {
                pullDirection = pullDirection.normalized * maxStretchDistance;
                pullDistance = maxStretchDistance;
                
                // Forzar la posición al límite
                loadedBird.transform.position = slingshotCenter.position + pullDirection;
            }
            
            pullPosition = loadedBird.transform.position;
            
            // Feedback visual
            float stretchPercent = (pullDistance / maxStretchDistance) * 100f;
            UpdateStatusText($"🎯 Estirando: {stretchPercent:F0}% - Suelta para lanzar!");
            
            if (!isStretching)
            {
                isStretching = true;
                stretchStartPosition = birdPlacementPoint.position;
            }
        }
    }

    void LaunchBird()
    {
        if (loadedBird == null) return;
        
        Debug.Log("🚀 Lanzando pájaro...");
        
        // Calcular dirección de lanzamiento (opuesta al estiramiento)
        Vector3 launchDirection = slingshotCenter.position - pullPosition;
        float pullDistance = launchDirection.magnitude;
        
        Debug.Log($"Pull distance: {pullDistance}m");
        
        if (pullDistance >= minStretchToLaunch)
        {
            if (loadedBirdRb != null)
            {
                // Activar física
                loadedBirdRb.isKinematic = false;
                loadedBirdRb.useGravity = true;
                
                float force = Mathf.Min(pullDistance * launchForceMultiplier, maxLaunchForce);
                loadedBirdRb.linearVelocity = Vector3.zero;
                loadedBirdRb.angularVelocity = Vector3.zero;
                
                // Aplicar fuerza
                loadedBirdRb.AddForce(launchDirection.normalized * force, ForceMode.Impulse);
                
                // Rotación para efecto visual
                Vector3 torqueAxis = Vector3.Cross(launchDirection, Vector3.up).normalized;
                loadedBirdRb.AddTorque(torqueAxis * force * 0.1f, ForceMode.Impulse);
                
                UpdateStatusText($"🚀 ¡LANZADO! Fuerza: {force:F1}");
                Debug.Log($"🚀 Pájaro lanzado con fuerza: {force}");
            }
            
            loadedBird = null;
            loadedBirdRb = null;
            loadedBirdGrab = null;
        }
        else
        {
            // No se estiró suficiente
            UpdateStatusText($"⚠️ Estira más! ({pullDistance:F2}m < {minStretchToLaunch}m)");
            Debug.Log($"⚠️ No se estiró suficiente: {pullDistance}m < {minStretchToLaunch}m");
            
            // Volver a posición
            loadedBird.transform.position = birdPlacementPoint.position;
            loadedBird.transform.rotation = birdPlacementPoint.rotation;
        }
        
        isStretching = false;
    }

    void UpdateBands()
    {
        if (leftFork == null || rightFork == null) return;
        
        Vector3 bandEndPoint;
        
        if (loadedBird != null)
        {
            bandEndPoint = loadedBird.transform.position;
        }
        else if (birdPlacementPoint != null)
        {
            bandEndPoint = birdPlacementPoint.position;
        }
        else
        {
            bandEndPoint = slingshotCenter.position;
        }
        
        leftBand.SetPosition(0, leftFork.position);
        leftBand.SetPosition(1, bandEndPoint);
        
        rightBand.SetPosition(0, rightFork.position);
        rightBand.SetPosition(1, bandEndPoint);
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[STATUS] {message}");
    }

    void OnDrawGizmos()
    {
        if (birdPlacementPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(birdPlacementPoint.position, placementRadius);
        }
        
        if (slingshotCenter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(slingshotCenter.position, maxStretchDistance);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(slingshotCenter.position, minStretchToLaunch);
        }
    }

    // Método público para registrar pájaros nuevos en runtime
    public void RegisterNewBird(GameObject bird)
    {
        if (bird == null)
        {
            Debug.LogWarning("⚠️ Intentando registrar un pájaro nulo");
            return;
        }

        Debug.Log($"🔔 RegisterNewBird llamado para: {bird.name}");
        RegisterBird(bird);
        
        // Asegurarse de que el pájaro tiene los componentes necesarios
        EnsureBirdComponents(bird);
    }

    void EnsureBirdComponents(GameObject bird)
    {
        // Verificar XRGrabInteractable
        var grab = bird.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
        {
            grab = bird.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.smoothPosition = true;
            grab.smoothRotation = true;
            Debug.Log($"✅ XRGrabInteractable añadido a {bird.name}");
        }

        // Verificar Rigidbody
        var rb = bird.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = bird.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            Debug.Log($"✅ Rigidbody añadido a {bird.name}");
        }

        // Verificar Collider
        var col = bird.GetComponent<Collider>();
        if (col == null)
        {
            bird.AddComponent<BoxCollider>();
            Debug.Log($"✅ BoxCollider añadido a {bird.name}");
        }

        // Verificar tag
        if (!bird.CompareTag("bird"))
        {
            bird.tag = "bird";
            Debug.Log($"🏷️ Tag 'bird' asignado a {bird.name}");
        }
    }
}