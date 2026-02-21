using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "무기 업그레이드 카드"를 ILevelUpCardData로 감싸 UI/선택 로직을 통합하기 위한 어댑터.
/// 
/// 설계 의도
/// - UI는 ILevelUpCardData만 알면 된다.
/// - 실제 적용(업그레이드/레벨 상태 갱신)은 Apply()에서 수행한다.
/// 
/// 주의
/// - 이 클래스는 런타임에만 사용된다.
/// - ScriptableObject 에셋을 생성/저장하지 않는다.
/// </summary>
public sealed class WeaponUpgradeCardData2D : ILevelUpCardData
{
    private static readonly SkillTag[] EmptyTags = Array.Empty<SkillTag>();

    private readonly WeaponUpgradeCardSO _card;
    private readonly WeaponShooterSystem2D _shooter;
    private readonly WeaponDefinitionSO _weaponDef;
    private readonly SkillLevelRuntimeState2D _levelState;
    private readonly WeaponShooterSlotUpgradeApplier2D _applier;

    // 태그는 매번 파싱하지 않도록 캐시(카드 수가 작아도 안정적)
    private SkillTag[] _cachedTags;

    public WeaponUpgradeCardData2D(
        WeaponUpgradeCardSO card,
        WeaponShooterSystem2D shooter,
        WeaponDefinitionSO weaponDef,
        SkillLevelRuntimeState2D levelState,
        WeaponShooterSlotUpgradeApplier2D applier)
    {
        _card = card;
        _shooter = shooter;
        _weaponDef = weaponDef;
        _levelState = levelState;
        _applier = applier;
    }

    public string TitleKorean => _card != null ? _card.GetTitleForUI() : string.Empty;

    public string DescriptionKorean
    {
        get
        {
            if (_card == null) return string.Empty;

            // 1) "공격 방식" 설명(레벨 표기 금지)
            string visual = _weaponDef != null ? _weaponDef.GetVisualDescription() : "가장 가까운 적을 자동으로 조준해 투사체를 발사합니다.";

            // 2) "업그레이드 효과" 설명(수치/효과)
            string effect = _card.GetDescriptionForUI();
            if (string.IsNullOrWhiteSpace(effect))
                effect = "효과 없음";

            // 카드에 레벨 표기 금지 조건을 지키기 위해, 여기서는 Lv.x 문자열을 넣지 않는다.
            return $"{visual}\n\n효과: {effect}";
        }
    }

    public Sprite Icon
    {
        get
        {
            if (_card != null && _card.icon != null) return _card.icon;
            if (_weaponDef != null && _weaponDef.icon != null) return _weaponDef.icon;
            return null;
        }
    }

    public IReadOnlyList<SkillTag> Tags
    {
        get
        {
            if (_cachedTags != null) return _cachedTags;

            string raw = null;
            if (_card != null && !string.IsNullOrWhiteSpace(_card.tagsKr)) raw = _card.tagsKr;
            else if (_weaponDef != null && !string.IsNullOrWhiteSpace(_weaponDef.tagsKr)) raw = _weaponDef.tagsKr;

            _cachedTags = ParseTags(raw);
            return _cachedTags;
        }
    }

    public bool CanPick()
    {
        if (_card == null) return false;

        // Enable(활성화) 카드가 "새로 켜는" 상황이면 슬롯 제한을 확인해야 한다.
        if (_card.type == WeaponUpgradeType.ToggleEnabled && _card.value.toggleBool)
        {
            if (_applier == null) return false;
            return _applier.CanEnableSlot(_card.slotIndex);
        }

        return true;
    }

    public void Apply()
    {
        if (_card == null) return;
        if (!CanPick()) return;

        bool changed = false;

        // 실제 적용
        if (_applier != null)
            changed = _applier.Apply(_card);

        // 적용 실패면 레벨을 올리면 안 된다(데이터/런타임 불일치 방지)
        if (!changed)
            return;

        // 레벨 상태 갱신(0~8)
        if (_levelState != null)
        {
            _levelState.IncreaseLevel(_card.slotIndex, 1);

            // shooter 슬롯의 표시용 레벨도 동기화(디버그/밸런싱 시 혼란 방지)
            if (_shooter != null)
            {
                int lv = _levelState.GetLevel(_card.slotIndex);
                var slots = _shooter.SlotsReadOnly;
                if (slots != null && _card.slotIndex >= 0 && _card.slotIndex < slots.Count)
                {
                    var s = slots[_card.slotIndex];
                    if (s != null)
                        s.level = Mathf.Max(1, lv);
                }
            }
        }
    }

    private static SkillTag[] ParseTags(string tagsKr)
    {
        if (string.IsNullOrWhiteSpace(tagsKr))
            return EmptyTags;

        // 매우 단순한 키워드 매칭(프로토타입)
        // - 여러 태그가 들어오면 모두 반영
        // - Unknown/공통 같은 건 태그로 보여주지 않는다.
        var list = new List<SkillTag>(4);

        void Add(SkillTag t)
        {
            if (!list.Contains(t)) list.Add(t);
        }

        string s = tagsKr;
        if (s.Contains("물리")) Add(SkillTag.Physical);
        if (s.Contains("빙") || s.Contains("동결") || s.Contains("혹한") || s.Contains("빙결")) Add(SkillTag.Freeze);
        if (s.Contains("불") || s.Contains("화")) Add(SkillTag.Fire);
        if (s.Contains("물")) Add(SkillTag.Water);
        if (s.Contains("바람") || s.Contains("풍")) Add(SkillTag.Wind);
        if (s.Contains("땅") || s.Contains("토")) Add(SkillTag.Earth);
        if (s.Contains("전기") || s.Contains("번개")) Add(SkillTag.Electric);
        if (s.Contains("혼돈")) Add(SkillTag.Chaos);
        if (s.Contains("음")) Add(SkillTag.Yin);
        if (s.Contains("양")) Add(SkillTag.Yang);

        return list.Count == 0 ? EmptyTags : list.ToArray();
    }
}
