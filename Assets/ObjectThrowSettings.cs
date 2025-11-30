using UnityEngine;


public class ObjectThrowSettings : MonoBehaviour
{
    public float velocityScale = 0.5f;
    public float angularVelocityScale = 0.5f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            grab.throwOnDetach = true;
            grab.velocityScale = velocityScale;
            grab.angularVelocityScale = angularVelocityScale;
        }
    }
}
