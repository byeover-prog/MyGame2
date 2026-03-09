using System.Collections;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// 피격 시 SpriteRenderer.color를 흰색으로 바꿔서 깜빡이게 합니다.
/// 머티리얼 교체(MaterialSwap)는 텍스처 참조 문제로 검은색 노이즈가 생길 수 있어,
/// color 틴트 방식만 사용합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class HitFlash2D : MonoBehaviour
{
    [Header("대상(비워두면 자식 SpriteRenderer 자동 수집)")]
    [Tooltip("플레이어가 여러 SpriteRenderer로 구성되어 있으면 비워두는 것을 권장합니다(자동 수집).")]
    [SerializeField] private SpriteRenderer[] targetRenderers;

    [Header("색/시간")]
    [Tooltip("피격 시 스프라이트에 입힐 색상입니다. 흰색이면 스프라이트가 하얗게 번쩍입니다.")]
    [SerializeField] private Color flashColor = Color.white;

    [Tooltip("한 번 깜빡이는 유지 시간(초)입니다.")]
    [SerializeField] private float flashSeconds = 0.06f;

    [Tooltip("깜빡임 반복 횟수입니다.")]
    [SerializeField] private int blinkCount = 2;

    [Tooltip("Time.timeScale=0에서도 깜빡이게 할지 여부입니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    private Color[] _originalColors;
    private bool _originalsCached;
    private Coroutine _co;

    private void Awake()
    {
        GatherTargets();
        CacheOriginals();
    }

    private void OnDisable()
    {
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
        RestoreOriginals();
    }

    /// <summary>
    /// 피격 플래시를 재생합니다. 연속 호출해도 안전합니다.
    /// </summary>
    [ContextMenu("테스트: 플래시 재생")]
    public void Play()
    {
        GatherTargets();
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        CacheOriginals();

        if (_co != null)
        {
            StopCoroutine(_co);
            RestoreOriginals();
        }

        _co = StartCoroutine(CoFlash());
    }

    private void GatherTargets()
    {
        if (targetRenderers != null && targetRenderers.Length > 0) return;
        targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void CacheOriginals()
    {
        if (_originalsCached) return;
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        int n = targetRenderers.Length;
        _originalColors = new Color[n];

        for (int i = 0; i < n; i++)
        {
            if (targetRenderers[i] != null)
                _originalColors[i] = targetRenderers[i].color;
        }

        _originalsCached = true;
    }

    private void ApplyFlash()
    {
        if (targetRenderers == null) return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].color = flashColor;
        }
    }

    private void RestoreOriginals()
    {
        if (targetRenderers == null || _originalColors == null) return;
        int n = Mathf.Min(targetRenderers.Length, _originalColors.Length);

        for (int i = 0; i < n; i++)
        {
            if (targetRenderers[i] != null)
                targetRenderers[i].color = _originalColors[i];
        }
    }

    private IEnumerator CoFlash()
    {
        int count = Mathf.Max(1, blinkCount);

        for (int i = 0; i < count; i++)
        {
            ApplyFlash();
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(flashSeconds);
            else yield return new WaitForSeconds(flashSeconds);

            RestoreOriginals();
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(flashSeconds);
            else yield return new WaitForSeconds(flashSeconds);
        }

        _co = null;
    }
}