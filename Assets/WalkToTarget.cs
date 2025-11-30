using UnityEngine;
using System.Collections;

public class WalkToTargetXR : MonoBehaviour
{
    [Header("XR Rig y cámara")]
    public Transform xrOrigin;          // XR Origin raíz
    public Transform xrCamera;          // Main Camera del XR

    [Header("Destino")]
    public Transform targetPosition;
    public float moveSpeed = 1.5f;
    public float arriveThreshold = 0.05f;

    private bool isWalking = false;

    public void StartWalking()
    {
        if (xrOrigin == null || xrCamera == null || targetPosition == null)
        {
            Debug.LogWarning("⚠ Faltan referencias");
            return;
        }

        if (!isWalking)
            StartCoroutine(WalkRoutine());
    }

    private IEnumerator WalkRoutine()
    {
        isWalking = true;

        // Posición actual de la cabeza (mundo)
        Vector3 cameraWorld = xrCamera.position;
        // Offset entre XR Origin y la cámara
        Vector3 offset = xrOrigin.position - cameraWorld;

        // Calculamos destino del XR Origin usando el offset
        Vector3 destinationRig = targetPosition.position + offset;

        // 1️⃣ Mover solo en X
        while (Mathf.Abs(xrOrigin.position.x - destinationRig.x) > arriveThreshold)
        {
            float step = moveSpeed * Time.deltaTime * Mathf.Sign(destinationRig.x - xrOrigin.position.x);
            xrOrigin.position += new Vector3(step, 0, 0);
            yield return null;
        }

        // 2️⃣ Mover solo en Z
        while (Mathf.Abs(xrOrigin.position.z - destinationRig.z) > arriveThreshold)
        {
            float step = moveSpeed * Time.deltaTime * Mathf.Sign(destinationRig.z - xrOrigin.position.z);
            xrOrigin.position += new Vector3(0, 0, step);
            yield return null;
        }

        // Ajuste final exacto
        xrOrigin.position = new Vector3(destinationRig.x, xrOrigin.position.y, destinationRig.z);

        Debug.Log("✅ Movimiento completado y manos sincronizadas");
        isWalking = false;
    }
}
