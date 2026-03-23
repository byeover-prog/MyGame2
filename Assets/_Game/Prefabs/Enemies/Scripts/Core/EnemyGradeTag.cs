// UTF-8
// ============================================================================
// EnemyGradeTag.cs
// 경로: Assets/_Game/Scripts/Skill_Scripts/HomingMissile/EnemyGradeTag.cs
//
// [구현 원리]
// 적 GameObject 루트에 붙이는 단순 태그 컴포넌트.
// Inspector에서 Boss / Elite / Normal 중 선택.
// 정화구의 우선순위 타겟팅 시 이 값을 읽어서 정렬한다.
//
// [Inspector 설정]
// 1. 적 프리팹의 루트 오브젝트 선택
// 2. Add Component → EnemyGradeTag
// 3. Grade 필드에서 Boss / Elite / Normal 선택
//    - 일반 몬스터는 기본값(Normal) 그대로 두면 됨
//    - 보스 프리팹만 Boss로, 엘리트 프리팹만 Elite로 설정
//
// [주의]
// 이 컴포넌트가 없는 적은 PurificationOrbProjectile2D에서
// EnemyGrade.Normal로 자동 처리되므로, 일반 몬스터에는 안 붙여도 무방.
// ============================================================================
using UnityEngine;

/// <summary>
/// 적 등급 태그. 적 프리팹 루트에 부착하여 우선순위 타겟팅에 사용.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyGradeTag : MonoBehaviour
{
    [Tooltip("적의 등급입니다. Boss > Elite > Normal 순으로 우선 타겟팅됩니다.")]
    [SerializeField] private EnemyGrade grade = EnemyGrade.Normal;

    /// <summary>이 적의 등급을 반환합니다.</summary>
    public EnemyGrade Grade => grade;
}