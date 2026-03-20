/// <summary>
/// CharacterProgressionService2D.AddXp()의 반환 결과입니다.
/// 레벨업 여부와 이전/이후 레벨 정보를 담습니다.
/// </summary>
public struct CharacterProgressionResult2D
{
    /// <summary>XP 추가 전 레벨입니다.</summary>
    public int previousLevel;

    /// <summary>XP 추가 후 레벨입니다.</summary>
    public int newLevel;

    /// <summary>실제 추가된 XP 양입니다.</summary>
    public int xpAdded;

    /// <summary>이번 추가로 올라간 레벨 수입니다.</summary>
    public int levelsGained;

    /// <summary>레벨업이 발생했는지 여부입니다.</summary>
    public bool DidLevelUp => levelsGained > 0;
}