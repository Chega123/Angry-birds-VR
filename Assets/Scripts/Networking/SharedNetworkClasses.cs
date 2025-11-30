using System;
using UnityEngine;

[Serializable]
public class TabletInput
{
    public float screenX;
    public float screenY;
    public string action; // "Began", "Moved", "Ended"
    public int touchId;
}