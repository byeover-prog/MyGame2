using UnityEngine;

/// <summary>
/// 스킬 이펙트 투명도 설정(임시)
/// - 오늘 빌드용 최소 스텁
/// - 나중에 옵션 UI와 연결해서 VfxAlpha 값을 바꾸면 됨
/// </summary>
public sealed class SkillVfxSettings : MonoBehaviour
{
    public static SkillVfxSettings Instance { get; private set; }

    [Header("스킬 이펙트 투명도")]
    [Range(0f, 1f)]
    [SerializeField] private float vfxAlpha = 1f;

    public float VfxAlpha => vfxAlpha;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
