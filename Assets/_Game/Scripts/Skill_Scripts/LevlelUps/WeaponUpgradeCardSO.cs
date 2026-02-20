using UnityEngine;

[CreateAssetMenu(menuName = "그날이후/업그레이드/무기 업그레이드 카드", fileName = "Card_WeaponUpgrade")]
public sealed class WeaponUpgradeCardSO : ScriptableObject
{
    [Header("대상(슬롯 기반)")]
    [Tooltip("WeaponShooterSystem2D slots 인덱스")]
    public int slotIndex = -1;

    [Tooltip("표시용 무기 이름(한글/에셋명)")]
    public string weaponNameKr;

    [Header("저장용(옵션)")]
    [Tooltip("저장/로그용 무기 ID (UI 표기에는 쓰지 않음)")]
    public string weaponId;

    [Header("업그레이드 종류")]
    public WeaponUpgradeType type;

    [Header("수치")]
    public UpgradeValue value;

    [Header("UI")]
    public Sprite icon;

    [Tooltip("카드 제목(한글)")]
    public string titleKr;

    [Tooltip("카드 설명(한글)")]
    [TextArea]
    public string descKr;

    [Tooltip("태그 표기(예: '공통, 원거리')")]
    public string tagsKr;

    public bool IsValid()
    {
        return slotIndex >= 0;
    }

    public string GetTitleForUI()
    {
        if (!string.IsNullOrWhiteSpace(titleKr)) return titleKr;

        string wname = string.IsNullOrWhiteSpace(weaponNameKr) ? "무기" : weaponNameKr;
        return $"{wname} 업그레이드";
    }

    public string GetDescriptionForUI()
    {
        if (!string.IsNullOrWhiteSpace(descKr)) return descKr;

        return UpgradeTextFormatter.Format(type, value);
    }

    public string GetTagsForUI()
    {
        return string.IsNullOrWhiteSpace(tagsKr) ? "" : tagsKr;
    }
}