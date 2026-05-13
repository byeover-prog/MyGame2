using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BingjuWeapon2D : CharacterSkillWeaponBase
{
    [Header("빙주 설정 (Fallback 기본값)")]
    [Tooltip("빙주 풀입니다.")]
    [SerializeField] private ProjectilePool2D spikePool;

    [Tooltip("빙주 적중 반경 fallback. SO에 hitRadius 키가 있으면 SO 우선. 기획=0.8")]
    [SerializeField] private float baseHitRadius = 0.8f;

    [Tooltip("빙주 예고 시간 fallback. SO에 armDelay/delay 키가 있으면 SO 우선. 기획=0.3")]
    [SerializeField] private float armDelay = 0.3f;

    [Tooltip("빙주 총 유지 시간 fallback. SO에 lifetime 키가 있으면 SO 우선. 기획=0.7")]
    [SerializeField] private float spikeLifetime = 0.7f;

    [Tooltip("동상 지속 시간 fallback. SO에 frostDuration/duration 키가 있으면 SO 우선. 기획=3.0")]
    [SerializeField] private float frostDuration = 3f;

    [Tooltip("동상 이동속도 배율 fallback. SO에 frostSlowMultiplier 키가 있으면 SO 우선. 기획=0.5")]
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
        // ★ v1 SO 전환:
        //  base.Awake() 호출만 유지.
        //  element / baseDamage / baseCooldown 직접 대입 제거됨.
        //  → element는 SO에서 자동으로 읽힘 (CharacterSkillWeaponBase.SyncDefinitionBasicInfo).
        //  → damage/cooldown은 사용 지점에서 GetBalance* 로 읽음.
        //  → balanceId가 비어있으면 SO의 SkillId 사용. 미설정 시 인스펙터에서 직접 입력 가능.
        base.Awake();

        if (spikePool == null)
            spikePool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    private void Start()
    {
        Debug.Log(
            $"[빙주 진단] Start | owner={(owner == null ? "NULL" : owner.name)} " +
            $"| spikePool={(spikePool == null ? "★NULL★" : spikePool.name)} " +
            $"| enemyMask.value={enemyMask.value} " +
            $"| element={element} (★ SO/Inspector 출처)",
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

        // ★ v1 전환: 모든 수치를 SO 우선순위로 읽음
        int dmg = GetSpikeDamage();
        float hitRadius = GetHitRadius();
        float curArmDelay = GetArmDelay();
        float curLifetime = GetSpikeLifetime();
        float curFrostDuration = GetFrostDuration();
        float curFrostSlow = GetFrostSlowMultiplier();

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
                damage: dmg,
                hitRadius: hitRadius,
                armDelay: curArmDelay,
                lifetime: curLifetime,
                impactPoint: impactPoint,
                frostDuration: curFrostDuration,
                frostSlowMultiplier: curFrostSlow,
                enableLog: debugLog,
                trackedEnemy: enemy
            );
        }

        if (!_diagFirstSpawnLogged)
        {
            Debug.Log(
                $"[빙주 진단] 첫 발사 성공! 생성 {pickedCount}개 | " +
                $"dmg={dmg} hitR={hitRadius:F2} arm={curArmDelay:F2} life={curLifetime:F2} " +
                $"frostDur={curFrostDuration:F2} frostSlow={curFrostSlow:F2}",
                this);
            _diagFirstSpawnLogged = true;
        }

        _pickedTargets.Clear();

        // ★ v1 전환: SO/JSON/fallback 우선순위로 쿨다운 결정
        cooldownTimer = ScaleCooldown(GetBalanceCooldown(), 0.1f);

        if (debugLog)
            CombatLog.Log($"[빙주] 생성 {pickedCount}개 dmg={dmg} cd={cooldownTimer:F2}", this);
    }

    // ════════════════════════════════════════════════════
    //  수치 게터 — 모두 SO → JSON → 인스펙터 fallback 순서
    // ════════════════════════════════════════════════════

    /// <summary>피해량. SO/JSON > baseDamage</summary>
    private int GetSpikeDamage()
    {
        return ScaleDamage(GetBalanceDamage());
    }

    /// <summary>적중 반경. SO/JSON("hitRadius"/"radius") > baseHitRadius</summary>
    private float GetHitRadius()
    {
        return ScaleRadius(GetBalanceFloat("hitRadius", baseHitRadius), 0.1f);
    }

    /// <summary>예고 시간. SO("armDelay"/"delay") > armDelay</summary>
    private float GetArmDelay()
    {
        return GetBalanceFloat("armDelay", armDelay);
    }

    /// <summary>스파이크 유지 시간. SO("lifetime") > spikeLifetime</summary>
    private float GetSpikeLifetime()
    {
        return GetBalanceFloat("lifetime", spikeLifetime);
    }

    /// <summary>동상 지속 시간. SO("frostDuration"/"duration") > frostDuration</summary>
    private float GetFrostDuration()
    {
        return GetBalanceFloat("frostDuration", frostDuration);
    }

    /// <summary>동상 이속 배율. SO("frostSlowMultiplier"/"slowRate") > frostSlowMultiplier</summary>
    private float GetFrostSlowMultiplier()
    {
        return GetBalanceFloat("frostSlowMultiplier", frostSlowMultiplier);
    }
}