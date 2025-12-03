using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-10)]
public class InputManager : Singleton<InputManager>
{
    [Header("Values")]
    public Vector2 MoveInput;
    public Vector2 LookInput; // Thường dùng cho mouse delta

    // Các sự kiện để script khác đăng ký
    public bool IsJumpPressed { get; private set; }

    //===== Unity Events Binding =====//
    // Gán các hàm này vào PlayerInput Component (Invoke Unity Events)

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
        if (context.performed)
        {
            TogglePause();
        }
    }

    // Logic toggle pause cơ bản
    private void TogglePause()
    {
        if (GameManager.Instance.CurrentState == GameState.Gameplay)
            GameManager.Instance.ChangeState(GameState.Paused);
        else if (GameManager.Instance.CurrentState == GameState.Paused)
            GameManager.Instance.ChangeState(GameState.Gameplay);
    }
}