using System;
using System.Text;
using UnityEngine;

public enum WeaponFirePattern
{
    AimStraight, // 가장 가까운 적 방향으로 일직선
    AimSpread,   // 가장 가까운 적 방향 + 퍼짐
    Radial       // 360도 원형 발사
}

[CreateAssetMenu(menuName = "Game/Weapon Skill", fileName = "WeaponSkill_")]
public sealed class WeaponSkillSO : ScriptableObject
{
    [Header("식별자(저장/호환용)")]
    [SerializeField] private string id = "wpn_";

    [Header("카드 표시(한글 권장)")]
    [SerializeField] private string displayNameKr = "무기";
    [TextArea(2, 4)]
    [SerializeField] private string baseDescriptionKr = "";
    [SerializeField] private string tagKr = "무기";
    [SerializeField] private Sprite icon;

    [Header("발사 패턴")]
    [SerializeField] private WeaponFirePattern pattern = WeaponFirePattern.AimStraight;

    [Header("투사체 프리팹(Projectile2D 또는 StraightPooledProjectile2D 권장)")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("레벨(1부터)")]
    [SerializeField] private LevelData[] levels = Array.Empty<LevelData>();

    [Serializable]
    public struct LevelData
    {
        [Header("전투 수치")]
        public int damage;
        [Min(0.05f)] public float cooldown;
        [Min(1)] public int projectilesPerShot;

        [Header("퍼짐(도 단위) - Spread/일직선 다 지원")]
        [Min(0f)] public float spreadAngleDeg;

        [Header("카드 설명(비워두면 자동 생성)")]
        [TextArea(2, 4)] public string descriptionOverrideKr;
    }

    public string Id => id;
    public string DisplayNameKr => displayNameKr;
    public string TagKr => tagKr;
    public Sprite Icon => icon;
    public WeaponFirePattern Pattern => pattern;
    public GameObject ProjectilePrefab => projectilePrefab;

    public int MaxLevel => (levels == null) ? 0 : levels.Length;

    public bool IsUsable()
    {
        return !string.IsNullOrEmpty(id) && projectilePrefab != null && MaxLevel > 0;
    }

    public LevelData GetLevelData(int level)
    {
        if (levels == null || levels.Length == 0)
            return default;

        int idx = Mathf.Clamp(level - 1, 0, levels.Length - 1);
        return levels[idx];
    }

    public string BuildCardTitle(int currentLevel, int nextLevel)
    {
        if (currentLevel <= 0) return $"{displayNameKr} (획득)";
        return $"{displayNameKr} Lv.{nextLevel}";
    }

    public string BuildCardDescription(int currentLevel, int nextLevel)
    {
        // nextLevel 기준 데이터
        var next = GetLevelData(nextLevel);

        if (!string.IsNullOrEmpty(next.descriptionOverrideKr))
            return next.descriptionOverrideKr;

        // 자동 생성(한글)
        var sb = new StringBuilder(256);

        if (!string.IsNullOrEmpty(baseDescriptionKr))
        {
            sb.Append(baseDescriptionKr);
            sb.Append('\n');
        }

        if (currentLevel <= 0)
        {
            sb.Append("새 무기 획득\n");
            sb.Append($"피해: {next.damage}\n");
            sb.Append($"쿨타임: {next.cooldown:0.##}s\n");
            sb.Append($"발사 수: {next.projectilesPerShot}\n");
            if (next.spreadAngleDeg > 0.01f)
                sb.Append($"탄 퍼짐: {next.spreadAngleDeg:0.#}도\n");
            return sb.ToString().TrimEnd();
        }

        var cur = GetLevelData(currentLevel);

        if (cur.damage != next.damage)
            sb.Append($"피해: {cur.damage} → {next.damage}\n");

        if (Mathf.Abs(cur.cooldown - next.cooldown) > 0.0001f)
            sb.Append($"쿨타임: {cur.cooldown:0.##}s → {next.cooldown:0.##}s\n");

        if (cur.projectilesPerShot != next.projectilesPerShot)
            sb.Append($"발사 수: {cur.projectilesPerShot} → {next.projectilesPerShot}\n");

        if (Mathf.Abs(cur.spreadAngleDeg - next.spreadAngleDeg) > 0.0001f)
            sb.Append($"탄 퍼짐: {cur.spreadAngleDeg:0.#}도 → {next.spreadAngleDeg:0.#}도\n");

        return sb.ToString().TrimEnd();
    }
}
