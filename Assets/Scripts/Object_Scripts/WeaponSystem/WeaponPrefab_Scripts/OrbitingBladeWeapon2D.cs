using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrbitingBladeWeapon2D : CommonSkillWeapon2D
{
    [Header("블레이드 템플릿(자식)")]
    [SerializeField] private OrbitingBladeHitbox2D bladeTemplate;

    private readonly List<OrbitingBladeHitbox2D> blades = new List<OrbitingBladeHitbox2D>(16);
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>(256);

    private float baseAngle;

    public override void Initialize(CommonSkillConfigSO cfg, Transform ownerTr, int startLevel)
    {
        base.Initialize(cfg, ownerTr, startLevel);
        EnsureBladeInstances();
        OnLevelChanged();
    }

    protected override void OnLevelChanged()
    {
        // 레벨당 검 개수 증가(기획)
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
        float radius = Mathf.Max(0.1f, P.orbitRadius);
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

        // 원본 템플릿은 비활성 권장(중복 타격 방지)
        if (bladeTemplate.gameObject.activeSelf)
            bladeTemplate.gameObject.SetActive(false);
    }

    internal void TryHit(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, enemyMask)) return;

        int id = DamageUtil2D.GetRootId(other);
        float now = Time.time;

        float interval = Mathf.Max(0.05f, P.hitInterval);

        if (lastHitTime.TryGetValue(id, out float t))
        {
            if (now - t < interval)
                return;
        }

        lastHitTime[id] = now;
        DamageUtil2D.TryApplyDamage(other, P.damage);
    }
}
