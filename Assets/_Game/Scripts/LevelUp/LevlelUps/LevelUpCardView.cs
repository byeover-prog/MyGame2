using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// [구현 원리 요약]
// SO 데이터(패시브/스킬)에 적힌 텍스트와 아이콘을 읽어와서 실제 화면의 UI(카드)에 글씨를 써주는 역할입니다.
public sealed class LevelUpCardView : MonoBehaviour
{
    [Header("UI 연결 (Inspector에서 끌어다 놓으세요)")]
    [Tooltip("카드의 그림이 들어갈 Image 컴포넌트입니다.")]
    [SerializeField] private Image iconImage;
    
    [Tooltip("카드의 이름(예: 회전검)이 적힐 텍스트 컴포넌트입니다.")]
    [SerializeField] private TMP_Text titleText;
    
    [Tooltip("카드의 수치 설명(예: 데미지 10% 증가)이 적힐 텍스트 컴포넌트입니다.")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("태그 UI (속성 표시용)")]
    [Tooltip("불, 물 등의 속성 태그가 생성될 빈 공간(부모)입니다.")]
    [SerializeField] private Transform tagContainer;
    
    [Tooltip("생성될 태그의 원본 프리팹입니다.")]
    [SerializeField] private GameObject tagItemPrefab;

    private ILevelUpCardData _data;

    // Phase2 이후 공통 카드 바인딩
    public void Bind(ILevelUpCardData data)
    {
        _data = data;

        if (iconImage != null) iconImage.sprite = data != null ? data.Icon : null;
        if (titleText != null) titleText.text = data != null ? data.TitleKorean : string.Empty;
        
        // 설명(Description)을 그대로 가져옵니다. 별도의 LV 문자열을 강제로 붙이지 않으므로, SO에 적힌 수치만 깔끔하게 나옵니다.
        if (descriptionText != null) descriptionText.text = data != null ? data.DescriptionKorean : string.Empty;

        if (data != null) BuildTagsSafe(data.Tags);
        else ClearTagsSafe();
    }

    // Phase1: 기존 LevelUpCardPicker 호환용 브릿지
    // LevelUpCardPicker가 BindWeaponUpgradeCard(...)를 호출하고 있으니 반드시 유지한다.
    public void BindWeaponUpgradeCard(WeaponUpgradeCardSO cardSo)
    {
        // 아직 ILevelUpCardData 적용 전이라면, 최소 표시만 하고 클릭 로직은 기존 Picker 쪽 흐름을 탄다고 가정한다.
        // (Picker가 선택 처리/적용을 직접 한다면 _data는 null이어도 괜찮다.)
        if (iconImage != null) iconImage.sprite = TryGetSprite(cardSo, "icon", "Icon", "sprite");
        if (titleText != null) titleText.text = TryGetString(cardSo, "titleKorean", "TitleKorean", "title", "nameKorean", "NameKorean", "displayName") ?? cardSo.name;
        if (descriptionText != null)
        {
            string desc =
                TryGetString(cardSo, "descriptionKorean", "descKorean", "uiDescription", "uiDesc", "description", "desc") ??
                string.Empty;

            // 설명 문자열이 없으면 최소 문구
            if (string.IsNullOrEmpty(desc)) desc = "효과가 강화 됩니다.";
            descriptionText.text = desc;
        }

        // 태그가 아직 SO에 없을 수 있으니 안전하게 비움
        ClearTagsSafe();
    }

    // 공통 스킬 카드 뷰가 따로 있다면, Phase1 호환으로 이 브릿지도 추가해둔다.
    // (현재 호출이 없다면 영향 없음)
    public void BindCommonSkillCard(CommonSkillCardSO cardSo)
    {
        if (iconImage != null) iconImage.sprite = TryGetSprite(cardSo, "icon", "Icon", "sprite");
        if (titleText != null) titleText.text = TryGetString(cardSo, "titleKorean", "TitleKorean", "title", "nameKorean", "NameKorean", "displayName") ?? cardSo.name;

        if (descriptionText != null)
        {
            string desc =
                TryGetString(cardSo, "descriptionKorean", "descKorean", "uiDescription", "uiDesc", "description", "desc") ??
                string.Empty;

            if (string.IsNullOrEmpty(desc)) desc = "효과가 강화 됩니다.";
            descriptionText.text = desc;
        }

        ClearTagsSafe();
    }

    public void OnClick()
    {
        // Phase2 이후: 공통 카드 방식이면 여기서 처리
        if (_data != null)
        {
            if (_data.CanPick())
                _data.Apply();

            return;
        }

        // Phase1: 기존 Picker 방식(외부에서 OnClick을 잡아 처리)일 수 있으므로 여기서는 아무것도 하지 않는다.
        // (여기서 임의로 적용하면 기존 흐름이 깨질 수 있음)
    }

    private void BuildTagsSafe(IReadOnlyList<SkillTag> tags)
    {
        if (tagContainer == null || tagItemPrefab == null)
            return;

        foreach (Transform child in tagContainer)
            Destroy(child.gameObject);

        if (tags == null) return;

        for (int i = 0; i < tags.Count; i++)
        {
            var go = Instantiate(tagItemPrefab, tagContainer);
            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = ConvertTagToKorean(tags[i]);
        }
    }

    private void ClearTagsSafe()
    {
        if (tagContainer == null) return;

        foreach (Transform child in tagContainer)
            Destroy(child.gameObject);
    }

    private static string ConvertTagToKorean(SkillTag tag)
    {
        switch (tag)
        {
            case SkillTag.Physical: return "물리";
            case SkillTag.Yin: return "음";
            case SkillTag.Yang: return "양";
            case SkillTag.Earth: return "땅";
            case SkillTag.Electric: return "전기";
            case SkillTag.Water: return "물";
            case SkillTag.Fire: return "불";
            case SkillTag.Wind: return "바람";
            case SkillTag.Freeze: return "빙결";
            case SkillTag.Chaos: return "혼돈";
        }
        return string.Empty;
    }

    private static string TryGetString(UnityEngine.Object obj, params string[] candidates)
    {
        if (obj == null) return null;

        var t = obj.GetType();
        foreach (var name in candidates)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(string))
            {
                var v = p.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(string))
            {
                var v = f.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(v)) return v;
            }
        }

        return null;
    }

    private static Sprite TryGetSprite(UnityEngine.Object obj, params string[] candidates)
    {
        if (obj == null) return null;

        var t = obj.GetType();
        foreach (var name in candidates)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType))
                return p.GetValue(obj) as Sprite;

            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
                return f.GetValue(obj) as Sprite;
        }

        return null;
    }
}