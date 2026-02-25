// UTF-8
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HitFlash2D : MonoBehaviour
{
    [Header("대상 렌더러(없으면 자식에서 자동 탐색)")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("반짝임 설정")]
    [Tooltip("피격 시 바꿀 색(보통 흰색이 무난)")]
    [SerializeField] private Color flashColor = Color.white;

    [Tooltip("반짝임 유지 시간(초)")]
    [SerializeField] private float flashSeconds = 0.08f;

    [Tooltip("반짝임 반복 횟수(2면 깜빡 2번)")]
    [SerializeField] private int blinkCount = 2;

    private Color _original;
    private Coroutine _co;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (targetRenderer != null) _original = targetRenderer.color;
    }

    public void Play()
    {
        if (targetRenderer == null) return;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlash());
    }

    private IEnumerator CoFlash()
    {
        // 구현 원리: 색을 잠깐 바꿨다가 원복(반복)
        for (int i = 0; i < Mathf.Max(1, blinkCount); i++)
        {
            targetRenderer.color = flashColor;
            yield return new WaitForSeconds(flashSeconds);

            targetRenderer.color = _original;
            yield return new WaitForSeconds(flashSeconds);
        }

        _co = null;
    }
}