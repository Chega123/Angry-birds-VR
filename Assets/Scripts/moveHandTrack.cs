using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class PalmPushLocomotion : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 1.5f;              // Velocidad de movimiento
    public Transform xrOrigin;                  // XR Origin (XR Rig)
    public XRHandSubsystem handSubsystem;
    public bool useRightHand = true;            // Mano que controla el movimiento

    [Header("Filtro de gestos")]
    public float holdTime = 0.5f;               // Tiempo mínimo sosteniendo la pose
    public float directionThreshold = 0.75f;    // Umbral para considerar palma "adelante"

    private float holdTimer = 0f;               // Contador interno

    void Start()
    {
        if (handSubsystem == null)
        {
            handSubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRHandSubsystem>();
        }
    }

    void Update()
    {
        if (handSubsystem == null || xrOrigin == null) return;

        XRHand hand = useRightHand ? handSubsystem.rightHand : handSubsystem.leftHand;

        if (hand.isTracked)
        {
            var palm = hand.GetJoint(XRHandJointID.Palm);

            if (palm.TryGetPose(out Pose palmPose))
            {
                // Normal de la palma (en XRHand suele ser "up")
                Vector3 palmNormal = palmPose.up;

                // Usamos la dirección de la palma en el plano horizontal
                Vector3 moveDir = new Vector3(palmNormal.x, 0, palmNormal.z).normalized;

                // Si la palma apunta hacia adelante (dot con cámara.forward)
                float dot = Vector3.Dot(moveDir, Camera.main.transform.forward);

                if (dot > directionThreshold) // Palma suficientemente hacia adelante
                {
                    holdTimer += Time.deltaTime;

                    if (holdTimer >= holdTime)
                    {
                        xrOrigin.position += moveDir * moveSpeed * Time.deltaTime;
                    }
                }
                else
                {
                    // Si se pierde la pose, reinicia el temporizador
                    holdTimer = 0f;
                }
            }
        }
    }
}
