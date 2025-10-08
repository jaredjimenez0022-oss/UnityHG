using UnityEngine;
using UnityEngine.InputSystem;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color crosshairColor = Color.white;
    public float size = 5f;
    public float gap = 10f;

    private bool isAiming = false;

    void Update()
    {
        if (Mouse.current != null)
        {
            isAiming = Mouse.current.rightButton.isPressed;
        }
    }

    void OnGUI()
    {
        if (!isAiming) return;

        Color oldColor = GUI.color;
        GUI.color = crosshairColor;

        float xCenter = Screen.width / 2f;
        float yCenter = Screen.height / 2f;

        GUI.DrawTexture(new Rect(xCenter - size / 2, yCenter - gap - size, size, size), Texture2D.whiteTexture); //Arriba
        GUI.DrawTexture(new Rect(xCenter - size / 2, yCenter + gap, size, size), Texture2D.whiteTexture);        //Abajo
        GUI.DrawTexture(new Rect(xCenter - gap - size, yCenter - size / 2, size, size), Texture2D.whiteTexture); //Izquierda
        GUI.DrawTexture(new Rect(xCenter + gap, yCenter - size / 2, size, size), Texture2D.whiteTexture);        //Derecha

        GUI.color = oldColor;
    }
}
