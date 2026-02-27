// UTF-8
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class HitFlash2D : MonoBehaviour
{
    public enum FlashMode
    {
        TintColor,        // SpriteRenderer.color만 변경
        MaterialSwap,     // 머티리얼만 교체(라이트 없을 때 가장 확실)
        TintAndMaterial,  // 둘 다
    }

    [Header("대상(비워두면 자식 SpriteRenderer 자동 수집)")]
    [SerializeField, InspectorName("대상 렌더러들")]
    [Tooltip("플레이어가 여러 SpriteRenderer로 구성되어 있으면 비워두는 것을 권장합니다(자동 수집).")]
    private SpriteRenderer[] targetRenderers;

    [Header("반짝임 방식")]
    [SerializeField, InspectorName("플래시 모드")]
    [Tooltip("Sprite-Lit + 라이트 부족이면 MaterialSwap(또는 TintAndMaterial)을 권장합니다.")]
    private FlashMode flashMode = FlashMode.MaterialSwap;

    [SerializeField, InspectorName("플래시 머티리얼")]
    [Tooltip("라이트 영향을 받지 않는(Unlit) 머티리얼을 넣으면 플래시가 확실하게 보입니다.\n예: M_Flash_UnlitWhite(Shader: URP/2D/Sprite-Unlit-Default)")]
    private Material flashMaterial;

    [Header("색/시간")]
    [SerializeField, InspectorName("플래시 색")]
    [Tooltip("TintColor 모드에서 사용됩니다.")]
    private Color flashColor = Color.white;

    [SerializeField, InspectorName("유지 시간(초)")]
    private float flashSeconds = 0.08f;

    [SerializeField, InspectorName("깜빡임 횟수")]
    [Tooltip("2면 2번 깜빡입니다.")]
    private int blinkCount = 2;

    [SerializeField, InspectorName("시간스케일 무시")]
    [Tooltip("피격 순간 Time.timeScale을 낮추는(히트스탑) 구현이 있으면 체크 권장")]
    private bool useUnscaledTime = true;

    private Color[] _originalColors;
    private Material[] _originalMaterials;
    private Coroutine _co;

    private void Awake()
    {
        CacheTargetsIfNeeded();
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

    [ContextMenu("테스트: 플래시 재생")]
    public void Play()
    {
        CacheTargetsIfNeeded();
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        CacheOriginals();

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoFlash());
    }

    private void CacheTargetsIfNeeded()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void CacheOriginals()
    {
        if (targetRenderers == null) return;

        int n = targetRenderers.Length;

        if (_originalColors == null || _originalColors.Length != n)
            _originalColors = new Color[n];

        if (_originalMaterials == null || _originalMaterials.Length != n)
            _originalMaterials = new Material[n];

        for (int i = 0; i < n; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            _originalColors[i] = r.color;
            _originalMaterials[i] = r.sharedMaterial;
        }
    }

    private void ApplyFlash()
    {
        int n = targetRenderers.Length;

        for (int i = 0; i < n; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            if (flashMode == FlashMode.TintColor || flashMode == FlashMode.TintAndMaterial)
                r.color = flashColor;

            if ((flashMode == FlashMode.MaterialSwap || flashMode == FlashMode.TintAndMaterial) && flashMaterial != null)
                r.sharedMaterial = flashMaterial;
        }
    }

    private void RestoreOriginals()
    {
        if (targetRenderers == null || _originalColors == null || _originalMaterials == null) return;

        int n = Mathf.Min(targetRenderers.Length, _originalColors.Length);

        for (int i = 0; i < n; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            r.color = _originalColors[i];
            r.sharedMaterial = _originalMaterials[i];
        }
    }

    private IEnumerator CoFlash()
    {
        // 구현 원리: (색 변경/머티리얼 교체) -> 잠깐 유지 -> 원복을 반복
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