using UnityEngine;

public static class CommonSkillLevelParamsConverter
{
    /// <summary>
    /// CommonSkillLevelParams -> SkillEffectConfig 변환
    /// - 기존 자동생성 데이터(구 구조)를 신 구조로 이관하기 위한 브리지
    /// - 없는 필드는 기본값(0) 유지
    /// </summary>
    public static SkillEffectConfig ToEffectConfig(this CommonSkillLevelParams src)
    {
        SkillEffectConfig dst = default;

        // ===== 공통 =====
        dst.cooldown = src.cooldown;
        dst.damage = src.damage;
        dst.projectileCount = src.projectileCount;
        dst.projectileSpeed = src.projectileSpeed;
        dst.lifeSeconds = src.lifeSeconds;
        dst.maxDistance = src.maxDistance;

        // ===== 멀티샷 =====
        dst.spreadAngleDeg = src.spreadAngleDeg;

        // ===== 회전검 =====
        dst.hitInterval = src.hitInterval;
        dst.orbitRadius = src.orbitRadius;
        dst.orbitAngularSpeed = src.orbitAngularSpeed;

        // ===== 부메랑 =====
        dst.returnSpeed = src.returnSpeed;

        // ===== 호밍 =====
        dst.turnSpeedDeg = src.turnSpeedDeg;
        dst.chainCount = src.chainCount;

        // ===== 다크오브 =====
        dst.explosionRadius = src.explosionRadius;
        dst.splitCount = src.splitCount;
        dst.childSpeed = src.childSpeed;

        // ===== 수리검 =====
        dst.bounceCount = src.bounceCount;

        // ===== 발시(신규) =====
        // 구 구조에 pierceCount가 없었을 수 있음.
        // 있으면 대입, 없으면 기본 0 유지.
        // dst.pierceCount = src.pierceCount;  // 필드 존재할 때만 주석 해제

        // ===== 화살비(신규) =====
        // dst.areaRadius = src.areaRadius; // 구 구조에 없을 가능성 높음
        // dst.areaDamageTickInterval = src.areaDamageTickInterval;

        return dst;
    }
}