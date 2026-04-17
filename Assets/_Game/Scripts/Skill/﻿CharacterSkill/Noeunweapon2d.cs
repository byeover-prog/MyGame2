using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 뇌운입니다.
/// 가장 가까운 적 위치에 고정 구름을 소환하고, 구름이 범위 틱 피해를 줍니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class NoeunWeapon2D : CharacterSkillWeaponBase
{
    [Header("뇌운 설정")]
    [Tooltip("구름 풀입니다.")]
    [SerializeField] private ProjectilePool2D cloudPool;

    [Tooltip("1레벨 기준 구름 반경입니다. (기획: 1.0)")]
    [SerializeField] private float cloudRadius = 1.0f;

    [Tooltip("구름이 피해를 주는 간격입니다. (기획: 0.5초)")]
    [SerializeField] private float cloudTickInterval = 0.5f;

    [Tooltip("구름 지속 시간입니다.")]
    [SerializeField] private float baseDuration = 4f;

    [Tooltip("기본 동시 활성 구름 수입니다.")]
    [SerializeField] private int baseCloudCount = 1;

    [Tooltip("구름 위치를 적 중심보다 살짝 위로 띄웁니다.")]
    [SerializeField] private float cloudYOffset = 1.1f;

    [Tooltip("레벨당 피해 증가 비율입니다. (기획: +10%)")]
    [SerializeField] private float damagePerLevel = 0.10f;

    [Tooltip("레벨당 범위 증가 비율입니다. (기획: +10%)")]
    [SerializeField] private float radiusPerLevel = 0.10f;

    [Header("각성")]
    [Tooltip("이 레벨부터 추가 구름 수를 적용합니다.")]
    [SerializeField] private int awakeningLevel = 7;

    [Tooltip("각성 시 증가하는 동시 활성 구름 수입니다.")]
    [SerializeField] private int awakeningExtraCount = 2;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<NoeunCloudArea2D> _activeClouds = new List<NoeunCloudArea2D>(8);

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Electric;
        baseDamage = 5;
        baseCooldown = 4.0f;

        if (cloudPool == null)
            cloudPool = GetComponentInChildren<ProjectilePool2D>(true);
    }

    protected override void OnOwnerBound()
    {
        CleanupCloudList();
    }

    private void Update()
    {
        if (owner == null) return;
        if (cloudPool == null) return;

        CleanupCloudList();

        if (_activeClouds.Count >= GetMaxCloudCount())
            return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (!TryGetNearestEnemy(out EnemyRegistryMember2D target) || target == null)
            return;

        SpawnCloud(target);
        cooldownTimer = ScaleCooldown(baseCooldown, 0.1f);
    }

    private void SpawnCloud(EnemyRegistryMember2D target)
    {
        Vector3 spawnPos = target.Transform != null
            ? target.Transform.position + Vector3.up * cloudYOffset
            : (Vector3)target.Position + Vector3.up * cloudYOffset;

        NoeunCloudArea2D cloud = cloudPool.Get<NoeunCloudArea2D>(spawnPos, Quaternion.identity);
        if (cloud == null)
            return;

        cloud.BindReturnCallback(HandleCloudReturned);
        cloud.Init(
            enemyMask: enemyMask,
            damageElement: element,
            tickDamage: GetCloudDamage(),
            tickInterval: cloudTickInterval,
            radius: GetCloudRadius(),
            duration: baseDuration,
            startPosition: spawnPos,
            enableLog: debugLog
        );

        _activeClouds.Add(cloud);

        if (debugLog)
            CombatLog.Log($"[뇌운] 구름 소환 | active={_activeClouds.Count}/{GetMaxCloudCount()}", this);
    }

    private void HandleCloudReturned(NoeunCloudArea2D returnedCloud)
    {
        for (int i = _activeClouds.Count - 1; i >= 0; i--)
        {
            if (_activeClouds[i] == null || _activeClouds[i] == returnedCloud)
                _activeClouds.RemoveAt(i);
        }
    }

    private void CleanupCloudList()
    {
        for (int i = _activeClouds.Count - 1; i >= 0; i--)
        {
            NoeunCloudArea2D cloud = _activeClouds[i];
            if (cloud == null || !cloud.gameObject.activeInHierarchy)
                _activeClouds.RemoveAt(i);
        }
    }

    private int GetCloudDamage()
    {
        float damage = baseDamage * (1f + damagePerLevel * Mathf.Max(0, level - 1));
        return ScaleDamage(damage);
    }

    private float GetCloudRadius()
    {
        float radius = cloudRadius * (1f + radiusPerLevel * Mathf.Max(0, level - 1));
        return ScaleRadius(radius, 0.2f);
    }

    private int GetMaxCloudCount()
    {
        int count = baseCloudCount;
        if (level >= awakeningLevel)
            count += awakeningExtraCount;

        return Mathf.Max(1, count);
    }
}