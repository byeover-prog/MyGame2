// UTF-8
using System;

/// <summary>
/// 캐릭터 속성 종류.
/// 기존 None/Ice/Dark/Electric 값은 유지 (Inspector/에셋 호환).
/// </summary>
public enum CharacterAttributeKind
{
    None     = 0,
    Ice      = 1,   // 빙결
    Dark     = 2,   // 음
    Electric = 3,   // 전기
    Fire     = 4,   // 화염
    Wind     = 5,   // 바람
    Earth    = 6,   // 땅
    Water    = 7,   // 물
    Light    = 8,   // 양
    Physical = 9,   // 물리
}

public static class CharacterAttributeKindExt
{
    /// <summary>한국어 표시명 반환.</summary>
    public static string ToKorean(this CharacterAttributeKind kind)
    {
        return kind switch
        {
            CharacterAttributeKind.Ice      => "빙결",
            CharacterAttributeKind.Dark     => "음",
            CharacterAttributeKind.Electric => "전기",
            CharacterAttributeKind.Fire     => "화염",
            CharacterAttributeKind.Wind     => "바람",
            CharacterAttributeKind.Earth    => "땅",
            CharacterAttributeKind.Water    => "물",
            CharacterAttributeKind.Light    => "양",
            CharacterAttributeKind.Physical => "물리",
            _ => "없음",
        };
    }

    /// <summary>
    /// CharacterAttributeKind → DamageElement2D 변환.
    /// 데미지 시스템에서 속성을 전달할 때 사용.
    /// </summary>
    public static DamageElement2D ToDamageElement(this CharacterAttributeKind kind)
    {
        return kind switch
        {
            CharacterAttributeKind.Ice      => DamageElement2D.Ice,
            CharacterAttributeKind.Dark     => DamageElement2D.Dark,
            CharacterAttributeKind.Electric => DamageElement2D.Electric,
            CharacterAttributeKind.Fire     => DamageElement2D.Fire,
            CharacterAttributeKind.Wind     => DamageElement2D.Wind,
            CharacterAttributeKind.Earth    => DamageElement2D.Earth,
            CharacterAttributeKind.Water    => DamageElement2D.Water,
            CharacterAttributeKind.Light    => DamageElement2D.Light,
            CharacterAttributeKind.Physical => DamageElement2D.Physical,
            _ => DamageElement2D.Physical,
        };
    }
}