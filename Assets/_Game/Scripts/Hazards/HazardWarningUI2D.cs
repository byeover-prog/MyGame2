// UTF-8
using System.Collections;
using UnityEngine;

/// <summary>
/// 화면 가장자리에 느낌표 경고 UI를 잠깐 표시하는 전용 UI 컴포넌트입니다.
///
/// 구현 원리:
/// 1. 방해 요소가 생성될 월드 위치를 카메라의 뷰포트 좌표로 변환합니다.
/// 2. 그 좌표가 가리키는 화면 방향(왼쪽/오른쪽/위/아래)에 맞춰 UI를 화면 가장자리로 배치합니다.
/// 3. CanvasGroup의 alpha와 scale을 이용해 잠깐 나타났다가 사라지는 경고 연출을 재생합니다.
/// 4. 연출이 끝나면 자기 자신을 자동으로 제거해, 매니저가 따로 정리하지 않아도 되게 합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class HazardWarningUI2D : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("이 경고 UI 자신의 RectTransform입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private RectTransform rectTransform;

    [Tooltip("알파 페이드용 CanvasGroup입니다. 비워두면 자동으로 찾아서 연결합니다.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("애니메이션 설정")]
    [Tooltip("전체 경고 시간 중 페이드 인이 차지하는 비율입니다.")]
    [SerializeField][Min(0f)] private float fadeInRatio = 0.2f;

    [Tooltip("전체 경고 시간 중 유지 구간이 차지하는 비율입니다.")]
    [SerializeField][Min(0f)] private float holdRatio = 0.45f;

    [Tooltip("전체 경고 시간 중 페이드 아웃이 차지하는 비율입니다.")]
    [SerializeField][Min(0f)] private float fadeOutRatio = 0.35f;

    [Tooltip("경고가 살짝 커졌다 작아지는 느낌을 줄 때 사용하는 스케일 진폭입니다.")]
    [SerializeField][Min(0f)] private float pulseScaleAmplitude = 0.08f;

    private Vector3 _baseScale = Vector3.one;
    private Coroutine _playRoutine;

    private void Reset()
    {
        CacheReferences();
    }

    private void Awake()
    {
        CacheReferences();
        _baseScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (rectTransform != null)
            rectTransform.localScale = _baseScale;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnValidate()
    {
        if (fadeInRatio < 0f)
            fadeInRatio = 0f;

        if (holdRatio < 0f)
            holdRatio = 0f;

        if (fadeOutRatio < 0f)
            fadeOutRatio = 0f;

        if (pulseScaleAmplitude < 0f)
            pulseScaleAmplitude = 0f;
    }

    /// <summary>
    /// 경고 UI를 스폰 방향에 맞게 배치하고, 지정한 시간 동안 재생합니다.
    /// </summary>
    public void Play(Camera gameplayCamera, Vector3 hazardWorldPosition, float duration, Vector2 edgePadding)
    {
        CacheReferences();

        if (gameplayCamera == null)
        {
            Debug.LogWarning("[HazardWarningUI2D] Gameplay Camera가 비어 있습니다.", this);
            Destroy(gameObject);
            return;
        }

        if (duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        PlaceAtScreenEdge(gameplayCamera, hazardWorldPosition, edgePadding);

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(CoPlay(duration));
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void PlaceAtScreenEdge(Camera gameplayCamera, Vector3 worldPosition, Vector2 edgePadding)
    {
        Vector3 viewportPoint = gameplayCamera.WorldToViewportPoint(worldPosition);

        Vector2 edgeAnchor = ResolveEdgeAnchor(viewportPoint);

        rectTransform.anchorMin = edgeAnchor;
        rectTransform.anchorMax = edgeAnchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = ResolveEdgeOffset(edgeAnchor, edgePadding);
    }

    private Vector2 ResolveEdgeAnchor(Vector3 viewportPoint)
    {
        float clampedX = Mathf.Clamp01(viewportPoint.x);
        float clampedY = Mathf.Clamp01(viewportPoint.y);

        if (viewportPoint.x < 0f)
            return new Vector2(0f, clampedY);

        if (viewportPoint.x > 1f)
            return new Vector2(1f, clampedY);

        if (viewportPoint.y < 0f)
            return new Vector2(clampedX, 0f);

        if (viewportPoint.y > 1f)
            return new Vector2(clampedX, 1f);

        // 이미 화면 안쪽이라면 가장 가까운 가장자리로 붙입니다.
        float leftDistance = clampedX;
        float rightDistance = 1f - clampedX;
        float bottomDistance = clampedY;
        float topDistance = 1f - clampedY;

        float minDistance = Mathf.Min(leftDistance, rightDistance, bottomDistance, topDistance);

        if (Mathf.Approximately(minDistance, leftDistance))
            return new Vector2(0f, clampedY);

        if (Mathf.Approximately(minDistance, rightDistance))
            return new Vector2(1f, clampedY);

        if (Mathf.Approximately(minDistance, bottomDistance))
            return new Vector2(clampedX, 0f);

        return new Vector2(clampedX, 1f);
    }

    private Vector2 ResolveEdgeOffset(Vector2 edgeAnchor, Vector2 edgePadding)
    {
        if (edgeAnchor.x <= 0.001f)
            return new Vector2(edgePadding.x, 0f);

        if (edgeAnchor.x >= 0.999f)
            return new Vector2(-edgePadding.x, 0f);

        if (edgeAnchor.y <= 0.001f)
            return new Vector2(0f, edgePadding.y);

        return new Vector2(0f, -edgePadding.y);
    }

    private IEnumerator CoPlay(float totalDuration)
    {
        float ratioSum = fadeInRatio + holdRatio + fadeOutRatio;
        if (ratioSum <= 0.0001f)
            ratioSum = 1f;

        float fadeInDuration = totalDuration * (fadeInRatio / ratioSum);
        float holdDuration = totalDuration * (holdRatio / ratioSum);
        float fadeOutDuration = totalDuration * (fadeOutRatio / ratioSum);

        rectTransform.localScale = _baseScale;
        canvasGroup.alpha = 0f;

        // Fade In
        if (fadeInDuration > 0f)
        {
            float timer = 0f;
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeInDuration);

                canvasGroup.alpha = t;
                rectTransform.localScale = _baseScale * (1f + pulseScaleAmplitude * Mathf.Sin(t * Mathf.PI));

                yield return null;
            }
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = _baseScale;

        // Hold
        if (holdDuration > 0f)
        {
            float timer = 0f;
            while (timer < holdDuration)
            {
                timer += Time.deltaTime;
                float normalized = Mathf.Clamp01(timer / holdDuration);

                float pulse = 1f + pulseScaleAmplitude * 0.5f * Mathf.Sin(normalized * Mathf.PI * 2f);
                rectTransform.localScale = _baseScale * pulse;

                yield return null;
            }
        }

        // Fade Out
        if (fadeOutDuration > 0f)
        {
            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeOutDuration);

                canvasGroup.alpha = 1f - t;
                rectTransform.localScale = _baseScale * (1f + pulseScaleAmplitude * (1f - t) * 0.5f);

                yield return null;
            }
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = _baseScale;

        Destroy(gameObject);
    }
}