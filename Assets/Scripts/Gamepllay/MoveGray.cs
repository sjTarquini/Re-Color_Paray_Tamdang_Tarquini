using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;

// Handles keyboard movement/jump for Role1 (Gray character)
public class MoveGray : MonoBehaviourPunCallbacks //, IPunObservable
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private SpriteRenderer spriteRenderer;
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

        if (groundCheckPoint == null)
            groundCheckPoint = transform;

        jumpCount = ActiveJumpCount;

        int role = MLevelSelectionManager.GetLocalRoleIndexFromPrefs();

        // Only the player who is actually assigned Role 1 should ever try to take ownership
        // of Grey. Requesting unconditionally (old behavior) caused whichever client's request
        // resolved last to "win" the object, regardless of role.
        if (role == 1 && !photonView.IsMine)
        {
            photonView.RequestOwnership();
        }

        ApplyOwnershipPhysicsState();

        Debug.Log($"[MoveGray] Start - Role: {role}, IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}, Owner: {photonView.Owner?.NickName}");
    }

    // Non-owners get a Static rigidbody so their local physics doesn't fight the
    // network-synced transform. This must be re-applied whenever ownership actually
    // changes (see OnOwnershipTransfered below) - not just once in Start(), since
    // RequestOwnership() resolves asynchronously.
    private void ApplyOwnershipPhysicsState()
    {
        if (rb == null)
            return;

        rb.bodyType = photonView.IsMine ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }

    void Update()
    {
        if (photonView == null)
        {
            Debug.LogError("[MoveGray] PhotonView is null!");
            return;
        }

        if (!photonView.IsMine)
        {
            return;
        }

        // if (!PlayerManager.Instance.IsAlive)
        //     return;

        int role = MLevelSelectionManager.GetLocalRoleIndexFromPrefs();
        if (role == 1)
            HandleInput();
        else
        {
            horizontalInput = 0f;
            isRunning = false;
            jumpRequested = false;
        }

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
            return;

        CheckGrounded();

        if (isGrounded && !wasGrounded)
            jumpCount = ActiveJumpCount;

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
        horizontalInput = (right ? 1f : 0f) - (left ? 1f : 0f);

        isRunning = Keyboard.current.shiftKey.isPressed;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpRequested = true;
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
        InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.jumpSound);

        if (isSecondJump)
        {
            StartCartwheel();
            InGameAudioManager.Instance.PlaySound(InGameAudioManager.Instance.jumpSound);
        }
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

    // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    // {
    //     if (stream.IsWriting)
    //     {
    //         // Send position, velocity, and animation state to other players
    //         stream.SendNext(transform.position);
    //         stream.SendNext(rb.velocity);
    //         stream.SendNext(isGrounded);
    //         stream.SendNext(horizontalInput);
    //         stream.SendNext(isRunning);
    //     }
    //     else
    //     {
    //         // Receive position, velocity, and animation state from network
    //         Vector3 networkPosition = (Vector3)stream.ReceiveNext();
    //         Vector2 networkVelocity = (Vector2)stream.ReceiveNext();
    //         bool networkIsGrounded = (bool)stream.ReceiveNext();
    //         float networkHorizontalInput = (float)stream.ReceiveNext();
    //         bool networkIsRunning = (bool)stream.ReceiveNext();

    //         // Update remote player
    //         if (!photonView.IsMine)
    //         {
    //             transform.position = networkPosition;
    //             rb.velocity = networkVelocity;
    //             isGrounded = networkIsGrounded;
    //             horizontalInput = networkHorizontalInput;
    //             isRunning = networkIsRunning;
    //         }
    //     }
    // }
}