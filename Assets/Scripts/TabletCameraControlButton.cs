using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TabletCameraControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private TabletConnectionManager connectionManager;
    
    [Header("Button Settings")]
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.yellow;
    
    private Image buttonImage;
    private bool isPressed = false;
    
    void Start()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        if (button != null)
        {
            buttonImage = button.GetComponent<Image>();
        }
        
        if (connectionManager == null)
        {
            connectionManager = FindObjectOfType<TabletConnectionManager>();
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isPressed)
        {
            isPressed = true;
            SendCameraCommand("move");
            
            if (buttonImage != null)
            {
                buttonImage.color = pressedColor;
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPressed)
        {
            isPressed = false;
            SendCameraCommand("reset");
            
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }
    }
    
    private void SendCameraCommand(string action)
    {
        if (connectionManager != null)
        {
            CameraControlMessage msg = new CameraControlMessage
            {
                type = "camera_control",
                action = action
            };
            
            string json = JsonUtility.ToJson(msg);
            connectionManager.SendToServer(json);
        }
    }
    
    void OnDisable()
    {
        if (isPressed)
        {
            isPressed = false;
            SendCameraCommand("reset");
        }
    }
}

[System.Serializable]
public class CameraControlMessage
{
    public string type;
    public string action;
}