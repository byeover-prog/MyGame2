using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빙주입니다.
/// 랜덤 적 발밑에 짧은 예고 후 얼음 기둥을 솟구치게 합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BingjuWeapon2D : CharacterSkillWeaponBase
{
    [Header("빙주 설정")]
    [Tooltip("빙주 풀입니다.")]
    [SerializeField] private ProjectilePool2D spikePool;

    [Tooltip("빙주 적중 반경입니다.")]
    [SerializeField] private float baseHitRadius = 0.8f;

    [Tooltip("빙주 예고 시간입니다.")]
    [SerializeField] private float armDelay = 0.3f;

    [Tooltip("빙주 총 유지 시간입니다.")]
    [SerializeField] private float spikeLifetime = 0.7f;

    [Tooltip("동상 지속 시간입니다.")]
    [SerializeField] private float frostDuration = 3f;

    [Tooltip("동상 이동속도 배율입니다. 0.5면 50% 이동속도입니다.")]
    [SerializeField] private float frostSlowMultiplier = 0.5f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<EnemyRegistryMember2D> _pickedTargets = new List<EnemyRegistryMember2D>(8);
    private readonly List<EnemyRegistryMember2D> _randomScratch = new List<EnemyRegistryMember2D>(32);

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Ice; // 윤설 속성 = 빙결
        baseDamage = 15;
        baseCooldown = 1.5f; // 기획 엑셀 R15: 재사용 대기시간 1.5초

        if (spikePool == null)
            spikePool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    private void Update()
    {
        if (owner == null) return;
        if (spikePool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        int shotCount = GetSpikeCount();
        int pickedCount = PickRandomEnemies(shotCount, _pickedTargets, _randomScratch);
        if (pickedCount <= 0)
            return;

        for (int i = 0; i < pickedCount; i++)
        {
            EnemyRegistryMember2D enemy = _pickedTargets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            Vector3 impactPoint = enemy.Transform != null
                ? enemy.Transform.position
                : (Vector3)enemy.Position;

            BingjuSpikeArea2D spike = spikePool.Get<BingjuSpikeArea2D>(impactPoint, Quaternion.identity);
            if (spike == null)
                continue;

            spike.Init(
                enemyMask: enemyMask,
                damageElement: element,
                damage: GetSpikeDamage(),
                hitRadius: GetHitRadius(),
                armDelay: armDelay,
                lifetime: spikeLifetime,
                impactPoint: impactPoint,
                frostDuration: frostDuration,
                frostSlowMultiplier: frostSlowMultiplier,
                enableLog: debugLog
            );
        }

        _pickedTargets.Clear();
        cooldownTimer = ScaleCooldown(baseCooldown, 0.1f);

        if (debugLog)
            CombatLog.Log($"[빙주] 생성 {pickedCount}개", this);
    }

    private int GetSpikeDamage()
    {
        // 기획: Lv2~6 피해량 10%씩 증가 (Lv1 대비 최대 +50%)
        // Lv7~8은 증가 없이 시전 횟수 +1만 (GetSpikeCount에서 처리)
        int bonusSteps = Mathf.Clamp(level - 1, 0, 5);
        float damage = baseDamage * (1f + 0.10f * bonusSteps);
        return ScaleDamage(damage);
    }

    private int GetSpikeCount()
    {
        // 기획: Lv7~8 시전 횟수 +1
        return level >= 7 ? 2 : 1;
    }

    private float GetHitRadius()
    {
        return ScaleRadius(baseHitRadius, 0.1f);
    }
}