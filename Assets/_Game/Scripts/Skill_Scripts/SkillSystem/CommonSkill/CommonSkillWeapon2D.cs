using System;
using System.Collections;
using UnityEngine;

public abstract class CommonSkillWeapon2D : MonoBehaviour, ILevelableSkill
{
    [Header("공통")]
    [SerializeField] protected CommonSkillConfigSO config;
    [SerializeField] protected LayerMask enemyMask;
    [SerializeField] protected bool requireTargetToFire = true;

    [Header("겹침 방지(추천)")]
    [Tooltip("발사 시작 위치를 플레이어 중심에서 살짝 분산한다(유닛). 0이면 분산 없음.")]
    [Min(0f)]
    [SerializeField] private float spawnOffsetRadius = 0.25f;

    [Tooltip("발사 타이밍을 살짝 흔들어서(초) 동시에 나가도 겹쳐 보이지 않게 한다.")]
    [Min(0f)]
    [SerializeField] private float fireJitterDelayMin = 0.05f;

    [Min(0f)]
    [SerializeField] private float fireJitterDelayMax = 0.10f;

    [Header("겹침 방지(정렬)")]
    [Tooltip("투사체 SpriteRenderer sortingOrder 기본값. 무기 종류별로 여기에 step이 더해진다.")]
    [SerializeField] private int projectileSortingBase = 0;

    [Tooltip("무기 종류(CommonSkillKind)마다 sortingOrder를 얼마나 벌릴지.")]
    [Min(0)]
    [SerializeField] private int projectileSortingStepPerKind = 10;

    protected Transform owner;
    protected int level = 1;
    protected float cooldownTimer;

    private bool _firePending;

    public CommonSkillKind Kind => config != null ? config.kind : 0;
    public int Level => level;

    public virtual void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        config = cfg;
        owner = ownerTr;
        level = Mathf.Max(1, startLevel);
        cooldownTimer = 0f;
        _firePending = false;
        OnLevelChanged();
    }

    public void SetOwner(Transform ownerTr)
    {
        owner = ownerTr;
    }

    public void SetLevel(int newLevel)
    {
        int lv = Mathf.Max(1, newLevel);
        if (lv == level) return;
        level = lv;
        OnLevelChanged();
    }

    protected CommonSkillLevelParams P => (config != null) ? config.GetLevelParams(level) : default;

    protected bool TryGetNearest(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetNearest(owner.position, out enemy);
    }

    protected bool TryGetFarthest(out EnemyRegistryMember2D enemy)
    {
        if (owner == null) { enemy = null; return false; }
        return EnemyRegistry2D.TryGetFarthest(owner.position, out enemy);
    }

    protected virtual void OnLevelChanged() { }

    protected Vector2 GetSpawnOrigin(Transform spawnPointOrNull)
    {
        Vector2 origin = owner != null ? (Vector2)owner.position : (Vector2)transform.position;
        if (spawnPointOrNull != null) origin = spawnPointOrNull.position;

        float r = Mathf.Max(0f, spawnOffsetRadius);
        if (r <= 0f) return origin;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * r;
        return origin + offset;
    }

    protected float GetFireJitterDelay()
    {
        float a = Mathf.Max(0f, fireJitterDelayMin);
        float b = Mathf.Max(0f, fireJitterDelayMax);
        if (b < a) b = a;
        if (b <= 0f) return 0f;
        return UnityEngine.Random.Range(a, b);
    }

    protected int GetProjectileSortingOrder()
    {
        int step = Mathf.Max(0, projectileSortingStepPerKind);
        return projectileSortingBase + ((int)Kind * step);
    }

    protected void ApplyProjectileSorting(GameObject projectileRoot)
    {
        if (projectileRoot == null) return;

        int order = GetProjectileSortingOrder();
        var srs = projectileRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < srs.Length; i++)
        {
            if (srs[i] == null) continue;
            srs[i].sortingOrder = order;
        }
    }

    // 기존 무기 스크립트 호환용(컴파일 에러 방지)
    protected bool TryBeginFire(Action fireAction)
    {
        return TryBeginFireConsumeCooldown(fireAction);
    }

    /// <summary>
    /// 발사(지터 포함) + 쿨다운 소비를 "실제 발사 시점"에 맞춰서 처리한다.
    /// - 지터(지연) 중에는 쿨다운을 절대 리셋하지 않는다.
    /// - 그래서 2발 연속/발사 간격 붕괴를 구조적으로 차단한다.
    /// </summary>
    protected bool TryBeginFireConsumeCooldown(Action fireAction)
    {
        if (fireAction == null) return false;
        if (_firePending) return false;

        float delay = GetFireJitterDelay();
        if (delay <= 0f)
        {
            fireAction.Invoke();
            ConsumeCooldown();
            return true;
        }

        _firePending = true;
        StartCoroutine(FireDelayed(delay, fireAction));
        return true;
    }

    private IEnumerator FireDelayed(float delay, Action fireAction)
    {
        yield return new WaitForSeconds(delay);

        _firePending = false;

        if (this == null || !isActiveAndEnabled) yield break;
        fireAction.Invoke();
        ConsumeCooldown();
    }

    private void ConsumeCooldown()
    {
        cooldownTimer = Mathf.Max(0.01f, P.cooldown);
    }

    // ===== SkillRunner 연동(ILevelableSkill) =====
    public void OnAttached(Transform newOwner)
    {
        // 프리팹 인스펙터에 config가 연결돼 있다는 가정.
        if (config == null)
            Debug.LogWarning($"[CommonSkillWeapon2D] config가 비어있습니다: {name}", this);

        Initialize(config, newOwner, 1);
    }

    public void ApplyLevel(int newLevel)
    {
        if (newLevel <= 0) return;
        SetLevel(newLevel);
    }
}
