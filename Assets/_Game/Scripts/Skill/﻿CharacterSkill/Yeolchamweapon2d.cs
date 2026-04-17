using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 열참입니다.
/// 플레이어 중심 도넛 판정으로 외곽 적중 시 추가 피해와 출혈을 부여합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class YeolchamWeapon2D : CharacterSkillWeaponBase
{
    [Header("열참 설정")]
    [Tooltip("내부 반경입니다. 이 안은 일반 피해입니다.")]
    [SerializeField] private float innerRadius = 1.2f;

    [Tooltip("외부 반경입니다. 이 안쪽까지 전체 판정이 들어갑니다.")]
    [SerializeField] private float outerRadius = 2.6f;

    [Tooltip("레벨당 피해 증가 비율입니다. (기획: +15%)")]
    [SerializeField] private float damagePerLevel = 0.15f;

    [Tooltip("외곽 적중 시 추가 피해 배율입니다.")]
    [SerializeField] private float outerBonusMultiplier = 1.5f;

    [Header("출혈")]
    [Tooltip("출혈 지속 시간입니다.")]
    [SerializeField] private float bleedDuration = 3f;

    [Tooltip("출혈 틱 피해 비율입니다. 기본 피해 기준 (0.10 = 10%).")]
    [SerializeField] private float bleedTickDamageRate = 0.10f;

    [Header("비주얼")]
    [Tooltip("SpriteSkillVisual 기준 반경입니다.")]
    [SerializeField] private float visualBaseRadius = 1f;

    [Tooltip("임팩트 후 비주얼을 숨기기까지의 시간입니다.")]
    [SerializeField] private float impactVisibleTime = 0.15f;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(32);
    private float _impactTimer;

    protected override void Awake()
    {
        base.Awake();
        element = DamageElement2D.Dark; // 하린 속성 = 음
        baseDamage = 15;
        baseCooldown = 3f;
    }

    protected override void OnOwnerBound()
    {
        UpdateVisualScaleOnly();
    }

    protected override void OnLevelApplied()
    {
        UpdateVisualScaleOnly();
    }

    private void Update()
    {
        if (owner == null) return;

        if (_impactTimer > 0f)
        {
            _impactTimer -= Time.deltaTime;

            if (visual != null)
            {
                visual.UpdatePosition(owner.position);
                visual.UpdateScale(GetOuterRadius() / Mathf.Max(0.01f, visualBaseRadius));
            }

            if (_impactTimer <= 0f && visual != null)
                visual.Stop();
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        Cast();
        cooldownTimer = ScaleCooldown(baseCooldown, 0.1f);
    }

    private void Cast()
    {
        Vector2 center = owner.position;
        int total = CollectEnemiesInRadius(center, GetOuterRadius(), _targets);
        if (total <= 0)
            return;

        int baseHitDamage = GetBaseHitDamage();
        int outerHitDamage = Mathf.Max(1, Mathf.RoundToInt(baseHitDamage * outerBonusMultiplier));
        int bleedTickDamage = Mathf.Max(1, Mathf.RoundToInt(baseHitDamage * bleedTickDamageRate));

        int hitCount = 0;
        float innerR = GetInnerRadius();

        // 메모리 낭비 방지: _targets는 베이스 클래스가 사용할 수 있으므로 복사본 순회
        int count = _targets.Count;
        for (int i = 0; i < count; i++)
        {
            EnemyRegistryMember2D enemy = _targets[i];
            if (enemy == null) continue;

            float distance = Vector2.Distance(center, enemy.Position);
            bool isOuterHit = distance >= innerR;

            int damage = isOuterHit ? outerHitDamage : baseHitDamage;
            bool applied = TryApplyDamageToEnemy(enemy, damage);
            if (!applied) continue;

            hitCount++;

            if (isOuterHit)
            {
                // IStatusReceiver 경유 출혈 적용 (Reflection 없음)
                // EnemyBleedStatus2D가 IStatusReceiver를 구현하면 자동 동작
                ApplyStatus(enemy, StatusEffectInfo.Bleed(bleedDuration, bleedTickDamage));
            }
        }

        _targets.Clear();

        if (visual != null)
        {
            visual.Play(owner.position);
            visual.UpdatePosition(owner.position);
            visual.UpdateScale(GetOuterRadius() / Mathf.Max(0.01f, visualBaseRadius));
            visual.PlayImpact(owner.position);
            _impactTimer = impactVisibleTime;
        }

        if (debugLog && hitCount > 0)
            CombatLog.Log($"[열참] 적중 {hitCount}명 | base={baseHitDamage} outer={outerHitDamage}", this);
    }

    private int GetBaseHitDamage()
    {
        float damage = baseDamage * (1f + damagePerLevel * Mathf.Max(0, level - 1));
        return ScaleDamage(damage);
    }

    private float GetInnerRadius()
    {
        return ScaleRadius(innerRadius, 0.1f);
    }

    private float GetOuterRadius()
    {
        return ScaleRadius(outerRadius, 0.2f);
    }

    private void UpdateVisualScaleOnly()
    {
        if (visual == null || owner == null) return;

        visual.Play(owner.position);
        visual.UpdatePosition(owner.position);
        visual.UpdateScale(GetOuterRadius() / Mathf.Max(0.01f, visualBaseRadius));
        visual.Stop();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 c = owner != null ? owner.position : transform.position;

        // 외곽 원 (흰색)
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(c, outerRadius);

        // 내곽 원 (회색)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        Gizmos.DrawWireSphere(c, innerRadius);
    }
#endif
}