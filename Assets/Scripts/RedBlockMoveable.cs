using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
public class RedBlockMoveable : MonoBehaviour
{
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private float collisionBuffer = 0.05f;
    [SerializeField] private float maxDragSpeed = 6f;
    [SerializeField] private GameObject[] wayPoints;
    [SerializeField] private PhotonView photonView;

    private Vector3 offset;
    private Rigidbody2D rb;
    private ContactFilter2D contactFilter;
    private Vector2 lastCastStart;
    private Vector2 lastCastDirection;
    private float lastCastDistance;
    private Vector2 lastCastHitPoint;
    private bool lastCastHit;
    public bool isDragged { get; set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (photonView == null)
            photonView = GetComponent<PhotonView>();

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(blockingLayers);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;
    }

    void FixedUpdate()
    {
        // Safety net: even though isDragged is only ever set true locally by whichever client
        // clicked the block (MoveCursor.MouseClick), also require ownership before actually
        // moving it. This covers the brief window right after a click where TransferOwnership
        // has been requested but PUN hasn't confirmed it back yet.
        if (photonView != null && !photonView.IsMine)
            return;

        if (isDragged)
        {
            DragObject();
        }
    }

    public void SetOffset()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = transform.position.z;
        offset = transform.position - mousePos;
    }

    void DragObject()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = transform.position.z;

        Vector2 targetPosition = new Vector2(mousePos.x + offset.x, mousePos.y + offset.y);
        
        // Constrain to waypoint path if waypoints exist
        if (wayPoints != null && wayPoints.Length > 1)
        {
            targetPosition = ConstrainToWaypointPath(targetPosition);
        }

        Vector2 currentPosition = rb.position;
        Vector2 direction = targetPosition - currentPosition;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
            return;

        direction /= distance;
        float maxDistanceThisFrame = maxDragSpeed * Time.fixedDeltaTime;
        if (distance > maxDistanceThisFrame)
            distance = maxDistanceThisFrame;

        targetPosition = currentPosition + direction * distance;

        lastCastStart = currentPosition;
        lastCastDirection = direction;
        lastCastDistance = distance;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        int hitCount = rb.Cast(direction, contactFilter, hits, distance);

        if (hitCount > 0)
        {
            lastCastHit = true;
            lastCastHitPoint = hits[0].point;
            float allowedDistance = Mathf.Max(0f, hits[0].distance - collisionBuffer);
            targetPosition = currentPosition + direction * allowedDistance;
        }
        else
        {
            lastCastHit = false;
        }

        rb.MovePosition(targetPosition);
    }

    private Vector2 ConstrainToWaypointPath(Vector2 targetPosition)
    {
        float closestDistance = float.MaxValue;
        Vector2 closestPoint = targetPosition;

        // Check all waypoint segments
        for (int i = 0; i < wayPoints.Length - 1; i++)
        {
            Vector2 waypointA = wayPoints[i].transform.position;
            Vector2 waypointB = wayPoints[i + 1].transform.position;

            // Project target onto segment
            Vector2 segmentPoint = ProjectPointOnSegment(targetPosition, waypointA, waypointB);
            float distance = Vector2.Distance(targetPosition, segmentPoint);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = segmentPoint;
            }
        }

        return closestPoint;
    }

    private Vector2 ProjectPointOnSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segmentDir = segmentEnd - segmentStart;
        float segmentLength = segmentDir.magnitude;

        if (segmentLength == 0f)
            return segmentStart;

        float t = Vector2.Dot(point - segmentStart, segmentDir) / (segmentLength * segmentLength);
        t = Mathf.Clamp01(t);

        return segmentStart + segmentDir * t;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isDragged)
            return;

        Gizmos.color = lastCastHit ? Color.red : Color.green;
        Gizmos.DrawRay(lastCastStart, lastCastDirection * lastCastDistance);

        if (lastCastHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastCastHitPoint, 0.1f);
        }
    }
}