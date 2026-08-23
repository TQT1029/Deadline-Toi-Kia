using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-11)]
public class InputManager : Singleton<InputManager>
{
    [Header("Values")]
    public static Vector2 CurrentMoveInput { get; private set; }
    public static Vector2 CurrentLookInput { get; private set; }
    public static bool IsJumpHolding { get; private set; }

    // --- Input Events ---
    public static event Action OnJumpDown;
    public static event Action OnJumpUp;
    public static event Action OnPauseToggle;
    public static event Action<Vector2> OnMoveChanged;

    private bool _newInputActiveThisFrame = false;

    // --- Unity Events Binding (New Input System) ---
    public void OnMove(InputAction.CallbackContext context)
    {
        _newInputActiveThisFrame = true;
        CurrentMoveInput = context.ReadValue<Vector2>();
        OnMoveChanged?.Invoke(CurrentMoveInput);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        CurrentLookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        _newInputActiveThisFrame = true;
        if (context.started || context.performed)
        {
            IsJumpHolding = true;
            OnJumpDown?.Invoke();
        }
        else if (context.canceled)
        {
            IsJumpHolding = false;
            OnJumpUp?.Invoke();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        _newInputActiveThisFrame = true;
        if (context.performed)
        {
            TogglePause();
        }
    }

    private void Update()
    {
        // Chỉ chạy fallback nếu New Input System không phát sự kiện trong frame này
        if (!_newInputActiveThisFrame)
        {
            HandleLegacyFallbackInput();
        }
    }

    private void LateUpdate()
    {
        _newInputActiveThisFrame = false;
    }

    private void HandleLegacyFallbackInput()
    {
        // Kiểm tra Jump Down
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            IsJumpHolding = true;
            OnJumpDown?.Invoke();
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space))
        {
            IsJumpHolding = false;
            OnJumpUp?.Invoke();
        }

        // Kiểm tra Pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (GameManager.Instance == null) return;

        var current = GameManager.Instance.CurrentState;
        if (current == GameState.Playing)
        {
            GameManager.Instance.ChangeState(GameState.Paused);
            OnPauseToggle?.Invoke();
        }
        else if (current == GameState.Paused)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
            OnPauseToggle?.Invoke();
        }
    }
}