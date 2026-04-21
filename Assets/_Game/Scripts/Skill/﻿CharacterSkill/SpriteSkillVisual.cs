using System.Collections;
using UnityEngine;

/// <summary>
/// 스프라이트 기반 임시 비주얼입니다.
/// 외주 VFX가 오면 같은 인터페이스 구현체로 교체하면 됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SpriteSkillVisual : MonoBehaviour, ISkillVisual
{
    [Header("참조")]
    [Tooltip("실제 스프라이트 루트입니다.")]
    [SerializeField] private Transform spriteRoot;

    [Tooltip("실제 SpriteRenderer입니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("크기")]
    [Tooltip("localScale 1일 때 시각적 반경입니다.")]
    [SerializeField] private float baseRadius = 1f;

    [Header("임팩트")]
    [Tooltip("임팩트 순간 확대 배율입니다.")]
    [SerializeField] private float impactScalePunch = 1.25f;

    [Tooltip("임팩트 유지 시간입니다.")]
    [SerializeField] private float impactDuration = 0.12f;

    [Tooltip("Stop 호출 시 스프라이트를 숨깁니다.")]
    [SerializeField] private bool hideRendererOnStop = true;

    private float _currentScale = 1f;
    private Coroutine _impactRoutine;

    public float BaseRadius => Mathf.Max(0.01f, baseRadius);

    private void Awake()
    {
        if (spriteRoot == null)
            spriteRoot = transform;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        ApplyScale();
        Stop();
    }

    public void Play(Vector3 pos)
    {
        transform.position = pos;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (spriteRoot != null && !spriteRoot.gameObject.activeSelf)
            spriteRoot.gameObject.SetActive(true);

        ApplyScale();
    }

    public void UpdatePosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public void UpdateScale(float scaleMul)
    {
        _currentScale = Mathf.Max(0.01f, scaleMul);
        ApplyScale();
    }

    public void PlayImpact(Vector3 pos)
    {
        Play(pos);

        if (_impactRoutine != null)
            StopCoroutine(_impactRoutine);

        _impactRoutine = StartCoroutine(ImpactRoutine());
    }

    public void Stop()
    {
        if (_impactRoutine != null)
        {
            StopCoroutine(_impactRoutine);
            _impactRoutine = null;
        }

        if (hideRendererOnStop && spriteRenderer != null)
            spriteRenderer.enabled = false;

        ApplyScale();
    }

    private void ApplyScale()
    {
        if (spriteRoot == null) return;
        spriteRoot.localScale = Vector3.one * _currentScale;
    }

    private IEnumerator ImpactRoutine()
    {
        float baseScale = _currentScale;
        float impactScale = Mathf.Max(0.01f, baseScale * impactScalePunch);

        if (spriteRoot != null)
            spriteRoot.localScale = Vector3.one * impactScale;

        yield return new WaitForSeconds(impactDuration);

        if (spriteRoot != null)
            spriteRoot.localScale = Vector3.one * baseScale;

        _impactRoutine = null;
    }
}
