using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public Animator animator;
    public InputActionAsset playerActions;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Input")]
    private Vector2 moveInput;
   

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        playerActions.Enable();
        playerActions["move"].performed += OnMove;
        playerActions["move"].canceled += OnMove;
    }

    void OnDisable()
    {
        playerActions.Disable();
        playerActions["move"].performed -= OnMove;
        playerActions["move"].canceled -= OnMove;
    }
    // Update is called once per frame
    void Update()
    {
        //InputManagement();
        Movement();
        HandleAnimation();
    }

    private void Movement()
    {
        Vector3 move = new(moveInput.x, 0f, moveInput.y);
        move *= moveSpeed;
        characterController.Move(move * Time.deltaTime);
    }

    //private void InputManagement()
    //{
    //    moveInput = Input.GetAxis("Horizontal");
    //    turnInput = Input.GetAxis("Vertical");
    //}

    private void HandleAnimation()
    {
        animator.SetBool("Walking", characterController.velocity != Vector3.zero);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
