using System;
using UnityEngine;

// [구현 원리 요약]
// - JsonUtility로 파싱 가능한 "배열 기반" 데이터 구조만 사용한다(딕셔너리 금지).
// - null/0 기본값 문제를 피하기 위해 "HasX()" 형태로 덮어쓰기 여부를 판단한다.
// - 레벨업 증가치는 AddPerLevel로 관리하고 (level-1)만큼 누적 적용한다.
[Serializable]
public sealed class SkillBalanceDB2D
{
    public int version = 1;
    public SkillRow2D[] skills;

    [Serializable]
    public sealed class SkillRow2D
    {
        [Tooltip("스킬 고유 ID (예: weapon_darkorb)")]
        public string id;

        // =========================
        // 공통(기본값) / 레벨당 증가치
        // =========================
        public int damage = -1;
        public int damageAddPerLevel = 0;

        public float cooldown = -1f;
        public float cooldownAddPerLevel = 0f; // 음수면 레벨업할수록 쿨 감소

        public float speed = -1f;
        public float speedAddPerLevel = 0f;

        public float life = -1f;
        public float lifeAddPerLevel = 0f;

        public int count = -1;
        public int countAddPerLevel = 0;

        // =========================
        // 스킬별 전용(기본값) / 레벨당 증가치
        // =========================

        // 회전검
        public float hitInterval = -1f;
        public float hitIntervalAddPerLevel = 0f;

        public float orbitRadius = -1f;
        public float orbitRadiusAddPerLevel = 0f;

        public float orbitSpeed = -1f;
        public float orbitSpeedAddPerLevel = 0f;

        public float active = -1f;          // (네 회전검은 별도 스크립트에서 사용)
        public float activeAddPerLevel = 0f;

        // 부메랑
        public float burstInterval = -1f;
        public float burstIntervalAddPerLevel = 0f;

        public float spinDps = -1f;
        public float spinDpsAddPerLevel = 0f;

        // 수리검(튕김)
        public int bounceCount = -1;
        public int bounceAddPerLevel = 0;

        // 호밍(연쇄)
        public int chainCount = -1;
        public int chainAddPerLevel = 0;

        // 다크오브(분열/폭발)
        public int splitCount = -1;
        public int splitAddPerLevel = 0;

        public float explosionRadius = -1f;
        public float explosionRadiusAddPerLevel = 0f;

        public float explodeDistance = -1f;
        public float explodeDistanceAddPerLevel = 0f;

        public float childSpeed = -1f;
        public float childSpeedAddPerLevel = 0f;

        // 발시 슬로우
        public float slowRate = -1f;
        public float slowRateAddPerLevel = 0f;

        public float slowSeconds = -1f;
        public float slowSecondsAddPerLevel = 0f;

        // =========================
        // HasXXX (덮어쓰기 판단)
        // =========================
        public bool HasId() => !string.IsNullOrEmpty(id);

        public bool HasDamage() => damage >= 0;
        public bool HasCooldown() => cooldown >= 0f;
        public bool HasSpeed() => speed >= 0f;
        public bool HasLife() => life >= 0f;
        public bool HasCount() => count >= 0;

        public bool HasHitInterval() => hitInterval >= 0f;
        public bool HasOrbitRadius() => orbitRadius >= 0f;
        public bool HasOrbitSpeed() => orbitSpeed >= 0f;
        public bool HasActive() => active >= 0f;

        public bool HasBurstInterval() => burstInterval >= 0f;
        public bool HasSpinDps() => spinDps >= 0f;

        public bool HasBounceCount() => bounceCount >= 0;
        public bool HasChainCount() => chainCount >= 0;
        public bool HasSplitCount() => splitCount >= 0;

        public bool HasExplosionRadius() => explosionRadius >= 0f;
        public bool HasExplodeDistance() => explodeDistance >= 0f;
        public bool HasChildSpeed() => childSpeed >= 0f;

        public bool HasSlowRate() => slowRate >= 0f;
        public bool HasSlowSeconds() => slowSeconds >= 0f;
    }
}