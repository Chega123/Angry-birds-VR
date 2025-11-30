using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.XR;

public class XRTeleporter : MonoBehaviour
{
    [Header("XR Rig")]
    public Transform xrOrigin;
    public Transform xrCamera;

    [Header("Destino")]
    public Transform targetPosition;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 0.25f;

    [Header("Lógica del Nivel")]
    public LevelTimer levelTimer;

    private bool isTeleporting = false;

    public void Teleport()
    {
        if (xrOrigin == null || xrCamera == null || targetPosition == null)
        {
            Debug.LogWarning("⚠ Faltan referencias en XRTeleporter.");
            return;
        }

        if (!isTeleporting)
            StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;

        // Fade Out
        if (fadeImage != null)
            yield return Fade(1f);

        // --- Teletransporte ---
        Vector3 cameraOffset = xrOrigin.position - xrCamera.position;
        Vector3 newRigPos = targetPosition.position + cameraOffset;
        xrOrigin.position = newRigPos;

        // Recentrar XR tracking
        RecenterXRTracking();

        // Fade In
        if (fadeImage != null)
            yield return Fade(0f);

        Debug.Log("⚡ XR Teleport completado hacia " + targetPosition.position);

        // Si este teletransportador tiene un Level Manager asignado...
        if (levelTimer != null)
        {
            // --- CAMBIO: Añadimos la lógica para reiniciar el puntaje ---
            // Primero, le pedimos al ScoreManager que se reinicie.
            if (levelTimer.scoreManager != null)
            {
                levelTimer.scoreManager.ResetScore();
            }

            // Después, iniciamos el cronómetro.
            levelTimer.StartTimer();
        }

        isTeleporting = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        Color c = fadeImage.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float blend = Mathf.Clamp01(t / fadeDuration);
            c.a = Mathf.Lerp(startAlpha, targetAlpha, blend);
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;
    }

    private void RecenterXRTracking()
    {
        var subsystems = new System.Collections.Generic.List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        foreach (var subsystem in subsystems)
        {
            subsystem.TryRecenter();
        }
    }
}