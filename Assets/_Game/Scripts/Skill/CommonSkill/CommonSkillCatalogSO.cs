using System.Collections.Generic;
using UnityEngine;
using _Game.Skills;

/// <summary>
/// [구현 원리 요약]
/// 프로젝트에 존재하는 모든 공통 스킬(CommonSkillConfigSO)의 목록을 보관합니다.
/// 새 4장 레벨업 시스템에서 선택한 SkillDefinitionSO를 실제 CommonSkillConfigSO로 매핑할 때도 사용합니다.
/// </summary>
[CreateAssetMenu(menuName = "혼령검/공통스킬/카탈로그", fileName = "CommonSkillCatalog")]
public sealed class CommonSkillCatalogSO : ScriptableObject
{
    [Header("공통 스킬 목록")]
    [Tooltip("이 카탈로그에 등록된 모든 공통 스킬 설정(CommonSkillConfigSO)입니다.")]
    public List<CommonSkillConfigSO> skills = new List<CommonSkillConfigSO>(16);

    [Header("카드 풀")]
    [Tooltip("레벨업 시 뽑기에 사용할 카드 풀입니다. SkillRootSO에도 같은 풀을 연결해야 합니다.")]
    public CommonSkillCardPoolSO cardPool;

    // ════════════════════════════════════════════
    //  기존 API
    // ════════════════════════════════════════════

    /// <summary>Kind로 공통 스킬 설정을 찾습니다.</summary>
    public bool TryGetByKind(CommonSkillKind kind, out CommonSkillConfigSO config)
    {
        config = null;

        if (skills == null || skills.Count == 0)
            return false;

        for (int i = 0; i < skills.Count; i++)
        {
            CommonSkillConfigSO candidate = skills[i];
            if (candidate == null) continue;
            if (candidate.kind != kind) continue;

            config = candidate;
            return true;
        }

        return false;
    }

    // ════════════════════════════════════════════
    //  새 시스템용 매핑 API
    // ════════════════════════════════════════════

    /// <summary>
    /// 새 SkillDefinitionSO를 실제 런타임 공통 스킬 설정으로 변환합니다.
    /// skillId와 displayName을 별칭 기반으로 매핑합니다.
    /// </summary>
    public bool TryResolve(SkillDefinitionSO definition, out CommonSkillConfigSO config)
    {
        config = null;

        if (definition == null)
            return false;

        return TryResolve(definition.SkillId, definition.DisplayName, out config);
    }

    /// <summary>
    /// skillId / displayName 기준으로 실제 공통 스킬 설정을 찾습니다.
    /// </summary>
    public bool TryResolve(string skillId, string displayName, out CommonSkillConfigSO config)
    {
        config = null;

        if (skills == null || skills.Count == 0)
            return false;

        string normalizedId = Normalize(skillId);
        string normalizedName = Normalize(displayName);

        for (int i = 0; i < skills.Count; i++)
        {
            CommonSkillConfigSO candidate = skills[i];
            if (candidate == null) continue;

            if (Matches(candidate, normalizedId, normalizedName))
            {
                config = candidate;
                return true;
            }
        }

        return false;
    }

    // ════════════════════════════════════════════
    //  내부 매칭 로직
    // ════════════════════════════════════════════

    private bool Matches(CommonSkillConfigSO candidate, string normalizedId, string normalizedName)
    {
        if (candidate == null) return false;

        // displayName 직접 비교
        string catalogName = Normalize(candidate.displayName);
        if (!string.IsNullOrEmpty(normalizedName) && normalizedName == catalogName)
            return true;

        // 별칭 비교
        foreach (string alias in EnumerateAliases(candidate.kind))
        {
            if (!string.IsNullOrEmpty(normalizedId) && normalizedId == alias)
                return true;

            if (!string.IsNullOrEmpty(normalizedName) && normalizedName == alias)
                return true;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateAliases(CommonSkillKind kind)
    {
        switch (kind)
        {
            case CommonSkillKind.OrbitingBlade:
                yield return Normalize("weapon_rotatesword");
                yield return Normalize("weapon_orbitingblade");
                yield return Normalize("회전검");
                yield return Normalize("월륜검");
                break;

            case CommonSkillKind.Boomerang:
                yield return Normalize("weapon_boomerang");
                yield return Normalize("부메랑");
                break;

            case CommonSkillKind.PiercingBullet:
                yield return Normalize("weapon_bullet");
                yield return Normalize("총알");
                yield return Normalize("화승총");
                break;

            case CommonSkillKind.HomingMissile:
                yield return Normalize("weapon_homming");
                yield return Normalize("weapon_homing");
                yield return Normalize("호밍미사일");
                yield return Normalize("정화구");
                break;

            case CommonSkillKind.DarkOrb:
                yield return Normalize("weapon_darkorb");
                yield return Normalize("다크오브");
                yield return Normalize("암흑구");
                break;

            case CommonSkillKind.Shuriken:
                yield return Normalize("weapon_shuriken");
                yield return Normalize("수리검");
                yield return Normalize("나선형수리검");
                break;

            case CommonSkillKind.ArrowShot:
                yield return Normalize("weapon_arrow");
                yield return Normalize("화살");
                yield return Normalize("발시");
                break;

            case CommonSkillKind.ArrowRain:
                yield return Normalize("weapon_arrowrain");
                yield return Normalize("화살비");
                yield return Normalize("화차");
                break;

            case CommonSkillKind.Balsi:
                yield return Normalize("weapon_balsi");
                yield return Normalize("발시");
                break;

            case CommonSkillKind.ThunderTalisman:
                yield return Normalize("weapon_thundertalisman");
                yield return Normalize("낙뢰부");
                break;
        }
    }

    private static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        return raw
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }
}