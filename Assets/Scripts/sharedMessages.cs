using System;

/// <summary>
/// Mensaje de puntaje que se envía de VR a Tablet
/// Este archivo debe estar TANTO en VR como en Tablet
/// </summary>
[Serializable]
public class ScoreMessage
{
    public string type;           // "score"
    public string mode;           // "Chef" o "Soldado"
    public int chefScore;         // Puntaje del Chef
    public int soldadoScore;      // Puntaje del Soldado
    public int currentScore;      // Puntaje del modo actual
}

/// <summary>
/// Clase auxiliar para identificar el tipo de mensaje
/// </summary>
[Serializable]
public class MessageType
{
    public string type;  // "score", "mode", etc.
}