using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Scripts;

//Modificar para que tenga como requisito para activarse el crosshair que el personaje tenga equipada un arma
public class Crosshair : NetworkBehaviour
{
    [Header("Crosshair Settings")]
    public Color crosshairColor = Color.white;
    public float size = 5f;
    public float gap = 10f;

    [Networked]
    private bool isAiming { get; set; }

    private PlayerController weaponManager;

    public override void Spawned()
    {
        //weaponManager = GetComponent <PlayerController>(); //Descomentar cuando se integre sistema de armas
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (Mouse.current != null) 
        {
            isAiming = Mouse.current.rightButton.isPressed;
        }
    }

    void OnGUI()
    {
        if (!HasStateAuthority) return;
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

    public void SetAiming(bool aiming) 
    {
        if (HasStateAuthority)
        {
            isAiming = aiming;
        }
    }

}
