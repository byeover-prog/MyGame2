using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class CharacterSkillWeaponBase : MonoBehaviour, ILevelableSkill
{
    [Header("공통 설정")]
    [Tooltip("이 스킬의 원본 SO입니다. 비워두면 기존 JSON 또는 fallback 수치를 사용합니다.")]
    [SerializeField] protected CharacterSkillDefinitionSO skillDefinition;

    [Tooltip("적 레이어 마스크입니다.")]
    [SerializeField] protected LayerMask enemyMask;

    [Tooltip("이 스킬이 사용하는 속성입니다. SO가 있으면 SO의 속성으로 덮어씁니다.")]
    [SerializeField] protected DamageElement2D element = DamageElement2D.Physical;

    [Header("비주얼")]
    [Tooltip("ISkillVisual 구현체가 붙은 자식 컴포넌트입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] protected Component visualBehaviour;

    [Header("밸런스 기본값 / Fallback")]
    [Tooltip("1레벨 기준 피해량입니다. SO 또는 skill_balance.json 값이 있으면 덮어씁니다.")]
    [SerializeField] protected int baseDamage = 15;

    [Tooltip("1레벨 기준 쿨다운입니다. SO 또는 skill_balance.json 값이 있으면 덮어씁니다.")]
    [SerializeField] protected float baseCooldown = 1.0f;

    [Tooltip("skill_balance.json에서 찾을 ID입니다. 비우면 SO의 SkillId를 사용합니다.")]
    [SerializeField] protected string balanceId;

    [Header("진단")]
    [Tooltip("SO/JSON 밸런스 적용 상태를 로그로 출력합니다.")]
    [SerializeField] protected bool balanceDebugLog = false;

    protected Transform owner;
    protected int level = 1;
    protected float cooldownTimer;
    protected ISkillVisual visual;
    protected PlayerCombatStats2D ownerStats;
    protected PlayerHealth ownerHealth;

    // SO 밸런스 캐시
    protected SkillLevelBalanceData2D soBalance;
    protected bool soBalanceApplied;

    // JSON 밸런스 캐시 — ★ 타입 SkillBalanceDB2D.SkillRow2D (서비스 시그니처 기준)
    protected SkillBalanceDB2D.SkillRow2D jsonRow;
    protected int jsonDamage = -1;
    protected float jsonCooldown = -1f;
    protected bool jsonBalanceApplied;

    protected virtual void Awake()
    {
        ResolveVisual();
        SyncDefinitionBasicInfo();
    }

    protected virtual void OnDisable()
    {
        if (visual != null)
            visual.Stop();
    }

    public virtual void OnAttached(Transform newOwner)
    {
        owner = newOwner;
        cooldownTimer = 0f;
        CacheOwnerRefs();
        ResolveVisual();
        RefreshBalanceCache();
        OnOwnerBound();
    }

    /// <summary>
    /// 인터페이스 오타(OnAttaced) 대응. 둘 다 유지.
    /// </summary>
    public virtual void OnAttaced(Transform newOwner)
    {
        OnAttached(newOwner);
    }

    public virtual void ApplyLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);
        RefreshBalanceCache();
        OnLevelApplied();
    }

    protected virtual void OnOwnerBound() { }
    protected virtual void OnLevelApplied() { }

    protected void RefreshBalanceCache()
    {
        SyncDefinitionBasicInfo();
        TryLoadSoBalance();
        TryLoadJsonBalance();

        if (balanceDebugLog)
        {
            string soState = soBalanceApplied ? "SO 적용" : "SO 없음";
            string jsonState = jsonBalanceApplied ? "JSON 적용" : "JSON 없음";
            Debug.Log($"[전용 스킬 밸런스] {name} | id={GetBalanceId()} | lv={level} | {soState} | {jsonState}", this);
        }
    }

    private void SyncDefinitionBasicInfo()
    {
        if (skillDefinition == null)
            return;

        if (string.IsNullOrWhiteSpace(balanceId))
            balanceId = skillDefinition.SkillId;

        element = skillDefinition.Element;
    }

    private void TryLoadSoBalance()
    {
        soBalance = null;
        soBalanceApplied = false;

        if (skillDefinition == null)
            return;

        soBalance = skillDefinition.GetLevelBalance(level);
        soBalanceApplied = soBalance != null;
    }

    private void TryLoadJsonBalance()
    {
        jsonBalanceApplied = false;
        jsonRow = null;
        jsonDamage = -1;
        jsonCooldown = -1f;

        string id = GetBalanceId();
        if (string.IsNullOrWhiteSpace(id)) return;
        if (!SkillBalanceService2D.IsLoaded) return;

        if (!SkillBalanceService2D.TryGet(id, out var row) || row == null)
            return;

        jsonRow = row;

        int lvMinus1 = Mathf.Max(0, level - 1);

        if (row.damage >= 0)
        {
            int final = row.damage + row.damageAddPerLevel * lvMinus1;
            jsonDamage = Mathf.Max(0, final);
        }

        if (row.cooldown >= 0f)
        {
            float final = row.cooldown + row.cooldownAddPerLevel * lvMinus1;
            jsonCooldown = Mathf.Max(0.01f, final);
        }

        jsonBalanceApplied = (jsonDamage >= 0) || (jsonCooldown >= 0f);
    }

    protected string GetBalanceId()
    {
        if (!string.IsNullOrWhiteSpace(balanceId))
            return balanceId;

        if (skillDefinition != null)
            return skillDefinition.SkillId;

        return string.Empty;
    }

    /// <summary>
    /// 1순위: SO  /  2순위: JSON  /  3순위: baseDamage (fallback)
    /// </summary>
    protected int GetBalanceDamage()
    {
        if (TryGetBalanceInt("damage", out int soOrJsonDamage))
            return soOrJsonDamage;

        if (jsonDamage >= 0)
            return jsonDamage;

        return baseDamage;
    }

    /// <summary>
    /// 1순위: SO  /  2순위: JSON  /  3순위: baseCooldown (fallback)
    /// </summary>
    protected float GetBalanceCooldown()
    {
        if (TryGetBalanceFloat("cooldown", out float soOrJsonCooldown))
            return Mathf.Max(0.01f, soOrJsonCooldown);

        if (jsonCooldown >= 0f)
            return jsonCooldown;

        return baseCooldown;
    }

    protected float GetBalanceFloat(string key, float fallback)
    {
        if (TryGetBalanceFloat(key, out float value))
            return value;

        return fallback;
    }

    protected int GetBalanceInt(string key, int fallback)
    {
        if (TryGetBalanceInt(key, out int value))
            return value;

        return fallback;
    }

    protected bool GetBalanceBool(string key, bool fallback)
    {
        if (soBalance != null && soBalance.TryGetBool(key, out bool value))
            return value;

        return fallback;
    }

    protected string GetBalanceString(string key, string fallback)
    {
        if (soBalance != null && soBalance.TryGetString(key, out string value))
            return value;

        return fallback;
    }

    protected bool TryGetBalanceFloat(string key, out float value)
    {
        value = 0f;

        if (soBalance != null && soBalance.TryGetFloat(key, out value))
            return true;

        if (TryGetJsonFloat(key, out value))
            return true;

        return false;
    }

    protected bool TryGetBalanceInt(string key, out int value)
    {
        value = 0;

        if (soBalance != null && soBalance.TryGetInt(key, out value))
            return true;

        if (TryGetJsonInt(key, out value))
            return true;

        return false;
    }

    private bool TryGetJsonFloat(string key, out float value)
    {
        value = 0f;

        if (jsonRow == null)
            return false;

        int lvMinus1 = Mathf.Max(0, level - 1);
        string normalized = NormalizeKey(key);

        switch (normalized)
        {
            case "cooldown":
                return TryUseJsonFloat(jsonRow.cooldown, jsonRow.cooldownAddPerLevel, lvMinus1, out value);

            case "speed":
            case "projectilespeed":
            case "arrowspeed":
                return TryUseJsonFloat(jsonRow.speed, jsonRow.speedAddPerLevel, lvMinus1, out value);

            case "life":
            case "lifetime":
            case "projectilelifetime":
            case "arrowmaxflighttime":
                return TryUseJsonFloat(jsonRow.life, jsonRow.lifeAddPerLevel, lvMinus1, out value);

            case "hitinterval":
            case "interval":
            case "boltinterval":
                return TryUseJsonFloat(jsonRow.hitInterval, jsonRow.hitIntervalAddPerLevel, lvMinus1, out value);

            case "orbitradius":
                return TryUseJsonFloat(jsonRow.orbitRadius, jsonRow.orbitRadiusAddPerLevel, lvMinus1, out value);

            case "orbitspeed":
                return TryUseJsonFloat(jsonRow.orbitSpeed, jsonRow.orbitSpeedAddPerLevel, lvMinus1, out value);

            case "active":
                return TryUseJsonFloat(jsonRow.active, jsonRow.activeAddPerLevel, lvMinus1, out value);

            case "burstinterval":
                return TryUseJsonFloat(jsonRow.burstInterval, jsonRow.burstIntervalAddPerLevel, lvMinus1, out value);

            case "spindps":
                return TryUseJsonFloat(jsonRow.spinDps, jsonRow.spinDpsAddPerLevel, lvMinus1, out value);

            case "radius":
            case "explosionradius":
            case "hitradius":
            case "boltradius":
                return TryUseJsonFloat(jsonRow.explosionRadius, jsonRow.explosionRadiusAddPerLevel, lvMinus1, out value);

            case "explodedistance":
                return TryUseJsonFloat(jsonRow.explodeDistance, jsonRow.explodeDistanceAddPerLevel, lvMinus1, out value);

            case "childspeed":
                return TryUseJsonFloat(jsonRow.childSpeed, jsonRow.childSpeedAddPerLevel, lvMinus1, out value);

            case "slowrate":
            case "frostslowmultiplier":
                return TryUseJsonFloat(jsonRow.slowRate, jsonRow.slowRateAddPerLevel, lvMinus1, out value);

            case "slowseconds":
            case "slowduration":
            case "frostduration":
            case "duration":
                return TryUseJsonFloat(jsonRow.slowSeconds, jsonRow.slowSecondsAddPerLevel, lvMinus1, out value);
        }

        return false;
    }

    private bool TryGetJsonInt(string key, out int value)
    {
        value = 0;

        if (jsonRow == null)
            return false;

        int lvMinus1 = Mathf.Max(0, level - 1);
        string normalized = NormalizeKey(key);

        switch (normalized)
        {
            case "damage":
                return TryUseJsonInt(jsonRow.damage, jsonRow.damageAddPerLevel, lvMinus1, out value);

            case "count":
            case "castcount":
            case "shotcount":
            case "projectilecount":
                return TryUseJsonInt(jsonRow.count, jsonRow.countAddPerLevel, lvMinus1, out value);

            case "bouncecount":
                return TryUseJsonInt(jsonRow.bounceCount, jsonRow.bounceAddPerLevel, lvMinus1, out value);

            case "chaincount":
                return TryUseJsonInt(jsonRow.chainCount, jsonRow.chainAddPerLevel, lvMinus1, out value);

            case "splitcount":
                return TryUseJsonInt(jsonRow.splitCount, jsonRow.splitAddPerLevel, lvMinus1, out value);
        }

        return false;
    }

    private static bool TryUseJsonFloat(float baseValue, float addPerLevel, int lvMinus1, out float value)
    {
        value = 0f;

        if (baseValue < 0f)
            return false;

        value = baseValue + addPerLevel * lvMinus1;
        return true;
    }

    private static bool TryUseJsonInt(int baseValue, int addPerLevel, int lvMinus1, out int value)
    {
        value = 0;

        if (baseValue < 0)
            return false;

        value = baseValue + addPerLevel * lvMinus1;
        return true;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return key
            .Trim()
            .Replace("_", "")
            .Replace("-", "")
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    protected void CacheOwnerRefs()
    {
        if (owner == null)
        {
            ownerStats = null;
            ownerHealth = null;
            return;
        }

        ownerStats = owner.GetComponent<PlayerCombatStats2D>();
        if (ownerStats == null)
            ownerStats = owner.GetComponentInParent<PlayerCombatStats2D>();

        ownerHealth = owner.GetComponent<PlayerHealth>();
        if (ownerHealth == null)
            ownerHealth = owner.GetComponentInParent<PlayerHealth>();
    }

    protected void ResolveVisual()
    {
        if (visualBehaviour is ISkillVisual visualComponent)
        {
            visual = visualComponent;
            return;
        }

        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISkillVisual found)
            {
                visual = found;
                visualBehaviour = behaviours[i];
                return;
            }
        }

        visual = null;
    }

    protected float DamageMul => ownerStats != null ? Mathf.Max(0.01f, ownerStats.DamageMul) : 1f;
    protected float CooldownMul => ownerStats != null ? Mathf.Max(0.01f, ownerStats.CooldownMul) : 1f;
    protected float AreaMul => ownerStats != null ? Mathf.Max(0.01f, ownerStats.AreaMul) : 1f;

    protected int ScaleDamage(float levelScaledDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(levelScaledDamage * DamageMul));
    }

    protected float ScaleCooldown(float levelScaledCooldown, float minimum = 0.05f)
    {
        return Mathf.Max(minimum, levelScaledCooldown * CooldownMul);
    }

    protected float ScaleRadius(float levelScaledRadius, float minimum = 0.05f)
    {
        return Mathf.Max(minimum, levelScaledRadius * AreaMul);
    }

    protected bool TryGetNearestEnemy(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetNearest(owner.position, out enemy);
    }

    protected bool TryGetNearestEnemy(float maxDistance, out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetNearest(owner.position, maxDistance, out enemy);
    }

    protected bool TryGetFarthestEnemy(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetFarthest(owner.position, out enemy);
    }

    protected int CollectEnemiesInRadius(Vector2 center, float radius, List<EnemyRegistryMember2D> results)
    {
        if (results == null) return 0;
        results.Clear();

        float sqrRadius = radius * radius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;

        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D enemy = members[i];
            if (enemy == null || !enemy.IsValidTarget) continue;

            Vector2 delta = enemy.Position - center;
            if (delta.sqrMagnitude > sqrRadius) continue;

            results.Add(enemy);
        }

        return results.Count;
    }

    protected int PickRandomEnemies(int count, List<EnemyRegistryMember2D> results, List<EnemyRegistryMember2D> scratch)
    {
        if (results == null || scratch == null) return 0;
        results.Clear();
        scratch.Clear();
        if (count <= 0) return 0;

        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;
        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D enemy = members[i];
            if (enemy == null || !enemy.IsValidTarget) continue;
            scratch.Add(enemy);
        }

        int available = scratch.Count;
        if (available <= 0) return 0;

        int takeCount = Mathf.Min(count, available);
        for (int i = 0; i < takeCount; i++)
        {
            int pickIndex = Random.Range(i, available);
            (scratch[i], scratch[pickIndex]) = (scratch[pickIndex], scratch[i]);
            results.Add(scratch[i]);
        }

        scratch.Clear();
        return results.Count;
    }

    protected bool TryApplyDamageToEnemy(EnemyRegistryMember2D enemy, int damage)
    {
        if (enemy == null || !enemy.IsValidTarget) return false;
        if (damage <= 0) return false;

        return DamageUtil2D.TryApplyDamage(enemy.gameObject, damage, element);
    }

    protected bool TryApplyDamageToCollider(Collider2D col, int damage)
    {
        if (col == null) return false;
        if (damage <= 0) return false;

        return DamageUtil2D.TryApplyDamage(col, damage, element);
    }

    protected void ApplyStatus(EnemyRegistryMember2D enemy, StatusEffectInfo info)
    {
        if (enemy == null) return;

        IStatusReceiver[] receivers = enemy.GetComponentsInChildren<IStatusReceiver>(true);
        if (receivers == null || receivers.Length == 0) return;

        for (int i = 0; i < receivers.Length; i++)
        {
            if (receivers[i] == null) continue;
            receivers[i].TryApplyStatus(info);
        }
    }
}