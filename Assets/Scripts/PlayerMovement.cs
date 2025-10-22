using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Animator animator;
    public InputActionAsset playerActions;
    public Transform cameraPivot;
    public CinemachineCamera cinemachineCam;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -9.81f;

    private float currentSpeed;
    private float targetSpeed;
    private Vector3 velocity;
    private bool _isSprinting;
    private Vector2 moveInput;

    [Header("Camera Settings")]
    [SerializeField] private float cameraSensitivity = 120f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;
    private float yaw;
    private float pitch;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction lookAction;
    private int velocityHash;

    public bool IsSprinting
    {
        get => _isSprinting;
        set
        {
            _isSprinting = value;
            targetSpeed = value ? sprintSpeed : walkSpeed;
        }
    }

    void Awake()
    {
        moveAction = playerActions.FindAction("move");
        sprintAction = playerActions.FindAction("Sprint");
        lookAction = playerActions.FindAction("Look");
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        velocityHash = Animator.StringToHash("Velocity");
        targetSpeed = walkSpeed;

        // Initialize yaw/pitch from current cameraPivot
        Vector3 angles = cameraPivot.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void OnEnable()
    {
        playerActions.Enable();
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
    }

    void OnDisable()
    {
        playerActions.Disable();
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
    }

    void Update()
    {
        HandleSprintToggle();
        HandleCameraLook();
        HandleMovement();
        HandleAnimation();
    }

    void HandleSprintToggle()
    {
        if (sprintAction != null && sprintAction.WasPressedThisFrame())
            IsSprinting = !IsSprinting;
    }

    void HandleCameraLook()
    {
        Vector2 lookDelta = lookAction.ReadValue<Vector2>();

        // On mobile, lookAction will be triggered by right-side drag gesture
        yaw += lookDelta.x * cameraSensitivity * Time.deltaTime;
        pitch -= lookDelta.y * cameraSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Apply rotation to the camera pivot
        cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        // Smoothly interpolate movement speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // Read movement vector
        Vector3 moveDir = new(moveInput.x, 0f, moveInput.y);
        moveDir = cameraPivot.forward * moveDir.z + cameraPivot.right * moveDir.x;
        moveDir.y = 0f;
        moveDir.Normalize();

        // Rotate player towards movement direction
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        // Apply motion
        Vector3 move = moveDir * currentSpeed + new Vector3(0, velocity.y, 0);
        controller.Move(move * Time.deltaTime);
    }

    void HandleAnimation()
    {
        Vector3 horizontalVelocity = new(controller.velocity.x, 0, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;
        float normalizedSpeed = Mathf.InverseLerp(0f, sprintSpeed, speed);

        animator.SetFloat(velocityHash, normalizedSpeed);
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
