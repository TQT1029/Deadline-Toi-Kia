using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-11)]
public class InputManager : Singleton<InputManager>
{
    [Header("Values")]
    public Vector2 MoveInput;
    public Vector2 LookInput;
    public bool IsJumpPressed { get; private set; }

    // --- Unity Events Binding ---
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) IsJumpPressed = true;
        if (context.canceled) IsJumpPressed = false;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed && GameManager.Instance != null)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        var current = GameManager.Instance.CurrentState;
        if (current == GameState.Gameplay)
            GameManager.Instance.ChangeState(GameState.Paused);
        else if (current == GameState.Paused)
            GameManager.Instance.ChangeState(GameState.Gameplay);
    }
}