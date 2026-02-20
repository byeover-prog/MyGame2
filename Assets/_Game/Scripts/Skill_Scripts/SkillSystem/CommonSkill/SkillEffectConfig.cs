using UnityEngine;

/// <summary>
/// 모든 공통 스킬의 Inspector 조정 가능한 레벨별 파라미터.
/// CommonSkillConfigSO.levels[] 배열로 관리되며, 런타임에서 즉시 적용됩니다.
///
/// 각 필드는 어떤 스킬에서 어떤 의미로 쓰이는지 Tooltip에 명시되어 있습니다.
/// 스킬에 사용되지 않는 필드는 기본값(0)으로 놔두면 됩니다.
/// </summary>
[System.Serializable]
public struct SkillEffectConfig
{
    // ─────────────────────────────────────────
    // [공통] 모든 스킬에서 사용
    // ─────────────────────────────────────────

    [Header("공통")]
    [Tooltip("발사/재사용 쿨타임(초).\n" +
             "OrbitingBlade: 사용 안 함(상시 회전)\n" +
             "ArrowRain: 장판 소환 쿨타임")]
    [Min(0.01f)]
    public float cooldown;

    [Tooltip("피해량(정수).\n" +
             "ArrowRain: 틱당 피해량")]
    [Min(0)]
    public int damage;

    [Tooltip("투사체 또는 오브젝트 개수.\n" +
             "OrbitingBlade: 회전하는 검 개수\n" +
             "Boomerang: 동시 발사 부메랑 수\n" +
             "PiercingBullet: 동시 발사 총알 수\n" +
             "ArrowShot: 동시 발사 화살 수")]
    [Min(1)]
    public int projectileCount;

    [Tooltip("투사체 이동 속도(유닛/초).\n" +
             "ArrowRain: 사용 안 함")]
    [Min(0f)]
    public float projectileSpeed;

    [Tooltip("투사체 수명(초). 수명이 끝나면 풀에 반환됩니다.\n" +
             "ArrowRain: 장판 지속 시간")]
    [Min(0.05f)]
    public float lifeSeconds;

    [Tooltip("투사체 최대 이동 거리(유닛).\n" +
             "Boomerang: 날아가는 최대 거리")]
    [Min(0f)]
    public float maxDistance;

    // ─────────────────────────────────────────
    // [멀티샷] 퍼짐 각도
    // ─────────────────────────────────────────

    [Header("멀티샷 퍼짐")]
    [Tooltip("발사 방향에서 각 투사체가 벌어지는 각도(도).\n" +
             "ArrowShot, PiercingBullet, Boomerang에서 다중 발사 시 사용.\n" +
             "0이면 퍼짐 없이 일직선으로 발사됩니다.")]
    [Min(0f)]
    public float spreadAngleDeg;

    // ─────────────────────────────────────────
    // [OrbitingBlade] 회전검 전용
    // ─────────────────────────────────────────

    [Header("회전검 (OrbitingBlade)")]
    [Tooltip("같은 적에게 다시 피해를 줄 수 있는 최소 간격(초).\n" +
             "너무 짧으면 적이 너무 빨리 죽으므로 0.3~0.6 권장.")]
    [Min(0.05f)]
    public float hitInterval;

    [Tooltip("플레이어 중심으로부터 검이 회전하는 반지름(유닛).")]
    [Min(0.1f)]
    public float orbitRadius;

    [Tooltip("검이 회전하는 각속도(도/초). 양수=반시계, 음수=시계 방향.\n" +
             "권장: 120~360.")]
    public float orbitAngularSpeed;

    // ─────────────────────────────────────────
    // [Boomerang] 부메랑 전용
    // ─────────────────────────────────────────

    [Header("부메랑 (Boomerang)")]
    [Tooltip("플레이어에게 돌아올 때의 속도(유닛/초).\n" +
             "outSpeed(projectileSpeed)보다 약간 빠르게 하면 자연스럽습니다.")]
    [Min(0.1f)]
    public float returnSpeed;

    // ─────────────────────────────────────────
    // [HomingMissile] 호밍 미사일 전용
    // ─────────────────────────────────────────

    [Header("호밍 미사일 (HomingMissile)")]
    [Tooltip("목표물을 향해 방향을 꺾는 각속도(도/초).\n" +
             "값이 클수록 더 날카롭게 추적합니다. 권장: 60~200.")]
    [Min(0f)]
    public float turnSpeedDeg;

    [Tooltip("명중 후 다음 적으로 연쇄 타격하는 횟수(0=연쇄 없음).")]
    [Min(0)]
    public int chainCount;

    // ─────────────────────────────────────────
    // [DarkOrb] 암흑구 전용
    // ─────────────────────────────────────────

    [Header("암흑구 (DarkOrb)")]
    [Tooltip("명중 시 폭발 반경(유닛). 범위 내 적 전체에게 피해.")]
    [Min(0f)]
    public float explosionRadius;

    [Tooltip("폭발 후 방사형으로 발사되는 분열 투사체 수(0=분열 없음).")]
    [Min(0)]
    public int splitCount;

    [Tooltip("분열 투사체의 이동 속도(유닛/초).")]
    [Min(0f)]
    public float childSpeed;

    // ─────────────────────────────────────────
    // [Shuriken] 수리검 전용
    // ─────────────────────────────────────────

    [Header("수리검 (Shuriken)")]
    [Tooltip("적 명중 후 다음 적으로 튕기는 횟수(0=튕김 없음).")]
    [Min(0)]
    public int bounceCount;

    // ─────────────────────────────────────────
    // [Balsi] 발시 관통 전용
    // ─────────────────────────────────────────

    [Header("발시 관통 (Balsi)")]
    [Tooltip("투사체가 적을 관통할 수 있는 횟수(0=첫 타격 후 소멸).")]
    [Min(0)]
    public int pierceCount;

    // ─────────────────────────────────────────
    // [ArrowRain] 화살비 장판 전용
    // ─────────────────────────────────────────

    [Header("화살비 장판 (ArrowRain)")]
    [Tooltip("장판(화살비 범위)의 반지름(유닛).\n" +
             "cooldown: 장판 소환 쿨타임\n" +
             "lifeSeconds: 장판 지속 시간\n" +
             "damage: 틱당 피해\n" +
             "hitInterval: 피해 틱 간격")]
    [Min(0.1f)]
    public float areaRadius;

    [Tooltip("장판 안에서 피해가 적용되는 시간 간격(초). 권장: 0.2~0.5.\n" +
             "OrbitingBlade의 hitInterval과 용도가 같으므로, ArrowRain에서는 hitInterval을 재사용합니다.")]
    [Min(0.05f)]
    public float areaDamageTickInterval;
}
