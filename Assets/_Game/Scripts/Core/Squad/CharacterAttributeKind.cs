using System;

public enum CharacterAttributeKind
{
    None = 0,
    Ice = 1,
    Dark = 2,
    Electric = 3,
}

public static class CharacterAttributeKindExt
{
    public static string ToKorean(this CharacterAttributeKind kind)
    {
        return kind switch
        {
            CharacterAttributeKind.Ice => "빙결",
            CharacterAttributeKind.Dark => "음",
            CharacterAttributeKind.Electric => "전기",
            _ => "없음",
        };
    }
}
