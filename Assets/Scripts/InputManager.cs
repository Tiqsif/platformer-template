using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static PlayerInput PlayerInputInstance;

    public static Vector2 Movement;
    public static bool JumpWasPressed;
    public static bool JumpWasReleased;
    public static bool JumpIsHeld;
    public static bool DashWasPressed;
    public static bool DashIsHeld;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;

    private void Awake()
    {
        PlayerInputInstance = GetComponent<PlayerInput>();
        _moveAction = PlayerInputInstance.actions["Move"];
        _jumpAction = PlayerInputInstance.actions["Jump"];
        _dashAction = PlayerInputInstance.actions["Dash"];
    }

    private void Update()
    {
        Movement = _moveAction.ReadValue<Vector2>();
        JumpWasPressed = _jumpAction.WasPressedThisFrame();
        JumpWasReleased = _jumpAction.WasReleasedThisFrame();
        JumpIsHeld = _jumpAction.IsPressed();

        DashWasPressed = _dashAction.WasPressedThisFrame();
        DashIsHeld = _dashAction.IsPressed();
    }
}
