using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabOutlineMesh : MonoBehaviour
{
    public Material outlineMaterial;   // Material con el shader de contorno
    private GameObject outlineObj;     // La malla duplicada
    private XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // Crear segunda malla para el contorno
        CreateOutlineMesh();

        // Eventos de interacción
        grab.hoverEntered.AddListener(OnHoverEnter);
        grab.hoverExited.AddListener(OnHoverExit);
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void CreateOutlineMesh()
    {
        // Crear un objeto hijo con MeshFilter/MeshRenderer
        outlineObj = new GameObject("OutlineMesh");
        outlineObj.transform.SetParent(transform, false);

        MeshFilter originalMF = GetComponent<MeshFilter>();
        MeshRenderer originalMR = GetComponent<MeshRenderer>();

        if (originalMF != null && originalMR != null)
        {
            MeshFilter mf = outlineObj.AddComponent<MeshFilter>();
            mf.sharedMesh = originalMF.sharedMesh;

            MeshRenderer mr = outlineObj.AddComponent<MeshRenderer>();
            mr.material = outlineMaterial;
        }

        outlineObj.SetActive(false); // Inactivo por defecto
    }

    void OnHoverEnter(HoverEnterEventArgs args) => outlineObj.SetActive(true);
    void OnHoverExit(HoverExitEventArgs args) => outlineObj.SetActive(false);
    void OnGrab(SelectEnterEventArgs args) => outlineObj.SetActive(true);
    void OnRelease(SelectExitEventArgs args) => outlineObj.SetActive(false);
}
