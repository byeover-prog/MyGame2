using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NoeunWeapon2D : CharacterSkillWeaponBase
{
    [Header("뇌운 설정 — 풀 2개")]
    [Tooltip("구름 본체 풀입니다.")]
    [SerializeField] private ProjectilePool2D cloudPool;

    [Tooltip("번개 자식 풀입니다. NoeunCloud2D로 전달됨.")]
    [SerializeField] private ProjectilePool2D boltPool;

    [Header("기본 수치")]
    [Tooltip("번개 폭발 범위(유닛)입니다. 레벨당 +10% 증가.")]
    [SerializeField] private float baseBoltRadius = 1.0f;

    [Tooltip("구름 지속 시간(초)입니다.")]
    [SerializeField] private float cloudLifetime = 5.0f;

    [Tooltip("번개 발사 간격(초)입니다.")]
    [SerializeField] private float boltInterval = 0.5f;

    [Tooltip("구름이 적을 따라가는 속도(유닛/초). 적보다 살짝 빠르게.")]
    [SerializeField] private float cloudFollowSpeed = 4.0f;

    [Tooltip("초기 구름 스폰 시 플레이어로부터의 반경(유닛). 0이면 플레이어 위치.")]
    [SerializeField] private float spawnRadius = 1.5f;

    [Tooltip("구름 초기 타겟 탐색 최대 반경입니다.")]
    [SerializeField] private float initialSeekRadius = 12f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 번개 폭발 범위 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float radiusPerLevel = 0.10f;

    [Header("각성 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 추가 시전 횟수.")]
    [SerializeField] private int awakeningExtraCasts = 3;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    protected override void Awake()
    {
        base.Awake();

        element = DamageElement2D.Electric;
        baseDamage = 5;
        baseCooldown = 5.0f;
        balanceId = "weapon_noeun";

        // 풀 자동 탐색 (이름 기반)
        if (cloudPool == null || boltPool == null)
        {
            ProjectilePool2D[] pools = GetComponentsInChildren<ProjectilePool2D>(true);
            for (int i = 0; i < pools.Length; i++)
            {
                ProjectilePool2D p = pools[i];
                if (p == null) continue;
                if (cloudPool == null && (p.name.Contains("Cloud") || p.name.Contains("구름")))
                    cloudPool = p;
                else if (boltPool == null && (p.name.Contains("Bolt") || p.name.Contains("번개")))
                    boltPool = p;
            }
        }
    }

    private void Update()
    {
        if (owner == null) return;
        if (cloudPool == null || boltPool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Fire();
        cooldownTimer = ScaleCooldown(GetBalanceCooldown(), 0.1f);
    }

    private void Fire()
    {
        Vector3 ownerPos = owner.position;
        int finalDamage = GetFinalDamage();
        float finalRadius = GetFinalRadius();
        int castCount = GetCastCount();

        for (int i = 0; i < castCount; i++)
        {
            // 다발 발사 시 플레이어 주위에 분산
            Vector3 spawnPos = ownerPos;
            if (castCount > 1)
            {
                float angle = (i / (float)castCount) * 2f * Mathf.PI;
                spawnPos += new Vector3(
                    Mathf.Cos(angle) * spawnRadius,
                    Mathf.Sin(angle) * spawnRadius,
                    0f);
            }
            else if (spawnRadius > 0.01f)
            {
                spawnPos += new Vector3(0f, spawnRadius, 0f);
            }

            var cloud = cloudPool.Get<NoeunCloud2D>(spawnPos, Quaternion.identity);
            if (cloud == null) continue;

            cloud.Initialize(
                damage: finalDamage,
                boltRadius: finalRadius,
                lifetime: cloudLifetime,
                boltInterval: boltInterval,
                followSpeed: cloudFollowSpeed,
                seekRadius: initialSeekRadius,
                element: element,
                boltPool: boltPool);
        }

        if (debugLog)
            CombatLog.Log(
                $"[뇌운] 구름 {castCount}개 소환! dmg={finalDamage} 범위={finalRadius:F2}",
                this);
    }

    // ── 계산 헬퍼 ──

    private int GetFinalDamage()
    {
        int finalBase = GetBalanceDamage();
        float levelScale = 1f + damagePerLevel * Mathf.Max(0, level - 1);
        return ScaleDamage(finalBase * levelScale);
    }

    private float GetFinalRadius()
    {
        float radiusScale = 1f + radiusPerLevel * Mathf.Max(0, level - 1);
        return ScaleRadius(baseBoltRadius * radiusScale, 0.2f);
    }

    private int GetCastCount()
    {
        int count = 1;
        if (level >= awakeningLevel)
            count += awakeningExtraCasts;
        return Mathf.Max(1, count);
    }
}