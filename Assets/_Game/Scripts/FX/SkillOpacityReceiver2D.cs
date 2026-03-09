// UTF-8
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillOpacityReceiver2D : MonoBehaviour
{
    [Header("대상(비워두면 자식 SpriteRenderer 자동 수집)")]
    [SerializeField, InspectorName("대상 렌더러들")]
    private SpriteRenderer[] targetRenderers;

    private Color[] _original;

    private void Awake()
    {
        CacheTargetsIfNeeded();
        CacheOriginalColors();
    }

    private void OnEnable()
    {
        if (GameSettingsRuntime.HasInstance)
        {
            GameSettingsRuntime.Instance.OnSkillOpacityChanged += Apply;
            Apply(GameSettingsRuntime.Instance.SkillOpacity);
        }
    }

    private void OnDisable()
    {
        if (GameSettingsRuntime.HasInstance)
            GameSettingsRuntime.Instance.OnSkillOpacityChanged -= Apply;

        Restore();
    }

    private void CacheTargetsIfNeeded()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void CacheOriginalColors()
    {
        if (targetRenderers == null) return;

        _original = new Color[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            _original[i] = targetRenderers[i].color;
        }
    }

    private void Apply(float opacity01)
    {
        if (targetRenderers == null || _original == null) return;

        float aMul = Mathf.Clamp01(opacity01);

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;

            Color c = _original[i];
            c.a = _original[i].a * aMul;
            r.color = c;
        }
    }

    private void Restore()
    {
        if (targetRenderers == null || _original == null) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            var r = targetRenderers[i];
            if (r == null) continue;
            r.color = _original[i];
        }
    }
}