using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// NOTE: This uses Rigidbody2D.velocity / .drag, which work on Unity 2022/2023 and earlier.
// If you're on Unity 6+, those are obsolete - swap every `rb.velocity` for `rb.linearVelocity`
// and `rb.drag` for `rb.linearDamping`.
public class GrayMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private SpriteRenderer spriteRenderer; // optional, used for facing flip
    [SerializeField] private Animator animator;


    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float groundDrag = 4f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Double Jump")]
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private bool enableDebugDoubleJump = false;
    [SerializeField] private float cartwheelDuration = 0.25f;

    [Header("Drag Object References")]
    [Tooltip("Currently dragged object. Set by mouse click.")]
    [SerializeField] private GameObject draggedObject;
    private bool isDraggingObject;

    private float horizontalInput;
    private bool isGrounded;
    private bool isRunning;

    private int jumpCount;
    private bool wasGrounded;
    private bool jumpRequested;
    private ContactPoint2D[] contactPoints = new ContactPoint2D[16];
    private Coroutine cartwheelCoroutine;

    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        jumpCount = ActiveJumpCount;

        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
            Debug.LogWarning($"{name}: groundCheckPoint wasn't assigned, so using the character root transform as fallback. Create a child object at the feet for more accurate ground detection.", this);
        }
    }

    void Update()
    {
        if (!PlayerManager.Instance.IsAlive)
        {
            return;
        }
        
        HandleInput();
        UpdateAnimation();
    }

    void FixedUpdate()
{
    CheckGrounded();
    
    if (isGrounded && !wasGrounded)
    {
        jumpCount = ActiveJumpCount;
    }

    ApplyDrag();
    ApplyFallMultiplier();
    MovePlayer();
    TryConsumeJump();
    FlipSprite();
    wasGrounded = isGrounded;
}

    private void HandleInput()
    {
        if (Keyboard.current == null)
        {
            horizontalInput = 0f;
            isRunning = false;
            return;
        }

        bool left = Keyboard.current.aKey.isPressed;
        bool right = Keyboard.current.dKey.isPressed;
        horizontalInput = (right ? 1f : 0f) - (left ? 1f : 0f); // opposing keys cancel out instead of A always winning

        isRunning = Keyboard.current.shiftKey.isPressed;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        // Mouse click attempt
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse pressed.");
            MouseClick();
        }

        if (isDraggingObject)
        {
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Debug.Log("Object let go of.");
                MouseLetGo();
            }
        }
    }

    private void TryConsumeJump()
    {
        if (!jumpRequested)
            return;

        if (!isGrounded && jumpCount == 0)
        {
            jumpRequested = false;
            return;
        }

        bool isSecondJump = !isGrounded && jumpCount == 1 && ActiveJumpCount > 1;
        Jump(isSecondJump);
        jumpRequested = false;
    }

    private void MovePlayer()
    {
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        rb.velocity = new Vector2(horizontalInput * currentSpeed, rb.velocity.y);
    }

    private void Jump(bool isSecondJump)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpCount = Mathf.Max(0, jumpCount - 1);

        if (isSecondJump)
            StartCartwheel();
    }

    private void MouseClick()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        // Cast a ray at that exact point in 2D space
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);

        // Check if the ray hit a 2D collider
        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;
            Debug.Log("Globally detected click on: " + clickedObject.name);
            // Interact with the object here
            if (clickedObject.CompareTag("RedMoveable") && 
            // clickedObject.TryGetComponent<RedBlockMoveable>(out RedBlockMoveable redBlock)
            clickedObject.GetComponent<RedBlockMoveable>() != null)
            {
                // Sets the player's currently "dragged object" as the object detected from raycast.
                Debug.Log("Object is moveable.");
                draggedObject = clickedObject;

                RedBlockMoveable redBlockMoveable = draggedObject.GetComponent<RedBlockMoveable>();

                isDraggingObject = true;
                redBlockMoveable.isDragged = true;
                redBlockMoveable.SetOffset();
            }
        }
    }

    private void MouseLetGo()
    {
        RedBlockMoveable redBlockMoveable = draggedObject.GetComponent<RedBlockMoveable>();
        isDraggingObject = false;
        redBlockMoveable.isDragged = false;
        draggedObject = null;
    }
 
    private int ActiveJumpCount => (Debug.isDebugBuild && enableDebugDoubleJump) ? maxJumpCount : 1;

    private void CheckGrounded()
    {
        if (groundCheckPoint == null)
        {
            isGrounded = false;
            return;
        }

        int contactCount = rb.GetContacts(contactPoints);
        isGrounded = false;

        for (int i = 0; i < contactCount; i++)
        {
            if (contactPoints[i].normal.y > 0.65f)
                isGrounded = true;
        }

        if (!isGrounded)
        {
            bool touchingGround = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            if (touchingGround)
                isGrounded = true;

            if (!isGrounded)
            {
                RaycastHit2D hit = Physics2D.CircleCast(
                    groundCheckPoint.position + Vector3.up * 0.05f,
                    groundCheckRadius,
                    Vector2.down,
                    groundCheckRadius * 1.2f,
                    groundLayer
                );

                if (hit.collider != null && hit.normal.y > 0.65f)
                    isGrounded = true;
            }
        }

        if (isGrounded && rb.velocity.y > 0.2f)
            isGrounded = false;
    }

    private void ApplyDrag()
    {
        rb.drag = isGrounded ? groundDrag : 0f;
    }

    private void ApplyFallMultiplier()
    {
        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void FlipSprite()
    {
        if (spriteRenderer == null || Mathf.Approximately(horizontalInput, 0f))
            return;

        spriteRenderer.flipX = horizontalInput < 0f;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // Core parameters
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsRunning", isRunning && isGrounded);
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) * (isRunning ? runSpeed : moveSpeed));
        animator.SetBool("IsJumping", !isGrounded);
    }

    private void StartCartwheel()
    {
        if (spriteRenderer == null)
            return;

        if (cartwheelCoroutine != null)
            StopCoroutine(cartwheelCoroutine);

        cartwheelCoroutine = StartCoroutine(CartwheelRoutine());
    }

    private System.Collections.IEnumerator CartwheelRoutine()
    {
        float timer = 0f;
        float startY = spriteRenderer.transform.localEulerAngles.y;

        while (timer < cartwheelDuration)
        {
            timer += Time.deltaTime;
            float spin = Mathf.Lerp(0f, 360f, timer / cartwheelDuration);
            spriteRenderer.transform.localEulerAngles = new Vector3(0f, startY + spin, 0f);
            yield return null;
        }

        spriteRenderer.transform.localEulerAngles = new Vector3(0f, startY, 0f);
        cartwheelCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}