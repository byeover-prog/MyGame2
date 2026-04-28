using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SeolbingtanWeapon2D : CharacterSkillWeaponBase
{
    [Header("설빙탄 설정")]
    [Tooltip("화살 투사체 풀입니다.")]
    [SerializeField] private ProjectilePool2D arrowPool;

    [Tooltip("발사 시작점입니다. 비우면 owner 위치 사용.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("화살 비행 속도입니다.")]
    [SerializeField] private float arrowSpeed = 12f;

    [Tooltip("화살 최대 비행 시간(초)입니다. 적 못 맞히면 사라짐.")]
    [SerializeField] private float arrowMaxFlightTime = 2.0f;

    [Tooltip("부착 후 폭발까지 시간(초)입니다.")]
    [SerializeField] private float attachDelay = 0.5f;

    [Tooltip("폭발 반경(유닛)입니다.")]
    [SerializeField] private float explosionRadius = 1.5f;

    [Tooltip("발사 시 플레이어 위치에서의 오프셋(유닛).")]
    [SerializeField] private float spawnOffset = 0.5f;

    [Header("빙결 효과")]
    [Tooltip("폭발 시 적용되는 동상 지속 시간(초)입니다.")]
    [SerializeField] private float frostDuration = 2.0f;

    [Tooltip("동상 이속 감소 비율입니다. 0.5 = 50% 감속.")]
    [SerializeField] private float frostSlowMultiplier = 0.5f;

    [Header("레벨 스케일링")]
    [Tooltip("레벨당 피해량 증가 비율입니다. 0.10 = +10%.")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 쿨다운 감소 비율입니다. 0.10 = -10%/Lv.")]
    [SerializeField] private float cooldownReductionPerLevel = 0.10f;

    [Tooltip("쿨다운 감소 최저 한도(곱). 0.4 = 최대 60% 감소까지.")]
    [SerializeField] private float cooldownMinMultiplier = 0.4f;

    [Header("각성 (Lv7+)")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 추가 시전 횟수.")]
    [SerializeField] private int awakeningExtraCasts = 3;

    [Tooltip("다발 발사 시 화살 사이 발사 각도(도).")]
    [SerializeField] private float multiCastAngleSpread = 15f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    protected override void Awake()
    {
        base.Awake();

        element = DamageElement2D.Ice;
        baseDamage = 10;
        baseCooldown = 2.5f;
        balanceId = "weapon_seolbingtan";

        if (arrowPool == null)
            arrowPool = GetComponentInChildren<ProjectilePool2D>(true);

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        if (owner == null) return;
        if (arrowPool == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Fire();
        cooldownTimer = GetFinalCooldown();
    }

    private void Fire()
    {
        Vector3 ownerPos = owner.position;
        int finalDamage = GetFinalDamage();
        int castCount = GetCastCount();

        // 가장 가까운 적 1마리 (대표 타겟)
        Transform mainTarget = null;
        if (TryGetNearestEnemy(out EnemyRegistryMember2D enemy) && enemy != null)
            mainTarget = enemy.Transform;

        // castCount만큼 발사
        for (int i = 0; i < castCount; i++)
        {
            // 다발 발사 시 각도 분산
            float angleOffset = 0f;
            if (castCount > 1)
            {
                float halfSpread = multiCastAngleSpread * (castCount - 1) * 0.5f;
                angleOffset = -halfSpread + multiCastAngleSpread * i;
            }

            FireOne(ownerPos, mainTarget, finalDamage, angleOffset);
        }

        if (debugLog)
            CombatLog.Log(
                $"[설빙탄] 발사 {castCount}발 dmg={finalDamage} target={(mainTarget != null ? mainTarget.name : "없음")}",
                this);
    }

    private void FireOne(Vector3 ownerPos, Transform target, int damage, float angleOffsetDeg)
    {
        // 발사 방향 결정
        Vector2 direction;
        if (target != null)
        {
            direction = ((Vector2)target.position - (Vector2)ownerPos).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        }
        else
        {
            direction = Vector2.right;
        }

        // 각도 분산 적용
        if (Mathf.Abs(angleOffsetDeg) > 0.01f)
        {
            float rad = angleOffsetDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            direction = new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        Vector3 origin = spawnPoint != null ? spawnPoint.position : ownerPos;
        Vector3 spawnPos = origin + (Vector3)(direction * spawnOffset);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        var arrow = arrowPool.Get<SeolbingtanArrow2D>(spawnPos, rot);
        if (arrow == null) return;

        arrow.Initialize(
            damage: damage,
            direction: direction,
            speed: arrowSpeed,
            maxFlightTime: arrowMaxFlightTime,
            attachDelay: attachDelay,
            explosionRadius: ScaleRadius(explosionRadius, 0.2f),
            element: element,
            frostDuration: frostDuration,
            frostSlowMultiplier: frostSlowMultiplier);
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
        float cdMultiplier = Mathf.Max(cooldownMinMultiplier,
            1f - cooldownReductionPerLevel * Mathf.Max(0, level - 1));
        return ScaleCooldown(cd * cdMultiplier, 0.1f);
    }

    private int GetCastCount()
    {
        int count = 1;
        if (level >= awakeningLevel)
            count += awakeningExtraCasts;
        return Mathf.Max(1, count);
    }
}