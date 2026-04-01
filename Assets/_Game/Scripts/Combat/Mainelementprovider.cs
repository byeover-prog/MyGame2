// 메인 캐릭터의 데미지 속성을 전역으로 제공합니다.
// DamageUtil2D에서 속성 미지정 시 이 값을 사용합니다.

public static class MainElementProvider
{
    // true = 테스트 중 (전기 비활성화), false = 정상 동작
    private const bool FORCE_PHYSICAL = true;

    // 현재 메인 캐릭터의 데미지 속성입니다. 기본값 Physical.
    public static DamageElement2D Element { get; private set; } = DamageElement2D.Physical;
    
    // 메인 캐릭터 속성을 설정합니다.
    // CharacterPassiveManager2D.Start()에서 호출합니다.

    public static void Set(DamageElement2D element)
    {
        Element = FORCE_PHYSICAL ? DamageElement2D.Physical : element;
        GameLogger.Log($"[MainElementProvider] 메인 속성 설정 → {Element} (원본={element}, 강제Physical={FORCE_PHYSICAL})");
    }
    
    // Physical로 초기화합니다. 씬 전환 시 호출.
    
    public static void Reset()
    {
        Element = DamageElement2D.Physical;
    }
}