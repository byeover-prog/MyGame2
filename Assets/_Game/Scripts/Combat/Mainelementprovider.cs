// ──────────────────────────────────────────────
// MainElementProvider.cs
// 메인 캐릭터의 속성을 전역으로 제공하는 정적 클래스
//
// [동작 원리]
// 1. SquadApplier2D 또는 CharacterPassiveManager2D에서 메인 캐릭터 결정 시 Set 호출
// 2. DamageUtil2D의 속성 미지정 버전이 이 값을 읽음
// 3. 공통 스킬들은 코드 변경 없이 자동으로 메인 속성 데미지를 넣게 됨
//
// [Hierarchy / Inspector]
// 컴포넌트가 아님 — Player에 붙은 CharacterPassiveManager2D가 자동 호출
// ──────────────────────────────────────────────

/// <summary>
/// 메인 캐릭터의 데미지 속성을 전역으로 제공합니다.
/// DamageUtil2D에서 속성 미지정 시 이 값을 사용합니다.
/// </summary>
public static class MainElementProvider
{
    /// <summary>현재 메인 캐릭터의 데미지 속성입니다. 기본값 Physical.</summary>
    public static DamageElement2D Element { get; private set; } = DamageElement2D.Physical;

    /// <summary>
    /// 메인 캐릭터 속성을 설정합니다.
    /// CharacterPassiveManager2D.Start()에서 호출합니다.
    /// </summary>
    public static void Set(DamageElement2D element)
    {
        Element = element;
        UnityEngine.Debug.Log($"[MainElementProvider] 메인 속성 설정 → {element}");
    }

    /// <summary>
    /// Physical로 초기화합니다. 씬 전환 시 호출.
    /// </summary>
    public static void Reset()
    {
        Element = DamageElement2D.Physical;
    }
}