using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerCollisionBody2D : MonoBehaviour
{
    [Header("Body")]
    [SerializeField] private string bodyObjectName = "PlayerBody";
    [SerializeField] private BoxCollider2D sourceTrigger;
    [SerializeField] private BoxCollider2D bodyCollider;

    [Header("Shape")]
    [SerializeField] private bool syncFromSource = true;
    [SerializeField, Range(0.1f, 1f)] private float sourceSizeScale = 0.72f;
    [SerializeField] private Vector2 bodyOffset = new Vector2(0f, -0.08f);
    [SerializeField] private Vector2 fallbackBodySize = new Vector2(0.48f, 0.46f);

    private void Reset()
    {
        sourceTrigger = GetComponent<BoxCollider2D>();
    }

    private void Awake()
    {
        GameplayCollisionPolicy2D.Apply();
        EnsureBody();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (sourceTrigger == null)
                sourceTrigger = GetComponent<BoxCollider2D>();
        }
    }
#endif

    public void EnsureBody()
    {
        if (sourceTrigger == null)
            sourceTrigger = GetComponent<BoxCollider2D>();

        if (sourceTrigger != null)
            sourceTrigger.isTrigger = true;

        int playerLayer = GameplayCollisionLayers2D.PlayerLayer;
        int bodyLayer = GameplayCollisionLayers2D.PlayerBodyLayer;
        if (playerLayer >= 0)
            gameObject.layer = playerLayer;

        if (bodyCollider == null)
            bodyCollider = FindBodyCollider();

        if (bodyCollider == null)
            bodyCollider = CreateBodyCollider();

        if (bodyCollider == null)
            return;

        bodyCollider.gameObject.layer = bodyLayer >= 0 ? bodyLayer : gameObject.layer;
        bodyCollider.isTrigger = false;

        if (syncFromSource && sourceTrigger != null)
        {
            bodyCollider.offset = sourceTrigger.offset + bodyOffset;
            bodyCollider.size = new Vector2(
                Mathf.Max(0.01f, sourceTrigger.size.x * sourceSizeScale),
                Mathf.Max(0.01f, sourceTrigger.size.y * sourceSizeScale));
        }
        else
        {
            bodyCollider.offset = bodyOffset;
            bodyCollider.size = fallbackBodySize;
        }
    }

    private BoxCollider2D FindBodyCollider()
    {
        Transform body = transform.Find(bodyObjectName);
        return body != null ? body.GetComponent<BoxCollider2D>() : null;
    }

    private BoxCollider2D CreateBodyCollider()
    {
        GameObject body = new GameObject(bodyObjectName);
        body.transform.SetParent(transform, false);
        return body.AddComponent<BoxCollider2D>();
    }
}
