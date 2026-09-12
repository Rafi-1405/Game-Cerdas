using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerMovementState
{
    Idle,
    Sneak,
    Walk,
    Run
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float sneakSpeed = 2f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    public PlayerMovementState CurrentState { get; private set; }

    private Rigidbody rb;
    private Vector3 movementInput;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

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

        // Store direction to class variable movementInput so FixedUpdate can use it
        movementInput = new Vector3(horizontal, 0f, vertical).normalized;

        // Determine state and calculate speed into class-level variable currentSpeed
        if (movementInput == Vector3.zero)
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

        // Apply visual rotation using movementInput
        if (movementInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        // Move using Rigidbody position update in physics loop
        Vector3 targetPosition = rb.position + movementInput * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }
}