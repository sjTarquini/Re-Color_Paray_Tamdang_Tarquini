using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Handles cursor interactions and dragging for Role2
public class MoveCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private LayerMask interactLayer;

    [Header("Custom Cursor Visuals")]
    [Tooltip("The UI Image's RectTransform that visually represents the cursor. Should be a direct child of cursorCanvas.")]
    [SerializeField] private RectTransform cursorRect;
    [Tooltip("The Image component on the same object as cursorRect.")]
    [SerializeField] private Image cursorImage;
    [Tooltip("The Canvas the cursor icon lives under (needed to convert mouse screen position into UI space).")]
    [SerializeField] private Canvas cursorCanvas;
    [SerializeField] private Sprite idleCursorSprite;
    [SerializeField] private Sprite clickHoldCursorSprite;

    [Header("Drag Object References")]
    [SerializeField] private GameObject draggedObject;
    private bool isDraggingObject;

    private bool customCursorActive;

    private void Start()
    {
        // Hidden until we confirm the local player actually owns Role2.
        SetCustomCursorActive(false);
    }

    private void Update()
    {
        if (!PlayerManager.Instance.IsAlive)
        {
            SetCustomCursorActive(false);
            return;
        }

        int role = 0;
        if (MLevelSelectionManager.Instance != null)
            role = MLevelSelectionManager.Instance.GetLocalSelectedRoleIndexPublic();

        if (role != 2)
        {
            SetCustomCursorActive(false);

            // clear any drag state if role changed
            if (isDraggingObject && draggedObject != null)
            {
                var rb = draggedObject.GetComponent<RedBlockMoveable>();
                if (rb != null) rb.isDragged = false;
                draggedObject = null;
                isDraggingObject = false;
            }
            return;
        }

        SetCustomCursorActive(true);

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);

        UpdateCursorVisualPosition(screenPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            MouseClick(mousePos2D);
        }

        if (isDraggingObject && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            MouseLetGo();
        }

        UpdateCursorSprite();
    }

    private void SetCustomCursorActive(bool active)
    {
        if (customCursorActive == active) return;
        customCursorActive = active;

        // Hide the real OS cursor while our custom one is shown, restore it otherwise.
        Cursor.visible = !active;

        if (cursorRect != null)
            cursorRect.gameObject.SetActive(active);
    }

    private void UpdateCursorVisualPosition(Vector2 screenPos)
    {
        if (cursorRect == null || cursorCanvas == null) return;

        RectTransform canvasRect = cursorCanvas.transform as RectTransform;
        Camera uiCamera = cursorCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cursorCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPoint))
        {
            cursorRect.anchoredPosition = localPoint;
        }
    }

    private void UpdateCursorSprite()
    {
        if (cursorImage == null) return;
        cursorImage.sprite = isDraggingObject ? clickHoldCursorSprite : idleCursorSprite;
    }

    private void MouseClick(Vector2 mousePos2D)
    {
        RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero, Mathf.Infinity, interactLayer);
        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;
            if (clickedObject.CompareTag("RedMoveable") && clickedObject.GetComponent<RedBlockMoveable>() != null)
            {
                draggedObject = clickedObject;
                RedBlockMoveable red = draggedObject.GetComponent<RedBlockMoveable>();
                isDraggingObject = true;
                red.isDragged = true;
                red.SetOffset();
            }
        }
    }

    private void MouseLetGo()
    {
        if (draggedObject == null) return;
        RedBlockMoveable red = draggedObject.GetComponent<RedBlockMoveable>();
        if (red != null) red.isDragged = false;
        draggedObject = null;
        isDraggingObject = false;
    }

    private void OnDisable()
    {
        // Safety net: always give the OS cursor back if this component gets disabled mid-game.
        Cursor.visible = true;
    }
}