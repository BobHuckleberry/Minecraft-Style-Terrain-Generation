using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Player Transforms")]
    public Transform handTransform;
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public float stamina = 100f;
    public float staminaDepletionRate = 1f;
    public float staminaRegenRate = 5f;

    [Header("Look Settings")]
    public float lookSensitivity = 1f;
    public Transform playerCamera;

    [Header("Crouch Settings")]
    public float crouchScale = 0.5f;
    public float crouchTransitionSpeed = 5f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;
    private bool isGrounded;

    private Vector2 movementInput;
    private Vector2 lookInput;
    private CharacterController characterController;
    private float verticalLookRotation = 0f;
    private float verticalVelocity = 0f;

    private GameObject equippedItemInstance;

    private float currentSpeed;
    private bool isRunning = false;
    private bool isCrouching = false;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component missing on player!");
        }

        if (playerCamera == null)
        {
            Debug.LogError("Player Camera not assigned!");
        }

        originalScale = transform.localScale;
        targetScale = originalScale;
        currentSpeed = walkSpeed;



        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleMovement();
        HandleLooking();
        HandleCrouchTransition();
    }

    private void HandleMovement()
    {

        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = 0f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Determine current speed based on state
        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
            if (stamina < 100f)
            {

                stamina += Time.deltaTime * staminaRegenRate;
            }
        }
        else if (isRunning && stamina > 0)
        {
            //TODO: Add movement speed increase instead of flat run speed boost
            currentSpeed = runSpeed;
            stamina -= Time.deltaTime * staminaDepletionRate;
        }
        else
        {
            currentSpeed = walkSpeed;
            if (stamina < 100f)
            {

                stamina += Time.deltaTime * staminaRegenRate;
            }
        }
        if (isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = 0f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Calculate horizontal movement
        Vector3 horizontalMove = (transform.right * movementInput.x + transform.forward * movementInput.y) * currentSpeed;

        // Combine with vertical movement (gravity/jump)
        Vector3 move = horizontalMove;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime);
    }

    private void HandleLooking()
    {
        if (playerCamera == null) return;

        transform.Rotate(Vector3.up * lookInput.x * lookSensitivity);

        verticalLookRotation -= lookInput.y * lookSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(verticalLookRotation, 0f, 0f);
    }

    private void HandleCrouchTransition()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * crouchTransitionSpeed);
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (value.isPressed && stamina > 0f)
        {
            // Start running if sprint is pressed and stamina is available
            isRunning = true;
        }
        else
        {
            // Stop running if sprint is released or stamina is depleted
            isRunning = false;
        }
    }

    public void OnJump(InputValue value)
    {
        //idk how isGrounded works but it works
        if (value.isPressed && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
    }
    public void OnCrouch(InputValue value)
    {
        if (value.isPressed)
        {
            //if it isnt crouched, scale the player down, if it is crouched, scale the player back up
            isCrouching = !isCrouching;
            targetScale = isCrouching
                ? new Vector3(originalScale.x, originalScale.y * crouchScale, originalScale.z)
                : originalScale;
        }
    }
}