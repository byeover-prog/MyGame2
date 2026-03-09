using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공통 스킬 카드(ILevelUpCardData) 어댑터.
/// 
/// 핵심 요구사항
/// - 카드에 레벨 표기 금지
/// - 레벨 1에서 "시각적으로 어떻게 공격하는지" 텍스트가 반드시 노출
/// - 인스펙터에서 조절 가능한 수치(쿨/데미지/투사체 등)는 '효과 요약'으로 제공
/// </summary>
public sealed class CommonSkillCardData2D : ILevelUpCardData
{
    private static readonly SkillTag[] EmptyTags = Array.Empty<SkillTag>();

    private readonly CommonSkillManager2D _manager;
    private readonly CommonSkillConfigSO _skill;

    public CommonSkillCardData2D(CommonSkillManager2D manager, CommonSkillConfigSO skill)
    {
        _manager = manager;
        _skill = skill;
    }

    public string TitleKorean => _skill != null ? _skill.displayName : string.Empty;

    public string DescriptionKorean
    {
        get
        {
            if (_skill == null) return string.Empty;

            // 1) 공격 방식 설명(반드시 표시)
            string visual = !string.IsNullOrWhiteSpace(_skill.visualDescriptionKr)
                ? _skill.visualDescriptionKr
                : GetFallbackVisualDescription(_skill.kind);

            // 2) 다음 레벨 효과 요약(레벨 표기 금지)
            string effect = BuildNextLevelSummary();

            return $"{visual}\n\n효과: {effect}";
        }
    }

    public Sprite Icon => _skill != null ? _skill.icon : null;

    public IReadOnlyList<SkillTag> Tags
    {
        get
        {
            // 공통 스킬은 태그가 필수는 아님. 필요해지면 kind 기반으로 채우면 됨.
            return EmptyTags;
        }
    }

    public bool CanPick()
    {
        if (_manager == null) return false;
        if (_skill == null) return false;
        return !_manager.IsMaxLevel(_skill);
    }

    public void Apply()
    {
        if (!CanPick()) return;
        _manager.Upgrade(_skill);
    }

    private string BuildNextLevelSummary()
    {
        if (_manager == null || _skill == null)
            return "";

        int cur = _manager.GetLevel(_skill.kind);
        int next = Mathf.Clamp(cur + 1, 1, Mathf.Max(1, _skill.maxLevel));

        CommonSkillLevelParams p = _skill.GetLevelParams(next);

        // 스킬별로 의미 있는 필드만 간단히.
        switch (_skill.kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return $"피해 {p.damage}, 검 {p.projectileCount}개, 타격간격 {p.hitInterval:0.##}s";

            case CommonSkillKind.Boomerang:
                return $"피해 {p.damage}, 투사체 {p.projectileCount}개, 귀환속도 {p.returnSpeed:0.#}";

            case CommonSkillKind.PiercingBullet:
                return $"피해 {p.damage}, 쿨타임 {p.cooldown:0.##}s";

            case CommonSkillKind.HomingMissile:
                return $"피해 {p.damage}, 추가타겟 {p.chainCount}회, 회전속도 {p.turnSpeedDeg:0.#}°/s";

            case CommonSkillKind.DarkOrb:
                return $"피해 {p.damage}, 분열 {p.splitCount}개, 폭발반경 {p.explosionRadius:0.#}";

            case CommonSkillKind.Shuriken:
                return $"피해 {p.damage}, 튕김 {p.bounceCount}회";

            case CommonSkillKind.ArrowShot:
                return $"피해 {p.damage}, 투사체 {p.projectileCount}개, 퍼짐각 {p.spreadAngleDeg:0.#}°";

            case CommonSkillKind.ArrowRain:
                return $"피해 {p.damage}, 쿨타임 {p.cooldown:0.##}s";
        }

        // fallback
        return $"피해 {p.damage}, 쿨타임 {p.cooldown:0.##}s";
    }

    private static string GetFallbackVisualDescription(CommonSkillKind kind)
    {
        // ProjectGuidelines(공통스킬 x8) 기준 "레벨 1 동작" 텍스트.
        // ※ 여기서는 '각성'이나 상세 수치까지는 적지 않는다(최소 구현).
        switch (kind)
        {
            case CommonSkillKind.OrbitingBlade:
                return "플레이어 주변을 원형으로 회전하며, 닿는 적에게 지속 피해를 줍니다.";
            case CommonSkillKind.Boomerang:
                return "가장 먼 적을 향해 날아갔다가 되돌아오며 관통 공격합니다. 같은 적은 왕복 1회씩만 타격합니다.";
            case CommonSkillKind.PiercingBullet:
                return "가장 가까운 적을 향해 직선 관통 탄을 발사합니다.";
            case CommonSkillKind.HomingMissile:
                return "가장 먼 적을 추적하는 유도 탄을 발사합니다. 첫 대상 이후 추가 타겟을 연속 공격할 수 있습니다.";
            case CommonSkillKind.DarkOrb:
                return "가장 가까운 적을 향해 비관통 구체를 발사합니다. 적중 시 분열/폭발합니다.";
            case CommonSkillKind.Shuriken:
                return "가장 가까운 적에게 던져 적중 시 다른 적에게 튕깁니다.";
            case CommonSkillKind.ArrowShot:
                return "가장 가까운 적을 향해 기본 화살을 발사합니다.";
            case CommonSkillKind.ArrowRain:
                return "체력이 많은 적 위치에 화살을 낙하시켜 장판 피해를 줍니다.";
        }

        return "자동으로 적을 공격합니다.";
    }
}
