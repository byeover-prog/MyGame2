// UTF-8
// [구현 원리 요약]
// - 외부 VFX 프리팹이 없을 때도 현재 스프라이트를 복제해서 잔상처럼 보이게 만든다.
// - 인스턴스는 정적 풀로 재사용해서 GC를 줄인다.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스프라이트 잔상 VFX.
/// 외부 파티클 VFX 없이도 투사체에 잔상 효과를 부여한다.
/// 정적 풀로 관리되어 GC 부담이 없다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpriteGhostVfx2D : MonoBehaviour
{
    private static readonly Stack<SpriteGhostVfx2D> _pool = new Stack<SpriteGhostVfx2D>(128);
    private static Transform _poolRoot;

    [Header("렌더러")]
    [Tooltip("잔상용 스프라이트 렌더러 (자동 생성)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float _age;
    private float _life;
    private Color _startColor;
    private Color _endColor;
    private Vector3 _startScale;
    private Vector3 _endScale;

    /// <summary>잔상 생성 (정적 풀에서 꺼내서 재생)</summary>
    public static void Spawn(SpriteRenderer source, float life, float startAlpha, float endAlpha,
        float startScaleMul, float endScaleMul, int sortingOffset)
    {
        if (source == null || source.sprite == null) return;

        var inst = GetOrCreate();
        inst.Play(source, life, startAlpha, endAlpha, startScaleMul, endScaleMul, sortingOffset);
    }

    private static SpriteGhostVfx2D GetOrCreate()
    {
        while (_pool.Count > 0)
        {
            var item = _pool.Pop();
            if (item != null)
                return item;
        }

        var go = new GameObject("SpriteGhostVfx2D");
        var inst = go.AddComponent<SpriteGhostVfx2D>();
        inst.spriteRenderer = go.AddComponent<SpriteRenderer>();
        return inst;
    }

    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[SpriteGhostVfxPool]");
                Object.DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }
            return _poolRoot;
        }
    }

    private void Play(SpriteRenderer source, float life, float startAlpha, float endAlpha,
        float startScaleMul, float endScaleMul, int sortingOffset)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        transform.SetParent(null, false);
        transform.position = source.transform.position;
        transform.rotation = source.transform.rotation;
        transform.localScale = source.transform.lossyScale * Mathf.Max(0.01f, startScaleMul);

        spriteRenderer.sprite = source.sprite;
        spriteRenderer.sharedMaterial = source.sharedMaterial;
        spriteRenderer.sortingLayerID = source.sortingLayerID;
        spriteRenderer.sortingOrder = source.sortingOrder + sortingOffset;
        spriteRenderer.flipX = source.flipX;
        spriteRenderer.flipY = source.flipY;
        spriteRenderer.drawMode = source.drawMode;
        spriteRenderer.size = source.size;
        spriteRenderer.maskInteraction = source.maskInteraction;

        Color color = source.color;
        color.a = Mathf.Clamp01(startAlpha);
        spriteRenderer.color = color;
        spriteRenderer.enabled = true;

        _age = 0f;
        _life = Mathf.Max(0.01f, life);
        _startColor = color;
        _endColor = new Color(color.r, color.g, color.b, Mathf.Clamp01(endAlpha));
        _startScale = transform.localScale;
        _endScale = source.transform.lossyScale * Mathf.Max(0.01f, endScaleMul);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / _life);

        transform.localScale = Vector3.LerpUnclamped(_startScale, _endScale, t);
        spriteRenderer.color = Color.LerpUnclamped(_startColor, _endColor, t);

        if (_age >= _life)
            ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        gameObject.SetActive(false);
        transform.SetParent(PoolRoot, false);
        _pool.Push(this);
    }
}
