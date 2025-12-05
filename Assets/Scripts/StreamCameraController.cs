using UnityEngine;
using System.Collections;

public class StreamCameraController : MonoBehaviour
{
    [Header("Target Transform")]
    [SerializeField] private Vector3 targetPosition = new Vector3(0, 15, -10);
    [SerializeField] private Vector3 targetRotation = new Vector3(60, 0, 0);
    
    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 3f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isMoving = false;
    private Coroutine currentCoroutine;
    private Camera streamCamera;
    
    void Start()
    {
        streamCamera = GetComponent<Camera>();
        
        if (streamCamera != null)
        {
            originalPosition = streamCamera.transform.localPosition;
            originalRotation = streamCamera.transform.localRotation;
        }
    }
    
    public void MoveToTarget()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        currentCoroutine = StartCoroutine(MoveCamera(targetPosition, Quaternion.Euler(targetRotation), true));
    }
    
    public void ResetToOriginal()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        currentCoroutine = StartCoroutine(MoveCamera(originalPosition, originalRotation, false));
    }
    
    private IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot, bool moving)
    {
        isMoving = moving;
        
        Vector3 startPos = streamCamera.transform.localPosition;
        Quaternion startRot = streamCamera.transform.localRotation;
        
        float elapsed = 0f;
        float duration = 1f / transitionSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = transitionCurve.Evaluate(t);
            
            streamCamera.transform.localPosition = Vector3.Lerp(startPos, targetPos, curveT);
            streamCamera.transform.localRotation = Quaternion.Slerp(startRot, targetRot, curveT);
            
            yield return null;
        }
        
        streamCamera.transform.localPosition = targetPos;
        streamCamera.transform.localRotation = targetRot;
        
        isMoving = moving;
    }
    
    public void OnCameraControlMessage(string action)
    {
        if (action == "move")
        {
            MoveToTarget();
        }
        else if (action == "reset")
        {
            ResetToOriginal();
        }
    }
}