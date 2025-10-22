using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    public Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Input")]
    private float moveInput;
    private float turnInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        InputManagement();
        Movement();
        HandleAnimation();
    }

    private void Movement()
    {
        Vector3 move = new Vector3(moveInput, 0f, turnInput);
        move *= moveSpeed;
        characterController.Move(move * Time.deltaTime);
    }

    private void InputManagement()
    {
        moveInput = Input.GetAxis("Horizontal");
        turnInput = Input.GetAxis("Vertical");
    }

    private void HandleAnimation()
    {
        animator.SetBool("walking", characterController.velocity != Vector3.zero);
    }
}
