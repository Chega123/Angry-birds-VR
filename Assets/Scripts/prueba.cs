using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public class TestGrab : MonoBehaviour
{
    void Start()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            Debug.Log($"✅ XRGrabInteractable encontrado en {gameObject.name}");
            Debug.Log($"   Interaction Manager: {grab.interactionManager}");
            Debug.Log($"   Colliders: {grab.colliders.Count}");

            grab.selectEntered.AddListener((args) => {
                Debug.Log($"🎯 TEST: Objeto agarrado!");
            });
        }
        else
        {
            Debug.LogError($"❌ NO se encontró XRGrabInteractable!");
        }
    }
}