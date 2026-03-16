using UnityEngine;

/// <summary>
/// 다크오브 무기 스크립트.
/// CommonSkillManager2D에서 호출되며, DarkOrbManager.SpawnRoot()를 호출하는 것이 유일한 역할.
/// 자체적으로 투사체를 생성하거나 관리하지 않는다.
/// </summary>
public class DarkOrbWeapon2D : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════════════════

    [Header("매니저 참조")]
    [Tooltip("씬에 배치된 DarkOrbManager")]
    [SerializeField] private DarkOrbManager _manager;

    [Header("루트 투사체 기본값")]
    [Tooltip("루트 다크오브 이동 속도")]
    [SerializeField] private float _rootSpeed = 6f;

    [Tooltip("루트 다크오브 수명 (초). 이 시간이 지나면 폭발")]
    [SerializeField] private float _rootLifetime = 0.8f;

    [Tooltip("1레벨 기준 폭발 데미지")]
    [SerializeField] private float _baseDamage = 10f;

    [Tooltip("레벨 5~8 폭발 데미지 증가량 (레벨당)")]
    [SerializeField] private float _damagePerLevelAfter4 = 2.5f;

    [Tooltip("폭발 반경")]
    [SerializeField] private float _explosionRadius = 1.5f;

    // ═══════════════════════════════════════════════════════════════
    //  런타임 상태
    // ═══════════════════════════════════════════════════════════════

    private int _currentLevel = 1;
    private float _skillAreaMultiplier = 1f;

    // ═══════════════════════════════════════════════════════════════
    //  공개 API — CommonSkillManager2D가 호출
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 스킬 레벨 설정. 레벨업 시 CommonSkillManager2D에서 호출.
    /// </summary>
    public void SetLevel(int level)
    {
        _currentLevel = Mathf.Clamp(level, 1, 8);
    }

    /// <summary>
    /// 패시브 스킬 범위 배율 설정. PassiveManager2D에서 호출.
    /// </summary>
    public void SetSkillAreaMultiplier(float multiplier)
    {
        _skillAreaMultiplier = multiplier;
    }

    /// <summary>
    /// 다크오브 1발을 발사한다.
    /// CommonSkillManager2D의 발사 로직에서 호출.
    /// </summary>
    /// <param name="origin">플레이어 위치</param>
    /// <param name="targetDirection">가장 가까운 적 방향 (정규화)</param>
    /// <param name="attackPower">현재 최종 공격력 (패시브/버프 반영 후)</param>
    public void Fire(Vector2 origin, Vector2 targetDirection, float attackPower)
    {
        if (_manager == null)
        {
            Debug.LogError("[DarkOrbWeapon2D] DarkOrbManager 참조가 없습니다!");
            return;
        }

        float damage = CalculateDamage(attackPower);
        int splitDepth = CalculateSplitDepth();

        _manager.SpawnRoot(
            origin: origin,
            direction: targetDirection,
            speed: _rootSpeed,
            lifetime: _rootLifetime,
            damage: damage,
            radius: _explosionRadius,
            splitDepth: splitDepth,
            scaleMultiplier: _skillAreaMultiplier
        );
    }

    // ═══════════════════════════════════════════════════════════════
    //  레벨별 계산
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 레벨별 폭발 데미지 계산.
    /// Lv1~4: baseDamage (분열 depth만 증가)
    /// Lv5~8: baseDamage + (level - 4) * damagePerLevelAfter4
    /// 최종 = 레벨 데미지 × 공격력 배율
    /// </summary>
    private float CalculateDamage(float attackPower)
    {
        float levelDamage = _baseDamage;

        if (_currentLevel > 4)
        {
            levelDamage += (_currentLevel - 4) * _damagePerLevelAfter4;
        }

        // 공격력 배율 적용 (설계: 최종 피해량 = 스킬 데미지 × 공격력 보정)
        // attackPower는 이미 패시브/버프가 반영된 최종 공격력
        return levelDamage * (attackPower / 20f); // 기본 공격력 20 기준 정규화
    }

    /// <summary>
    /// 레벨별 분열 깊이(depth) 계산.
    /// Lv1: depth 1 (루트 1개 폭발, 분열 없음 → SplitDepthRemaining = 0)
    /// Lv2: depth 2 (루트 → 자식 2개 → SplitDepthRemaining = 1)
    /// Lv3: depth 3 (루트 → 2 → 4 → SplitDepthRemaining = 2)
    /// Lv4+: depth 4 (루트 → 2 → 4 → 8 → SplitDepthRemaining = 3)
    /// </summary>
    private int CalculateSplitDepth()
    {
        // Lv1 = depth 0 (폭발만, 분열 없음)
        // Lv2 = depth 1 (1→2)
        // Lv3 = depth 2 (1→2→4)
        // Lv4+ = depth 3 (1→2→4→8)
        return Mathf.Clamp(_currentLevel - 1, 0, 3);
    }
}