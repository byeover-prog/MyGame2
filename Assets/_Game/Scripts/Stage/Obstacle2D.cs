using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public sealed class Obstacle2D : MonoBehaviour
{
    [SerializeField] private bool applyLayerToChildren = true;
    [SerializeField] private bool forceSolidColliders = true;
    [SerializeField] private bool autoCreateTilemapCollider = true;
    [SerializeField] private bool autoCreateSpriteBoundsCollider = true;
    [SerializeField] private bool disableMeshColliders = false;
    [SerializeField] private BoxCollider2D spriteBoundsCollider;

    private void Reset()
    {
        Apply();
    }

    private void Awake()
    {
        GameplayCollisionPolicy2D.Apply();
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            Apply();
    }
#endif

    public void Apply()
    {
        int obstacleLayer = GameplayCollisionLayers2D.ObstacleLayer;
        if (obstacleLayer >= 0)
            SetLayer(gameObject, obstacleLayer);

        EnsureTilemapCollider();
        EnsureSpriteBoundsColliderFallback();
        Disable3DMeshCollidersIfRequested();

        if (forceSolidColliders)
        {
            Collider2D[] colliders = applyLayerToChildren
                ? GetComponentsInChildren<Collider2D>(true)
                : GetComponents<Collider2D>();

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                colliders[i].isTrigger = false;
            }
        }
    }

    private void EnsureTilemapCollider()
    {
        if (!autoCreateTilemapCollider)
            return;

        Tilemap tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
            return;

        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
            tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();

        tilemapCollider.isTrigger = false;
    }

    private void EnsureSpriteBoundsColliderFallback()
    {
        if (!autoCreateSpriteBoundsCollider || HasUsableSolidCollider2D())
            return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        if (spriteBoundsCollider == null)
            spriteBoundsCollider = GetComponent<BoxCollider2D>();

        if (spriteBoundsCollider == null)
            spriteBoundsCollider = gameObject.AddComponent<BoxCollider2D>();

        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        spriteBoundsCollider.offset = spriteBounds.center;
        spriteBoundsCollider.size = spriteBounds.size;
        spriteBoundsCollider.isTrigger = false;
    }

    private bool HasUsableSolidCollider2D()
    {
        Collider2D[] colliders = applyLayerToChildren
            ? GetComponentsInChildren<Collider2D>(true)
            : GetComponents<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
                continue;

            if (collider.shapeCount > 0)
                return true;
        }

        return false;
    }

    private void Disable3DMeshCollidersIfRequested()
    {
        if (!disableMeshColliders)
            return;

        MeshCollider[] meshColliders = applyLayerToChildren
            ? GetComponentsInChildren<MeshCollider>(true)
            : GetComponents<MeshCollider>();

        for (int i = 0; i < meshColliders.Length; i++)
        {
            if (meshColliders[i] == null) continue;
            meshColliders[i].enabled = false;
        }
    }

    private void SetLayer(GameObject target, int layer)
    {
        target.layer = layer;

        if (!applyLayerToChildren)
            return;

        for (int i = 0; i < target.transform.childCount; i++)
            SetLayer(target.transform.GetChild(i).gameObject, layer);
    }
}
