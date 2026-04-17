using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 전용 스킬 공통 베이스입니다.
/// owner / 레벨 / 비주얼 / 플레이어 스탯 접근만 공통으로 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public abstract class CharacterSkillWeaponBase : MonoBehaviour, ILevelableSkill
{
    [Header("공통 설정")]
    [Tooltip("적 레이어 마스크입니다.")]
    [SerializeField] protected LayerMask enemyMask;

    [Tooltip("이 스킬이 사용하는 속성입니다.")]
    [SerializeField] protected DamageElement2D element = DamageElement2D.Physical;

    [Header("비주얼")]
    [Tooltip("ISkillVisual 구현체가 붙은 자식 컴포넌트입니다. 비워두면 자동 탐색합니다.")]
    [SerializeField] protected Component visualBehaviour;

    [Header("밸런스(기본값 / Fallback)")]
    [Tooltip("1레벨 기준 피해량입니다. skill_balance.json이 있으면 덮어씁니다.")]
    [SerializeField] protected int baseDamage = 15;

    [Tooltip("1레벨 기준 쿨다운입니다. skill_balance.json이 있으면 덮어씁니다.")]
    [SerializeField] protected float baseCooldown = 1.0f;

    [Tooltip("skill_balance.json에서 찾을 ID입니다. 비우면 JSON 오버라이드를 사용하지 않습니다.\n" +
             "예: weapon_bingju, weapon_noeun")]
    [SerializeField] protected string balanceId;

    protected Transform owner;
    protected int level = 1;
    protected float cooldownTimer;
    protected ISkillVisual visual;
    protected PlayerCombatStats2D ownerStats;
    protected PlayerHealth ownerHealth;

    // JSON 밸런스에서 읽은 레벨별 최종값. -1이면 미적용(fallback 사용).
    protected int jsonDamage = -1;
    protected float jsonCooldown = -1f;
    protected bool jsonBalanceApplied;

    protected virtual void Awake()
    {
        ResolveVisual();
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
        OnOwnerBound();
    }

    /// <summary>
    /// 인터페이스 오타 대응(OnAttaced). 프로젝트 ILevelableSkill이 이 이름으로 정의되어 있어
    /// 파생 클래스가 어느 쪽을 오버라이드하든 동작하도록 두 메서드 모두 구현합니다.
    /// </summary>
    public virtual void OnAttaced(Transform newOwner)
    {
        OnAttached(newOwner);
    }

    public virtual void ApplyLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        // skill_balance.json 값을 읽어 baseDamage/baseCooldown 오버라이드
        TryLoadJsonBalance();

        OnLevelApplied();
    }

    protected virtual void OnOwnerBound() { }
    protected virtual void OnLevelApplied() { }
    
    // JSON 밸런스 로드
    /// <summary>
    /// skill_balance.json에서 현재 level 기준 최종 damage/cooldown을 계산해
    /// jsonDamage / jsonCooldown에 저장합니다.
    /// balanceId가 비어있거나 서비스가 로드되지 않았으면 fallback(baseDamage/baseCooldown)을 그대로 사용.
    /// </summary>
    private void TryLoadJsonBalance()
    {
        jsonBalanceApplied = false;
        jsonDamage = -1;
        jsonCooldown = -1f;

        if (string.IsNullOrEmpty(balanceId)) return;
        if (!SkillBalanceService2D.IsLoaded) return;

        if (!SkillBalanceService2D.TryGet(balanceId, out var row) || row == null)
            return;

        int lvMinus1 = Mathf.Max(0, level - 1);

        // damage
        if (row.damage >= 0)
        {
            int final = row.damage + row.damageAddPerLevel * lvMinus1;
            jsonDamage = Mathf.Max(0, final);
        }

        // cooldown
        if (row.cooldown >= 0f)
        {
            float final = row.cooldown + row.cooldownAddPerLevel * lvMinus1;
            jsonCooldown = Mathf.Max(0.01f, final);
        }

        jsonBalanceApplied = (jsonDamage >= 0) || (jsonCooldown >= 0f);
    }

    /// <summary>
    /// JSON 우선, 없으면 코드 fallback. 파생 클래스에서 피해 계산 시 이 값을 기준으로 사용.
    /// </summary>
    protected int GetBalanceDamage()
    {
        return jsonDamage >= 0 ? jsonDamage : baseDamage;
    }

    /// <summary>
    /// JSON 우선, 없으면 코드 fallback. 파생 클래스에서 쿨다운 계산 시 이 값을 기준으로 사용.
    /// </summary>
    protected float GetBalanceCooldown()
    {
        return jsonCooldown >= 0f ? jsonCooldown : baseCooldown;
    }
    
    // 내부 유틸

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
    
    // PlayerCombatStats2D 배율 접근자

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
    
    // 적 조회 (EnemyRegistry2D 직접 경유 — O(N), Physics 쿼리 없음)

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

    /// <summary>
    /// 주어진 중심/반경 안의 적들을 results 리스트에 채워 반환합니다.
    /// GC 0을 위해 호출자가 List를 재사용해야 합니다.
    /// </summary>
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

    /// <summary>
    /// 활성화된 적 중 랜덤하게 count명을 고릅니다.
    /// Fisher-Yates 셔플. results/scratch는 호출자가 재사용합니다.
    /// </summary>
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
    
    // 피해 적용 (DamageUtil2D 경유 원칙 준수)
    /// <summary>
    /// 적에게 피해를 적용합니다. 항상 DamageUtil2D를 경유해 속성 시너지/팝업/흡혈 이벤트가 발동합니다.
    /// </summary>
    protected bool TryApplyDamageToEnemy(EnemyRegistryMember2D enemy, int damage)
    {
        if (enemy == null || !enemy.IsValidTarget) return false;
        if (damage <= 0) return false;

        // EnemyRegistryMember2D는 적 루트에 붙어 있으므로 GameObject 버전으로 바로 호출
        return DamageUtil2D.TryApplyDamage(enemy.gameObject, damage, element);
    }

    /// <summary>
    /// Collider 기반 피해 적용(OverlapCircle 결과 등). Collider가 팝업 위치 계산에 활용됩니다.
    /// </summary>
    protected bool TryApplyDamageToCollider(Collider2D col, int damage)
    {
        if (col == null) return false;
        if (damage <= 0) return false;

        return DamageUtil2D.TryApplyDamage(col, damage, element);
    }
    
    /// <summary>
    /// 적이 가진 IStatusReceiver 들에게 상태이상을 브로드캐스트합니다.
    /// </summary>
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