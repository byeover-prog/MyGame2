using System;
using UnityEngine;

// 런타임에 UI로 전달되는 '카드 1장' 데이터.
// - ScriptableObject 없이도 카드/스킬 연결이 되도록 설계
// - prefab은 선택 시 SkillRunner가 장착할 대상


public enum OfferKind
{
    Weapon         = 0,   // 공통 스킬
    Passive        = 1,   // 패시브
    CharacterSkill = 2    // 캐릭터 전용 스킬
}

[Serializable]
public struct Offer
{
    [Header("식별")]
    public string id;
    public OfferKind kind;

    [Header("표시")]
    public Sprite icon;
    public string titleKr;
    [TextArea] public string descKr;
    public string tagKr;

    [Header("동작")]
    public GameObject prefab;

    [Header("전용 스킬")]
    [Tooltip("true면 캐릭터 전용 스킬 → UI에서 테두리/태그로 시각 구분")]
    public bool isExclusive;

    [Tooltip("전용 스킬 소유 캐릭터 ID (예: yunseol, hayul, harin)")]
    public string ownerId;

    [Header("디버그")]
    [Tooltip("이 카드가 선택되면 적용될 '예상 레벨'(표시는 안 함)")]
    public int previewLevel;
}