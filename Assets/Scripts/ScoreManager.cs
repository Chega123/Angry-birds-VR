using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    private int currentScore = 0;
    private Vector3 originalScale;
    private Coroutine scoreAnimationCoroutine;
    private bool isLevelOver = false; // new

    public enum ScoreEventType { BlockHit, BlockDestroy, PigDestroy }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (scoreText != null)
        {
            originalScale = scoreText.transform.localScale;
        }
        UpdateScoreUI();
    }

    public void AddScore(int points, ScoreEventType eventType)
    {
        if (isLevelOver) return; // bloquea puntos despuÃ©s de que acabe el tiempo

        currentScore += points;
        UpdateScoreUI();

        if (scoreText != null)
        {
            if (scoreAnimationCoroutine != null) StopCoroutine(scoreAnimationCoroutine);
            scoreAnimationCoroutine = StartCoroutine(AnimateScoreText(eventType));
        }
    }

    // nuevo
    public void ShowFinalScoreEffect()
    {
        if (isLevelOver) return;
        isLevelOver = true;

        if (scoreText != null)
        {
            if (scoreAnimationCoroutine != null) StopCoroutine(scoreAnimationCoroutine);
            StartCoroutine(FinalScoreAnimation());
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Puntaje: " + currentScore.ToString();
        }
    }

    private IEnumerator AnimateScoreText(ScoreEventType eventType)
    {
        float duration = 0.3f;
        float timer = 0;
        float scaleMultiplier = 1.2f;
        Color targetColor = Color.white;
        
        switch (eventType)
        {
            case ScoreEventType.BlockHit:
                scaleMultiplier = 1.2f;
                targetColor = Color.white;
                break;
            case ScoreEventType.BlockDestroy:
                scaleMultiplier = 1.5f;
                targetColor = Color.yellow;
                break;
            case ScoreEventType.PigDestroy:
                scaleMultiplier = 1.8f;
                targetColor = new Color(0.5f, 1f, 0.5f);
                break;
        }

        Vector3 targetScale = originalScale * scaleMultiplier;
        
        while (timer < duration / 2)
        {
            scoreText.transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / (duration / 2));
            scoreText.color = Color.Lerp(Color.white, targetColor, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }
        
        timer = 0;
        
        while (timer < duration / 2)
        {
            scoreText.transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / (duration / 2));
            scoreText.color = Color.Lerp(targetColor, Color.white, timer / (duration / 2));
            timer += Time.deltaTime;
            yield return null;
        }
        
        scoreText.transform.localScale = originalScale;
        scoreText.color = Color.white;
    }

    // --- COROUTINE NUEVA PARA EL EFECTO FINAL ---
    private IEnumerator FinalScoreAnimation()
    {
        float duration = 0.5f; // Una animaciÃ³n un poco mÃ¡s lenta
        float timer = 0;
        float targetScaleMultiplier = 2.0f; // MÃ¡s grande que cualquier otro efecto
        Color targetColor = new Color(1f, 0.84f, 0f); // Color dorado

        Vector3 startScale = scoreText.transform.localScale;
        Vector3 targetScale = originalScale * targetScaleMultiplier;

        // Animamos hasta el estado final
        while (timer < duration)
        {
            scoreText.transform.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            scoreText.color = Color.Lerp(Color.white, targetColor, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Nos aseguramos de que se quede exactamente en el estado final
        scoreText.transform.localScale = targetScale;
        scoreText.color = targetColor;
    }

    public void ResetScore()
    {
        currentScore = 0;
        isLevelOver = false; // --- NUEVO: Reseteamos la bandera
        if (scoreText != null)
        {
            scoreText.transform.localScale = originalScale;
            scoreText.color = Color.white;
        }
        UpdateScoreUI();
    }
    /// <summary>
    /// Obtiene el puntaje actual (para GameEndManager)
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }
}