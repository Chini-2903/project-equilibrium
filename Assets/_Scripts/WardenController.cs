using UnityEngine;

public class WardenController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseWalkSpeed = 5f;
    [HideInInspector] public float currentWalkSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float upDownRange = 80f; // Stops your neck from snapping backwards

    private CharacterController controller;
    private float verticalRotation;
    private Vector3 velocity;

    void Start()
    {
        // Get the Character Controller attached to the player
        controller = GetComponent<CharacterController>();

        // Lock the mouse cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Get mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Up/Down looking (rotating the camera)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Left/Right looking (rotating the whole player body)
        transform.Rotate(0f, mouseX, 0f);
    }

    void HandleMovement()
    {
        // Get WASD inputs
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calculate movement direction based on where the player is facing
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Move the player horizontally
        controller.Move(move * currentWalkSpeed * Time.deltaTime);

        // Apply basic gravity so we don't float away
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Keeps the player snapped to the floor
        }

        velocity.y += gravity * Time.deltaTime;

        // Apply vertical movement (falling)
        controller.Move(velocity * Time.deltaTime);
    }
}