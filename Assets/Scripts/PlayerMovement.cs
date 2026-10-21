using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Pulo")]
    public float jumpForce = 6f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.2f;

    [Header("Câmera")]
    public Transform cameraTransform;

    [Header("Fome")]
    public PlayerHunger hunger;

    [Header("Animação")]
    public Animator animator;

    [Header("Efeitos externos")]
    [Range(0f, 1f)]
    public float warmthSpeedMultiplier = 1f;

    [Range(0f, 1f)]
    public float oxygenSpeedMultiplier = 1f;

    [Range(0f, 1f)]
    public float hungerSpeedMultiplier = 1f;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isRunning;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        warmthSpeedMultiplier = 1f;
        oxygenSpeedMultiplier = 1f;
        hungerSpeedMultiplier = 1f;
    }

    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateAnimator();

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        MoveCharacter();
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        bool isMoving = inputDir.magnitude >= 0.1f;

        if (hunger != null)
        {
            if (wantsToRun && isMoving)
            {
                isRunning = true;
            }
            else
            {
                isRunning = false;
            }
        }
        else
        {
            isRunning = wantsToRun;
        }

        if (isMoving && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveDirection = (camForward * vertical + camRight * horizontal).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        currentSpeed =
    (isRunning ? runSpeed : walkSpeed) *
    warmthSpeedMultiplier *
    oxygenSpeedMultiplier *
    hungerSpeedMultiplier;
    }

    void MoveCharacter()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;

            rb.linearVelocity = new Vector3(
                targetVelocity.x,
                rb.linearVelocity.y,
                targetVelocity.z
            );

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        float animationSpeed;

        if (isRunning)
        {
            animationSpeed = runSpeed;
        }
        else if (moveDirection.magnitude >= 0.1f)
        {
            animationSpeed = walkSpeed;
        }
        else
        {
            animationSpeed = 0f;
        }

        animator.SetFloat("Speed", animationSpeed);
    }

    void CheckGrounded()
    {
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }
}