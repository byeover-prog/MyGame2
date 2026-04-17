/// <summary>
/// 몬스터의 행동 방식 분류입니다.
/// 
/// 왜 분리하는가:
/// - MonsterType은 일반 / 엘리트 / 보스 같은 "등급" 분류입니다.
/// - MonsterBehaviorType은 추적형 / 원거리형 같은 "행동" 분류입니다.
/// - 둘을 분리해야 같은 Normal이라도 Chase, Ranged를 독립적으로 표현할 수 있습니다.
/// </summary>
public enum MonsterBehaviorType
{
    Chase = 0,
    Ranged = 1,

    // TODO: Dash
    // TODO: Summoner
}