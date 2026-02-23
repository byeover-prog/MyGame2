using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 템플릿(자식)")]
    [SerializeField] private OrbitingBladeHitbox2D bladeTemplate;

    [Header("안전 캡(프로토타입용)")]
    [Tooltip("회전 반경이 너무 작으면 시각적으로 답답하고 근접 히트가 과도해집니다.")]
    [SerializeField] private float minOrbitRadius = 2.0f;

    [Tooltip("히트 간격이 너무 짧으면 접촉형이 폭발적으로 강해집니다.")]
    [SerializeField] private float minHitInterval = 0.20f;

    [Tooltip("밸런스 사고 방지용 데미지 상한(레벨 데이터가 잘못 들어왔을 때만 의미 있음)")]
    [SerializeField] private int maxDamagePerHit = 50;

    private readonly List<OrbitingBladeHitbox2D> blades = new List<OrbitingBladeHitbox2D>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);

    private float baseAngle;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        EnsureBladeInstances();
        OnLevelChanged();
    }

    private void LateUpdate()
    {
        if (owner == null) return;
        transform.position = owner.position;
    }

    protected override void OnLevelChanged()
    {
        EnsureBladeInstances();

        int activeCount = Mathf.Max(1, P.projectileCount);

        for (int i = 0; i < blades.Count; i++)
            blades[i].gameObject.SetActive(i < activeCount);

        lastHitTime.Clear();
    }

    private void Update()
    {
        if (owner == null || config == null) return;

        int activeCount = Mathf.Max(1, P.projectileCount);
        float radius = Mathf.Max(minOrbitRadius, P.orbitRadius);
        float angSpeed = P.orbitAngularSpeed;
        baseAngle = (baseAngle + angSpeed * Time.deltaTime) % 360f;

        float step = 360f / activeCount;

        for (int i = 0; i < activeCount && i < blades.Count; i++)
        {
            float a = (baseAngle + step * i) * Mathf.Deg2Rad;
            Vector3 local = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            blades[i].transform.localPosition = local;
        }
    }

    private void EnsureBladeInstances()
    {
        if (bladeTemplate == null) return;

        int want = Mathf.Max(1, config != null ? config.maxLevel : 1);
        while (blades.Count < want)
        {
            OrbitingBladeHitbox2D b = Instantiate(bladeTemplate, bladeTemplate.transform.parent);
            b.gameObject.SetActive(false);
            b.BindOwner(this);
            blades.Add(b);
        }

        if (bladeTemplate.gameObject.activeSelf)
            bladeTemplate.gameObject.SetActive(false);
    }

    internal void TryHit(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        float now = Time.time;

        // 접촉형은 “적별 히트 쿨다운”이 없으면 DPS 폭발
        float interval = Mathf.Max(minHitInterval, P.hitInterval);

        if (lastHitTime.TryGetValue(id, out float t))
        {
            if (now - t < interval)
                return;
        }

        lastHitTime[id] = now;

        int dmg = Mathf.Clamp(P.damage, 1, Mathf.Max(1, maxDamagePerHit));
        DamageUtil2D.TryApplyDamage(other, dmg);
    }
}