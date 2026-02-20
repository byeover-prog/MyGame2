using UnityEngine;

/// <summary>
/// [화살비] CommonSkillWeapon2D 래퍼.
///
/// 동작 (레벨 1):
///  - 쿨타임(P.cooldown)마다 가장 가까운 적 위치에 ArrowRainArea2D 장판을 소환합니다.
///  - 장판은 P.lifeSeconds 동안 유지되며, 반경(P.areaRadius) 안의 적에게
///    P.hitInterval마다 P.damage의 피해를 줍니다.
///  - 시각적으로 낙하 화살(ArrowRainFallingArrow)이 장판 위에서 비처럼 떨어집니다.
///
/// SkillEffectConfig 필드 매핑 (ArrowRain 레벨 1 권장값):
///  cooldown        = 5.0f   → 장판 소환 쿨타임(초)
///  damage          = 2      → 틱당 피해량
///  hitInterval     = 0.25f  → 틱 간격(초)
///  areaRadius      = 2.5f   → 장판 반지름(유닛)
///  lifeSeconds     = 4.0f   → 장판 지속 시간(초)
///
/// 프리팹 구성:
///  - 이 스크립트가 붙은 빈 GameObject (이름 예: "ArrowRainWeapon")
///  - areaRainPrefab: ArrowRainArea2D가 붙은 프리팹(CircleCollider2D 필수)
///  - CommonSkillManager2D 가 owner.transform 자식으로 스폰 후 Initialize() 를 호출합니다.
///  - 장판 프리팹은 월드 공간(null parent)에 Instantiate되므로
///    플레이어 이동에 영향받지 않고 소환 위치에 고정됩니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArrowRainWeapon2D : CommonSkillWeapon2D
{
    [Header("화살비 장판 프리팹")]
    [Tooltip("ArrowRainArea2D 컴포넌트가 붙어있는 프리팹.\n" +
             "CircleCollider2D(IsTrigger=true)와 ArrowRainFallingArrow 풀이 포함되어야 합니다.")]
    [SerializeField] private ArrowRainArea2D areaRainPrefab;

    [Header("타겟 선택")]
    [Tooltip("true: 가장 가까운 적 위치에 소환. false: 플레이어 위치에 소환.")]
    [SerializeField] private bool spawnAtNearestEnemy = true;

    [Tooltip("적이 없을 때 플레이어 위치에서 이 거리만큼 앞쪽에 소환합니다.")]
    [SerializeField, Min(0f)] private float fallbackSpawnOffset = 2f;

    private void Update()
    {
        if (config == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        EnemyRegistryMember2D target = null;

        if (requireTargetToFire)
        {
            if (!TryGetNearest(out target))
                return;
        }
        else
        {
            TryGetNearest(out target);
        }

        TryBeginFireConsumeCooldown(() => SpawnArea(target));
    }

    private void SpawnArea(EnemyRegistryMember2D target)
    {
        if (areaRainPrefab == null)
        {
            Debug.LogWarning("[ArrowRainWeapon2D] areaRainPrefab이 비어있습니다. Inspector에서 할당하세요.", this);
            return;
        }

        if (owner == null) return;

        // 소환 위치 결정
        Vector2 spawnPos;
        if (spawnAtNearestEnemy && target != null)
        {
            spawnPos = target.Position;
        }
        else
        {
            // 플레이어 위치 + 작은 전방 오프셋(기본)
            spawnPos = (Vector2)owner.position + Vector2.right * fallbackSpawnOffset;
        }

        // 장판 스폰 - 월드 공간에 배치(플레이어 이동에 영향 없음)
        ArrowRainArea2D area = Instantiate(areaRainPrefab, spawnPos, Quaternion.identity, parent: null);

        // SkillEffectConfig 값 주입
        // hitInterval 필드를 ArrowRain의 틱 간격으로 재사용합니다(0이면 기본값 사용).
        float tickInterval = P.areaDamageTickInterval > 0.05f ? P.areaDamageTickInterval
                             : (P.hitInterval > 0.05f ? P.hitInterval : 0.25f);
        float areaRad = P.areaRadius > 0.1f ? P.areaRadius : 2.5f;

        area.Configure(
            tickDamage:    Mathf.Max(1, P.damage),
            tickInterval:  tickInterval,
            areaRad:       areaRad,
            duration:      Mathf.Max(0.5f, P.lifeSeconds),
            mask:          enemyMask
        );

        area.gameObject.SetActive(true);
    }
}
