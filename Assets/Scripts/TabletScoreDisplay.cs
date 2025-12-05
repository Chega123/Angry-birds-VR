using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class TabletScoreDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI chefScoreText;
    [SerializeField] private TextMeshProUGUI soldadoScoreText;
    
    [Header("Formato")]
    [SerializeField] private string scoreFormat = "Score: {0}";
    [SerializeField] private string chefFormat = "Chef: {0}";
    [SerializeField] private string soldadoFormat = "Soldado: {0}";
    
    [Header("Colores")]
    [SerializeField] private Color chefColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color soldadoColor = new Color(0.2f, 0.8f, 0.3f);
    
    [Header("Feedback de Puntos")]
    [SerializeField] private AudioClip pointSound;
    [SerializeField] private Image borderFlash;
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float flashIntensity = 0.7f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Animacion de Texto")]
    [SerializeField] private float textScalePunch = 1.3f;
    [SerializeField] private float textAnimDuration = 0.2f;
    
    private AudioSource audioSource;
    private int currentChefScore = 0;
    private int currentSoldadoScore = 0;
    private GameMode currentMode = GameMode.None;
    private int lastChefScore = 0;
    private int lastSoldadoScore = 0;
    
    void Start()
    {
        SetupAudio();
        SetupBorderFlash();
        UpdateDisplay();
    }
    
    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
    }
    
    private void SetupBorderFlash()
    {
        if (borderFlash == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GameObject borderObj = new GameObject("ScoreBorderFlash");
                borderObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rt = borderObj.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
                
                borderFlash = borderObj.AddComponent<Image>();
                borderFlash.color = new Color(1, 1, 1, 0);
                borderFlash.raycastTarget = false;
                
                borderObj.transform.SetAsFirstSibling();
            }
        }
        
        if (borderFlash != null)
        {
            Color c = borderFlash.color;
            c.a = 0;
            borderFlash.color = c;
        }
    }
    
    public void UpdateScore(ScoreMessage scoreMessage)
    {
        if (scoreMessage == null) return;
        
        int newChefScore = scoreMessage.chefScore;
        int newSoldadoScore = scoreMessage.soldadoScore;
        
        if (System.Enum.TryParse<GameMode>(scoreMessage.mode, out GameMode mode))
        {
            currentMode = mode;
        }
        
        bool chefScoreChanged = newChefScore > currentChefScore;
        bool soldadoScoreChanged = newSoldadoScore > currentSoldadoScore;
        
        currentChefScore = newChefScore;
        currentSoldadoScore = newSoldadoScore;
        
        if (chefScoreChanged && currentMode == GameMode.Chef)
        {
            TriggerPointFeedback(chefColor);
        }
        else if (soldadoScoreChanged && currentMode == GameMode.Soldado)
        {
            TriggerPointFeedback(soldadoColor);
        }
        
        UpdateDisplay();
    }
    
    public void UpdateScore(int chefScore, int soldadoScore, GameMode mode)
    {
        bool chefScoreChanged = chefScore > currentChefScore;
        bool soldadoScoreChanged = soldadoScore > currentSoldadoScore;
        
        currentChefScore = chefScore;
        currentSoldadoScore = soldadoScore;
        currentMode = mode;
        
        if (chefScoreChanged && mode == GameMode.Chef)
        {
            TriggerPointFeedback(chefColor);
        }
        else if (soldadoScoreChanged && mode == GameMode.Soldado)
        {
            TriggerPointFeedback(soldadoColor);
        }
        
        UpdateDisplay();
    }
    
    private void TriggerPointFeedback(Color flashColor)
    {
        PlayPointSound();
        StartCoroutine(FlashBorder(flashColor));
        StartCoroutine(AnimateScoreText());
    }
    
    private void PlayPointSound()
    {
        if (audioSource != null && pointSound != null)
        {
            audioSource.PlayOneShot(pointSound);
        }
    }
    
    private IEnumerator FlashBorder(Color flashColor)
    {
        if (borderFlash == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            float alpha = flashCurve.Evaluate(t) * flashIntensity;
            
            Color c = flashColor;
            c.a = alpha;
            borderFlash.color = c;
            
            yield return null;
        }
        
        Color final = borderFlash.color;
        final.a = 0;
        borderFlash.color = final;
    }
    
    private IEnumerator AnimateScoreText()
    {
        if (scoreText == null) yield break;
        
        RectTransform rt = scoreText.GetComponent<RectTransform>();
        if (rt == null) yield break;
        
        Vector3 originalScale = rt.localScale;
        Vector3 targetScale = originalScale * textScalePunch;
        
        float elapsed = 0f;
        
        while (elapsed < textAnimDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (textAnimDuration / 2f);
            rt.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        while (elapsed < textAnimDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (textAnimDuration / 2f);
            rt.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        rt.localScale = originalScale;
    }
    
    /// <summary>
    /// Actualiza la visualización de los puntajes
    /// </summary>
    private void UpdateDisplay()
    {
        // Actualizar puntaje principal (el del modo actual)
        if (scoreText != null)
        {
            int currentScore = currentMode == GameMode.Chef ? currentChefScore : currentSoldadoScore;
            scoreText.text = string.Format(scoreFormat, currentScore);
            
            // Cambiar color según el modo
            if (currentMode == GameMode.Chef)
            {
                scoreText.color = chefColor;
            }
            else if (currentMode == GameMode.Soldado)
            {
                scoreText.color = soldadoColor;
            }
        }
        
        // Actualizar puntajes individuales
        if (chefScoreText != null)
        {
            chefScoreText.text = string.Format(chefFormat, currentChefScore);
            chefScoreText.color = chefColor;
        }
        
        if (soldadoScoreText != null)
        {
            soldadoScoreText.text = string.Format(soldadoFormat, currentSoldadoScore);
            soldadoScoreText.color = soldadoColor;
        }
    }
    
    /// <summary>
    /// Resetea los puntajes
    /// </summary>
    public void ResetScores()
    {
        currentChefScore = 0;
        currentSoldadoScore = 0;
        lastChefScore = 0;
        lastSoldadoScore = 0;
        UpdateDisplay();
    }
    
    /// <summary>
    /// Obtiene el puntaje actual del modo activo
    /// </summary>
    public int GetCurrentScore()
    {
        return currentMode == GameMode.Chef ? currentChefScore : currentSoldadoScore;
    }
}