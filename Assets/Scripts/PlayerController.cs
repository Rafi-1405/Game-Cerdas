using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerMovementState
{
    Idle,
    Sneak,
    Walk,
    Run
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float sneakSpeed = 2f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    // Properti publik agar status pemain bisa dibaca oleh sensor NPC
    public PlayerMovementState CurrentState { get; private set; }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = 0f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;

        float vertical = 0f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        // Menentukan status (State) dan kecepatan berdasarkan input tambahan
        float currentSpeed = walkSpeed;

        if (movement == Vector3.zero)
        {
            CurrentState = PlayerMovementState.Idle;
            currentSpeed = 0f;
        }
        else if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
        {
            CurrentState = PlayerMovementState.Sneak;
            currentSpeed = sneakSpeed;
        }
        else if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
        {
            CurrentState = PlayerMovementState.Run;
            currentSpeed = runSpeed;
        }
        else
        {
            CurrentState = PlayerMovementState.Walk;
            currentSpeed = walkSpeed;
        }

        // Terapkan pergerakan
        transform.position += movement * currentSpeed * Time.deltaTime;

        // Terapkan rotasi jika sedang bergerak
        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}