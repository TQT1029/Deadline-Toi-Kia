using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }


    private void FixedUpdate()
    {
        Movement();
    }

    private void Movement()
    {
        Vector2 moveInput = InputManager.Instance.MoveInput;
        _rb.linearVelocity = new Vector2(moveInput.y, _rb.linearVelocity.y);
    }

}
