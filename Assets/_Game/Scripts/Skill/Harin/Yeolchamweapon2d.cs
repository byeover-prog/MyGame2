using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 열참 (熱斬) — 하린 전용 스킬 #1 (음 속성, 근접 원형 범위)
///
/// 컨셉: 플레이어 몸 기준 원형 검 휘두름.
///   - 외곽 링(바깥쪽 30%) 적 = +30% 추가 데미지
///   - 내부 적 = -30% 감소 데미지
///   (LOL 다리우스 Q 메카닉)
///
/// 레벨 스케일링:
///   - 레벨당 피해량 +15%
///
/// 각성 (Lv7+):
///   - 외곽 적중 시 출혈 효과 부여
///   - 재사용 대기시간 30% 감소
///
/// 프리팹 구조:
///   Weapon_Yeolcham (프리팹 루트)
///   ├── LevelableSkillMarker2D
///   ├── YeolchamWeapon2D (이 스크립트, CharacterSkillWeaponBase 상속)
///   └── Pool_Yeolcham (ProjectilePool2D + PF_YeolchamSlash)
/// </summary>
[DisallowMultipleComponent]
public sealed class YeolchamWeapon2D : CharacterSkillWeaponBase
{
    [Header("열참 설정")]
    [Tooltip("YeolchamSlash2D 투사체 풀입니다.")]
    [SerializeField] private ProjectilePool2D slashPool;

    [Tooltip("참격 반경(월드 유닛)입니다. 외곽 링 포함 전체 반경.")]
    [SerializeField] private float baseSlashRadius = 3.0f;

    [Tooltip("참격 지속시간(초)입니다.")]
    [SerializeField] private float slashLifetime = 0.5f;

    [Header("외곽/내부 차등 데미지")]
    [Tooltip("외곽 링이 차지하는 반경 비율입니다. 0.7 = 외곽 30% 영역.")]
    [Range(0.5f, 0.95f)]
    [SerializeField] private float outerRingRatio = 0.7f;

    [Tooltip("외곽 링 적중 시 데미지 배율입니다. 1.30 = +30% 추가.")]
    [SerializeField] private float outerDamageMultiplier = 1.30f;

    [Tooltip("내부 영역 적중 시 데미지 배율입니다. 0.70 = -30% 감소.")]
    [SerializeField] private float innerDamageMultiplier = 0.70f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.15 = +15%.")]
    [SerializeField] private float damagePerLevel = 0.15f;

    [Header("각성 보너스 (Lv7+)")]
    [Tooltip("각성 발동 레벨입니다.")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 재사용 대기시간 감소 비율입니다. 0.30 = -30%.")]
    [SerializeField] private float awakeningCooldownReduction = 0.30f;

    [Tooltip("각성 시 외곽 적중 적에게 부여하는 출혈 지속시간(초)입니다.")]
    [SerializeField] private float awakeningBleedDuration = 3.0f;

    [Tooltip("각성 시 출혈 초당 피해량(현재 데미지의 비율)입니다. 0.10 = 10%/초.")]
    [SerializeField] private float awakeningBleedDpsRatio = 0.10f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    protected override void Awake()
    {
        base.Awake();

        element = DamageElement2D.Dark;
        baseDamage = 10;
        baseCooldown = 4.0f;
        balanceId = "weapon_yeolcham";  // JSON 밸런스 자동 로드

        if (slashPool == null)
            slashPool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    private void Update()
    {
        if (owner == null) return;
        if (slashPool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Fire();
        cooldownTimer = GetFinalCooldown();
    }

    private void Fire()
    {
        Vector3 ownerPos = owner.position;

        var slash = slashPool.Get<YeolchamSlash2D>(ownerPos, Quaternion.identity);
        if (slash == null) return;

        slash.Initialize(
            damage: GetFinalDamage(),
            radius: GetFinalRadius(),
            lifetime: slashLifetime,
            owner: owner,
            element: element,
            outerRingRatio: outerRingRatio,
            outerMultiplier: outerDamageMultiplier,
            innerMultiplier: innerDamageMultiplier,
            awakened: IsAwakened(),
            bleedDuration: awakeningBleedDuration,
            bleedDpsRatio: awakeningBleedDpsRatio);

        if (debugLog)
            CombatLog.Log(
                $"[열참] 휘두름! dmg={GetFinalDamage()} r={GetFinalRadius():F1} 각성={IsAwakened()}",
                this);
    }

    // ── 계산 헬퍼 ──

    private int GetFinalDamage()
    {
        int finalBase = GetBalanceDamage();
        float levelScale = 1f + damagePerLevel * Mathf.Max(0, level - 1);
        return ScaleDamage(finalBase * levelScale);
    }

    private float GetFinalCooldown()
    {
        float cd = GetBalanceCooldown();
        if (IsAwakened())
            cd *= (1f - awakeningCooldownReduction);
        return ScaleCooldown(cd, 0.1f);
    }

    private float GetFinalRadius()
    {
        return ScaleRadius(baseSlashRadius, 0.5f);
    }

    private bool IsAwakened()
    {
        return level >= awakeningLevel;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = owner != null ? owner.position : transform.position;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(c, baseSlashRadius);

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(c, baseSlashRadius * outerRingRatio);
    }
#endif
}