using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

/// <summary>
/// Bridges Unity's Input System (InputActions) → Fusion's network input pipeline
/// Accumulates input every frame and sends it to Fusion every tick via OnInput()
/// </summary>
public class FusionInputProvider : SimulationBehaviour, INetworkRunnerCallbacks
{
    // Input Actions reference (generated from InputSystem_Actions.inputactions)
    private InputSystem_Actions controls;

    // Accumulated input between ticks
    private NetworkInputData accumulatedInput;
    private bool resetInput = false;

    // Track button states
    private bool jumpPressed = false;
    private bool sprintHeld = false;
    private bool crouchPressed = false;
    private bool attackPressed = false;
    private bool interactPressed = false;
    private bool inventoryPressed = false;
    private bool inputEnabled = true;

    private void Awake()
    {
        // Initialize Input Actions
        controls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        controls.Enable();

        // Register with NetworkRunner for callbacks
        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }
    }

    private void OnDisable()
    {
        controls.Disable();

        // Unregister from NetworkRunner
        if (Runner != null)
        {
            Runner.RemoveCallbacks(this);
        }
    }

    private void Update()
    {
        // Note: ESC key pause handling is now managed by PauseMenuController

        if (!inputEnabled)
        {
            ClearAccumulatedInput();
            return;
        }

        // Only sample input if cursor is locked (in game)
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            ClearAccumulatedInput();
            return;
        }

        // Reset accumulated input if needed (after a tick without a frame)
        if (resetInput)
        {
            accumulatedInput.move = Vector2.zero;
            accumulatedInput.look = Vector2.zero;
            resetInput = false;
        }

        // POLL input continuously every frame (not using callbacks!)
        var player = controls.Player;

        // Movement - Read current value every frame
        accumulatedInput.move = player.Move.ReadValue<Vector2>();

        // Look - Add mouse delta each frame (sensitivity applied in NetworkPlayer)
        Vector2 lookDelta = player.Look.ReadValue<Vector2>();
        accumulatedInput.look += lookDelta;

        // Buttons - Check state every frame
        if (player.Jump.WasPressedThisFrame())
        {
            jumpPressed = true;
        }

        sprintHeld = player.Sprint.IsPressed();

        if (player.Crouch.WasPressedThisFrame())
        {
            crouchPressed = true;
        }

        if (player.Attack.WasPressedThisFrame())
        {
            attackPressed = true;
        }

        if (player.Interact.WasPressedThisFrame())
        {
            interactPressed = true;
        }

        // Inventory (TAB) - Use Keyboard directly since it's not in InputActions
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            inventoryPressed = true;
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!inputEnabled)
        {
            input.Set(new NetworkInputData());
            return;
        }

        // This is called by Fusion every tick to get input data
        var data = new NetworkInputData
        {
            move = accumulatedInput.move,
            look = accumulatedInput.look  // Send accumulated mouse delta
        };

        // Set button states using bit flags
        data.buttons.Set(InputButtons.Jump, jumpPressed);
        data.buttons.Set(InputButtons.Sprint, sprintHeld);
        data.buttons.Set(InputButtons.Crouch, crouchPressed);
        data.buttons.Set(InputButtons.Attack, attackPressed);
        data.buttons.Set(InputButtons.Interact, interactPressed);
        data.buttons.Set(InputButtons.Inventory, inventoryPressed);

        // Send to Fusion
        input.Set(data);

        // Reset one-shot buttons (jump, crouch, attack, interact, inventory)
        jumpPressed = false;
        crouchPressed = false;
        attackPressed = false;
        interactPressed = false;
        inventoryPressed = false;

        // IMPORTANT: Reset look delta immediately to prevent applying same mouse movement multiple times
        // This prevents sensitivity fluctuation when multiple ticks happen in one frame
        accumulatedInput.look = Vector2.zero;

        // Mark that we need to reset movement input if next tick happens without a frame
        resetInput = true;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only lock cursor if we're in the game scene (not MainMenu)
        if (player == runner.LocalPlayer && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
        {
            LockCursor(true);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Optional: Handle player leaving
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // Unlock cursor on disconnect
        LockCursor(false);

        Debug.Log($"[FusionInputProvider] Shutdown: {shutdownReason}");
    }

    private void ToggleCursorLock()
    {
        bool isLocked = Cursor.lockState == CursorLockMode.Locked;
        LockCursor(!isLocked);
    }

    private void LockCursor(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[FusionInputProvider] Cursor locked");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[FusionInputProvider] Cursor unlocked");
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            ClearAccumulatedInput();
        }
    }

    private void ClearAccumulatedInput()
    {
        accumulatedInput = default;
        resetInput = false;
        jumpPressed = false;
        crouchPressed = false;
        attackPressed = false;
        interactPressed = false;
        inventoryPressed = false;
        sprintHeld = false;
    }

    #region INetworkRunnerCallbacks - Empty implementations
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}
