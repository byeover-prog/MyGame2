using UnityEngine;

[System.Serializable]
public struct CommonSkillLevelParams
{
    [Header("공통")]
    public float cooldown;
    public int damage;

    [Tooltip("투사체/검 개수 등(스킬마다 의미가 다름)")]
    public int projectileCount;

    public float projectileSpeed;
    public float lifeSeconds;
    public float maxDistance;

    [Header("확장(스킬별 사용)")]
    public float spreadAngleDeg;     // 멀티샷/다발 발사용
    public int bounceCount;          // 수리검
    public int chainCount;           // 호밍 미사일(추가 타겟 횟수)
    public int splitCount;           // 다크오브(분열 수)
    public float explosionRadius;    // 다크오브 폭발 반경
    public float childSpeed;         // 다크오브 분열체 속도

    public float hitInterval;        // 회전검 타격 틱
    public float orbitRadius;        // 회전검 반지름
    public float orbitAngularSpeed;  // 회전검 각속도(도/초)

    public float returnSpeed;        // 부메랑 귀환 속도
    public float turnSpeedDeg;       // 호밍 회전 속도(도/초)
}