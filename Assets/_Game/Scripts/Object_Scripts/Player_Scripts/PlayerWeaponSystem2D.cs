using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerWeaponSystem2D : MonoBehaviour
{
    [Header("발사 기준 Transform(없으면 자기 자신)")]
    [SerializeField] private Transform firePivot;

    [Header("타겟(가장 가까운 적)")]
    [SerializeField] private bool aimNearestEnemy = true;
    [SerializeField] private float aimMaxDistance = 30f;

    [Header("피격 대상 레이어(투사체 Launch에 전달)")]
    [SerializeField] private LayerMask enemyMask;

    private sealed class RuntimeSkill
    {
        public WeaponSkillSO skill;
        public int level; // 0이면 미획득
        public float nextFireTime;
    }

    private readonly List<RuntimeSkill> _runtime = new List<RuntimeSkill>(8);
    private readonly Dictionary<string, RuntimeSkill> _map = new Dictionary<string, RuntimeSkill>(16);

    private void Awake()
    {
        if (firePivot == null) firePivot = transform;
    }

    private void Update()
    {
        float now = Time.time;

        for (int i = 0; i < _runtime.Count; i++)
        {
            var r = _runtime[i];
            if (r == null || r.skill == null) continue;
            if (r.level <= 0) continue;

            var lv = r.skill.GetLevelData(r.level);
            if (lv.cooldown <= 0.01f) lv.cooldown = 0.01f;

            if (now < r.nextFireTime) continue;

            FireSkill(r.skill, lv);
            r.nextFireTime = now + lv.cooldown;
        }
    }

    public void SetSkillLevel(WeaponSkillSO skill, int level)
    {
        if (skill == null) return;
        if (string.IsNullOrEmpty(skill.Id)) return;

        if (!_map.TryGetValue(skill.Id, out var r))
        {
            r = new RuntimeSkill
            {
                skill = skill,
                level = 0,
                nextFireTime = Time.time + 0.1f
            };
            _map.Add(skill.Id, r);
            _runtime.Add(r);
        }

        r.skill = skill;
        r.level = Mathf.Max(0, level);
    }

    private void FireSkill(WeaponSkillSO skill, WeaponSkillSO.LevelData lv)
    {
        if (skill.ProjectilePrefab == null) return;

        Vector3 origin = firePivot != null ? firePivot.position : transform.position;

        Vector2 aimDir = Vector2.right;
        if (aimNearestEnemy && EnemyRegistry2D.TryGetNearest((Vector2)origin, aimMaxDistance, out EnemyRegistryMember2D t) && t != null)
        {
            Vector2 diff = t.Position - (Vector2)origin;
            if (diff.sqrMagnitude > 0.0001f)
                aimDir = diff.normalized;
        }

        switch (skill.Pattern)
        {
            case WeaponFirePattern.AimStraight:
                FireSpread(skill.ProjectilePrefab, origin, aimDir, lv.damage, lv.projectilesPerShot, lv.spreadAngleDeg);
                break;

            case WeaponFirePattern.AimSpread:
                FireSpread(skill.ProjectilePrefab, origin, aimDir, lv.damage, lv.projectilesPerShot, Mathf.Max(1f, lv.spreadAngleDeg));
                break;

            case WeaponFirePattern.Radial:
                FireRadial(skill.ProjectilePrefab, origin, lv.damage, lv.projectilesPerShot);
                break;
        }
    }

    private void FireSpread(GameObject prefab, Vector3 origin, Vector2 dir, int damage, int count, float spreadAngleDeg)
    {
        count = Mathf.Max(1, count);

        if (count == 1 || spreadAngleDeg <= 0.01f)
        {
            SpawnAndLaunch(prefab, origin, dir, damage);
            return;
        }

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float half = spreadAngleDeg * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float t = (count == 1) ? 0.5f : (i / (float)(count - 1));
            float a = (baseAngle - half) + spreadAngleDeg * t;
            Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            SpawnAndLaunch(prefab, origin, d, damage);
        }
    }

    private void FireRadial(GameObject prefab, Vector3 origin, int damage, int count)
    {
        count = Mathf.Max(1, count);

        float step = 360f / count;
        float angle = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 d = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnAndLaunch(prefab, origin, d, damage);
            angle += step;
        }
    }

    private void SpawnAndLaunch(GameObject prefab, Vector3 origin, Vector2 dir, int damage)
    {
        var go = Instantiate(prefab, origin, Quaternion.identity);

        // Projectile2D 우선
        var p = go.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.Launch(dir, damage, enemyMask);
            return;
        }

        // StraightPooledProjectile2D도 지원(풀 없이도 Launch는 됨)
        var sp = go.GetComponent<StraightPooledProjectile2D>();
        if (sp != null)
        {
            sp.Launch(dir, damage, enemyMask);
            return;
        }

        // 마지막 안전장치: Rigidbody2D가 있으면 그냥 밀어줌
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = dir.normalized * 10f;
    }
}
