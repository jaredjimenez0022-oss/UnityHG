using Fusion;
using UnityEngine;

/// <summary>
/// Button flags for network input
/// </summary>
[System.Flags]
public enum InputButtons
{
    Jump = 1 << 0,      // Bit 0
    Sprint = 1 << 1,    // Bit 1
    Crouch = 1 << 2,    // Bit 2
    Attack = 1 << 3,    // Bit 3
    Interact = 1 << 4,  // Bit 4
    Inventory = 1 << 5, // Bit 5
}

/// <summary>
/// Network input data structure sent to Fusion every tick
/// Implements INetworkInput for Fusion serialization
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public NetworkButtons buttons;  // Bit flags for button states
    public Vector2 move;            // Movement input (WASD) - x=strafe, y=forward
    public Vector2 look;            // Mouse delta for camera rotation
}
