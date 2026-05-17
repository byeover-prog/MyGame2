using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Obstacle2D : MonoBehaviour
{
    [SerializeField] private bool applyLayerToChildren = true;
    [SerializeField] private bool forceSolidColliders = true;

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

    private void SetLayer(GameObject target, int layer)
    {
        target.layer = layer;

        if (!applyLayerToChildren)
            return;

        for (int i = 0; i < target.transform.childCount; i++)
            SetLayer(target.transform.GetChild(i).gameObject, layer);
    }
}
