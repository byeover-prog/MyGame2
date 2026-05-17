using UnityEngine;

/// <summary>
/// Player Rigidbody2D movement output.
/// Input, dash decisions, and animation remain outside this component.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovementMotor2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Boundary")]
    [SerializeField] private Collider2D mapBoundsCollider;
    [SerializeField] private Rect manualBounds = new Rect(-20, -20, 40, 40);
    [Min(0f)]
    [SerializeField] private float boundaryMargin = 0.5f;
    [SerializeField] private bool useBoundary = true;

    [Header("Obstacle Collision")]
    [SerializeField] private bool blockObstacles = true;
    [SerializeField, Min(0f)] private float skinWidth = 0.02f;

    private const int MaxObstacleHits = 8;

    private float _boundsMinX;
    private float _boundsMaxX;
    private float _boundsMinY;
    private float _boundsMaxY;
    private bool _boundsReady;
    private readonly RaycastHit2D[] _obstacleHits = new RaycastHit2D[MaxObstacleHits];
    private ContactFilter2D _obstacleFilter;
    private bool _obstacleFilterReady;
    private PlayerCollisionBody2D _collisionBody;
    private Vector2 _lastRequestedVelocity;

    public Rigidbody2D Rigidbody => rb;
    public Vector2 Velocity => rb != null ? _lastRequestedVelocity : Vector2.zero;
    public float VelocityMagnitude => Velocity.magnitude;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        EnsureCollisionBody();
        RebuildObstacleFilter();
        RebuildBounds();
    }

    public void Configure(
        Rigidbody2D sourceRb,
        Collider2D boundsCollider,
        Rect fallbackBounds,
        float margin,
        bool enableBoundary)
    {
        if (sourceRb != null)
            rb = sourceRb;
        else if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        mapBoundsCollider = boundsCollider;
        manualBounds = fallbackBounds;
        boundaryMargin = Mathf.Max(0f, margin);
        useBoundary = enableBoundary;

        EnsureCollisionBody();
        RebuildObstacleFilter();
        RebuildBounds();
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (rb == null) return;

        _lastRequestedVelocity = velocity;

        if (blockObstacles && Time.inFixedTimeStep && TryMoveWithObstacleCast(velocity * Time.fixedDeltaTime))
        {
            ClampToBoundary();
            return;
        }

        rb.linearVelocity = velocity;
        ClampToBoundary();
    }

    public void Stop()
    {
        _lastRequestedVelocity = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        ClampToBoundary();
    }

    public void RebuildBounds()
    {
        _boundsReady = false;
        if (!useBoundary) return;

        if (mapBoundsCollider == null)
        {
            GameObject found = GameObject.Find("MapBounds2D");
            if (found != null)
                mapBoundsCollider = found.GetComponent<Collider2D>();
        }

        if (mapBoundsCollider != null)
        {
            Bounds b = mapBoundsCollider.bounds;
            ApplyBounds(b.min.x, b.max.x, b.min.y, b.max.y);
        }
        else
        {
            ApplyBounds(manualBounds.xMin, manualBounds.xMax, manualBounds.yMin, manualBounds.yMax);
        }
    }

    private void ApplyBounds(float minX, float maxX, float minY, float maxY)
    {
        float m = boundaryMargin;
        _boundsMinX = minX + m;
        _boundsMaxX = maxX - m;
        _boundsMinY = minY + m;
        _boundsMaxY = maxY - m;

        if (_boundsMinX > _boundsMaxX) (_boundsMinX, _boundsMaxX) = (_boundsMaxX, _boundsMinX);
        if (_boundsMinY > _boundsMaxY) (_boundsMinY, _boundsMaxY) = (_boundsMaxY, _boundsMinY);

        _boundsReady = true;
    }

    private void EnsureCollisionBody()
    {
        if (rb == null)
            return;

        Transform bodyRoot = rb.transform;
        if (_collisionBody == null)
            _collisionBody = bodyRoot.GetComponent<PlayerCollisionBody2D>();

        if (_collisionBody == null)
            _collisionBody = bodyRoot.GetComponentInChildren<PlayerCollisionBody2D>(true);

        if (_collisionBody == null)
            _collisionBody = bodyRoot.gameObject.AddComponent<PlayerCollisionBody2D>();

        _collisionBody.EnsureBody();
    }

    private void RebuildObstacleFilter()
    {
        int obstacleLayer = GameplayCollisionLayers2D.ObstacleLayer;
        _obstacleFilterReady = obstacleLayer >= 0;
        if (!_obstacleFilterReady)
            return;

        _obstacleFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = 1 << obstacleLayer,
            useTriggers = false
        };
    }

    private bool TryMoveWithObstacleCast(Vector2 delta)
    {
        if (!_obstacleFilterReady)
            return false;

        rb.linearVelocity = Vector2.zero;

        float distance = delta.magnitude;
        if (distance <= 0.0001f)
            return true;

        EnsureCollisionBody();

        Vector2 direction = delta / distance;
        float castDistance = distance + skinWidth;
        int hitCount = rb.Cast(direction, _obstacleFilter, _obstacleHits, castDistance);
        float allowedDistance = distance;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _obstacleHits[i];
            if (hit.collider == null || hit.collider.isTrigger)
                continue;

            float hitDistance = Mathf.Max(0f, hit.distance - skinWidth);
            if (hitDistance < allowedDistance)
                allowedDistance = hitDistance;
        }

        rb.position += direction * allowedDistance;
        return true;
    }

    private void ClampToBoundary()
    {
        if (!useBoundary || !_boundsReady || rb == null)
            return;

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, _boundsMinX, _boundsMaxX);
        pos.y = Mathf.Clamp(pos.y, _boundsMinY, _boundsMaxY);
        rb.position = pos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useBoundary) return;

        float minX;
        float maxX;
        float minY;
        float maxY;

        if (Application.isPlaying && _boundsReady)
        {
            minX = _boundsMinX;
            maxX = _boundsMaxX;
            minY = _boundsMinY;
            maxY = _boundsMaxY;
        }
        else if (mapBoundsCollider != null)
        {
            Bounds b = mapBoundsCollider.bounds;
            float m = boundaryMargin;
            minX = b.min.x + m;
            maxX = b.max.x - m;
            minY = b.min.y + m;
            maxY = b.max.y - m;
        }
        else
        {
            float m = boundaryMargin;
            minX = manualBounds.xMin + m;
            maxX = manualBounds.xMax - m;
            minY = manualBounds.yMin + m;
            maxY = manualBounds.yMax - m;
        }

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
