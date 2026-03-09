using System;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - skill_balance.json 포맷(version + skills[])을 그대로 받는 루트.
/// - 이 파일은 "순수 데이터 모델"이며 컴포넌트가 아니다.
/// </summary>
[Serializable]
public sealed class SkillBalanceJsonRoot2D
{
    public int version = 1;
    public SkillBalanceSkill2D[] skills;
}

[Serializable]
public sealed class SkillBalanceSkill2D
{
    [Header("키")]
    [Tooltip("스킬 ID (예: weapon_bullet, weapon_boomerang)")]
    public string id;

    public int damage;
    public int damageAddPerLevel;

    public float cooldown;
    public float cooldownAddPerLevel;

    public float speed;
    public float speedAddPerLevel;

    public float life;
    public float lifeAddPerLevel;

    public int count;
    public int countAddPerLevel;

    public float hitInterval;
    public float hitIntervalAddPerLevel;

    public float orbitRadius;
    public float orbitRadiusAddPerLevel;

    public float orbitSpeed;
    public float orbitSpeedAddPerLevel;

    public float active;
    public float activeAddPerLevel;

    public float burstInterval;
    public float burstIntervalAddPerLevel;

    public float spinDps;
    public float spinDpsAddPerLevel;

    public int bounceCount;
    public int bounceAddPerLevel;

    public int chainCount;
    public int chainAddPerLevel;

    public int splitCount;
    public int splitAddPerLevel;

    public float explosionRadius;
    public float explosionRadiusAddPerLevel;

    public float explodeDistance;
    public float explodeDistanceAddPerLevel;

    public float childSpeed;
    public float childSpeedAddPerLevel;

    public float slowRate;
    public float slowRateAddPerLevel;

    public float slowSeconds;
    public float slowSecondsAddPerLevel;
}

public readonly struct SkillBalanceResolved2D
{
    public readonly string id;
    public readonly int level;

    public readonly int damage;
    public readonly float cooldown;
    public readonly float speed;
    public readonly float life;
    public readonly int count;
    public readonly float hitInterval;

    public readonly float orbitRadius;
    public readonly float orbitSpeed;

    public readonly float active;
    public readonly float burstInterval;
    public readonly float spinDps;

    public readonly int bounceCount;
    public readonly int chainCount;
    public readonly int splitCount;

    public readonly float explosionRadius;
    public readonly float explodeDistance;
    public readonly float childSpeed;

    public readonly float slowRate;
    public readonly float slowSeconds;

    public SkillBalanceResolved2D(
        string id, int level,
        int damage, float cooldown, float speed, float life,
        int count, float hitInterval,
        float orbitRadius, float orbitSpeed,
        float active, float burstInterval, float spinDps,
        int bounceCount, int chainCount, int splitCount,
        float explosionRadius, float explodeDistance, float childSpeed,
        float slowRate, float slowSeconds)
    {
        this.id = id;
        this.level = level;

        this.damage = damage;
        this.cooldown = cooldown;
        this.speed = speed;
        this.life = life;

        this.count = count;
        this.hitInterval = hitInterval;

        this.orbitRadius = orbitRadius;
        this.orbitSpeed = orbitSpeed;

        this.active = active;
        this.burstInterval = burstInterval;
        this.spinDps = spinDps;

        this.bounceCount = bounceCount;
        this.chainCount = chainCount;
        this.splitCount = splitCount;

        this.explosionRadius = explosionRadius;
        this.explodeDistance = explodeDistance;
        this.childSpeed = childSpeed;

        this.slowRate = slowRate;
        this.slowSeconds = slowSeconds;
    }
}