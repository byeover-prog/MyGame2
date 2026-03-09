// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberSpawner2D : MonoBehaviour
{
    [Header("프리팹(DamageNumberView2D 포함)")]
    [SerializeField] private DamageNumberView2D prefab;

    [Header("월드 루트(없으면 자동 생성)")]
    [SerializeField] private Transform root;

    private void Awake()
    {
        if (root == null)
        {
            var go = GameObject.Find("DamageNumbersRoot");
            if (go == null) go = new GameObject("DamageNumbersRoot");
            root = go.transform;
        }
    }

    public void Spawn(Vector2 worldPos, int damage)
    {
        if (prefab == null) return;

        var v = Instantiate(prefab, worldPos, Quaternion.identity, root);
        v.Play(damage);
    }
}