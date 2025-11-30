using UnityEngine;

// Enum para los diferentes modos de juego
public enum GameMode
{
    None,
    Chef,      // Controlar el sartén
    Soldado    // Disparar al pájaro
}

// Clase para compartir información entre escenas
public static class GameModeManager
{
    private static GameMode currentMode = GameMode.None;
    
    public static GameMode CurrentMode
    {
        get => currentMode;
        set
        {
            currentMode = value;
            Debug.Log($"[GameMode] Modo cambiado a: {currentMode}");
        }
    }
    
    public static bool IsChefMode => currentMode == GameMode.Chef;
    public static bool IsSoldadoMode => currentMode == GameMode.Soldado;
}
