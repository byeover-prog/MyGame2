// UTF-8
// ============================================================================
// EnemyGrade.cs
// 경로: Assets/_Game/Scripts/Combat/EnemyGrade.cs
//
// [구현 원리]
// 정화구 등 우선순위 기반 타겟팅에서 사용하는 적 등급 enum.
// 값이 작을수록 높은 우선순위. 정렬 시 오름차순 = 보스 우선.
//
// [사용법]
// 적 GameObject 루트에 EnemyGradeTag 컴포넌트를 붙이고 Inspector에서 등급을 설정.
// EnemyGradeTag가 없는 적은 기본적으로 Normal로 취급.
// ============================================================================

/// <summary>
/// 적 등급. 값이 작을수록 높은 우선순위.
/// </summary>
public enum EnemyGrade
{
    /// <summary>보스 (최우선)</summary>
    Boss = 0,

    /// <summary>엘리트</summary>
    Elite = 1,

    /// <summary>일반 몬스터</summary>
    Normal = 2,
}