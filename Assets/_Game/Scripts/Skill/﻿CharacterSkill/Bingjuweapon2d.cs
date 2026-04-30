using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 빙주입니다. (윤설 전용 / 빙결 속성 / 단일 타겟 핀포인트)
/// 랜덤 적 1명을 예고 추적 후 얼음 기둥으로 직격합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BingjuWeapon2D : CharacterSkillWeaponBase
{
    [Header("빙주 설정 (기획값)")]
    [Tooltip("빙주 풀입니다.")]
    [SerializeField] private ProjectilePool2D spikePool;

    [Tooltip("빙주 적중 반경입니다. 기획=0.8 (추적 대상 사망 시 fallback 탐색용)")]
    [SerializeField] private float baseHitRadius = 0.8f;

    [Tooltip("빙주 예고 시간입니다. 기획=0.3")]
    [SerializeField] private float armDelay = 0.3f;

    [Tooltip("빙주 총 유지 시간입니다. 기획=0.7")]
    [SerializeField] private float spikeLifetime = 0.7f;

    [Tooltip("동상 지속 시간입니다. 기획=3.0")]
    [SerializeField] private float frostDuration = 3f;

    [Tooltip("동상 이동속도 배율입니다. 기획=0.5 (50% 속도)")]
    [SerializeField] private float frostSlowMultiplier = 0.5f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<EnemyRegistryMember2D> _pickedTargets = new List<EnemyRegistryMember2D>(4);
    private readonly List<EnemyRegistryMember2D> _randomScratch = new List<EnemyRegistryMember2D>(32);

    private bool _diagOwnerNullLogged;
    private bool _diagPoolNullLogged;
    private bool _diagNoEnemiesLogged;
    private bool _diagFirstSpawnLogged;

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Ice;

        if (spikePool == null)
            spikePool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    private void Start()
    {
        Debug.Log(
            $"[빙주 진단] Start | owner={(owner == null ? "NULL" : owner.name)} " +
            $"| spikePool={(spikePool == null ? "★NULL★" : spikePool.name)} " +
            $"| enemyMask.value={enemyMask.value}",
            this);

        if (spikePool == null)
        {
            Debug.LogError("[빙주 진단] spikePool NULL.", this);
        }
    }

    private void Update()
    {
        if (owner == null)
        {
            if (!_diagOwnerNullLogged)
            {
                Debug.LogWarning("[빙주 진단] Update 차단: owner == null", this);
                _diagOwnerNullLogged = true;
            }
            return;
        }

        if (spikePool == null)
        {
            if (!_diagPoolNullLogged)
            {
                Debug.LogWarning("[빙주 진단] Update 차단: spikePool == null", this);
                _diagPoolNullLogged = true;
            }
            return;
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        // count=1 고정, 레벨업해도 증가 없음
        int shotCount = 1;
        int pickedCount = PickRandomEnemies(shotCount, _pickedTargets, _randomScratch);
        if (pickedCount <= 0)
        {
            if (!_diagNoEnemiesLogged)
            {
                Debug.LogWarning("[빙주 진단] 발사 불가: 주변 유효 적 0", this);
                _diagNoEnemiesLogged = true;
            }
            return;
        }

        _diagNoEnemiesLogged = false;

        for (int i = 0; i < pickedCount; i++)
        {
            EnemyRegistryMember2D enemy = _pickedTargets[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            Vector3 impactPoint = enemy.Transform != null
                ? enemy.Transform.position
                : (Vector3)enemy.Position;

            BingjuSpikeArea2D spike = spikePool.Get<BingjuSpikeArea2D>(impactPoint, Quaternion.identity);
            if (spike == null)
            {
                if (!_diagFirstSpawnLogged)
                {
                    Debug.LogError("[빙주 진단] spikePool.Get 반환 NULL", this);
                    _diagFirstSpawnLogged = true;
                }
                continue;
            }

            spike.Init(
                enemyMask: enemyMask,
                damageElement: element,
                damage: GetSpikeDamage(),
                hitRadius: GetHitRadius(),
                armDelay: armDelay,
                lifetime: spikeLifetime,
                impactPoint: impactPoint,
                frostDuration: GetBalanceFloat("frostDuration", frostDuration),
                frostSlowMultiplier: GetBalanceFloat("frostSlowMultiplier", frostSlowMultiplier),
                enableLog: debugLog,
                trackedEnemy: enemy
            );
        }

        if (!_diagFirstSpawnLogged)
        {
            Debug.Log($"[빙주 진단] 첫 발사 성공! 생성 {pickedCount}개", this);
            _diagFirstSpawnLogged = true;
        }

        _pickedTargets.Clear();
        cooldownTimer = ScaleCooldown(GetBalanceCooldown(), 0.1f);

        if (debugLog)
            CombatLog.Log($"[빙주] 생성 {pickedCount}개", this);
    }

    // damageAddPerLevel = 0 — 레벨업해도 데미지 고정
    private int GetSpikeDamage()
    {
        return ScaleDamage(GetBalanceDamage());
    }

    // explosionRadius = 0.8 고정 (ScaleRadius의 레벨업 보정은 유지해도 무방)
    private float GetHitRadius()
    {
        return ScaleRadius(GetBalanceFloat("hitRadius", baseHitRadius), 0.1f);
    }
}