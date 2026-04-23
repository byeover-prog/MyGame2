using UnityEngine;

/// <summary>
/// 적 사망 시 QuestManager에 처치를 자동 보고합니다.
/// EnemyHealth2D의 사망 이벤트에 구독하여 동작합니다.
///
/// [사용법 — 두 가지 중 택 1]
///
/// 방법 A: 모든 적 프리팹에 이 컴포넌트 부착 (비추천, 프리팹 수정 많음)
/// 방법 B: EnemyHealth2D.OnDeath 콜백에서 직접 호출 (추천)
///
/// [방법 B — EnemyHealth2D 수정]
/// EnemyHealth2D.cs의 사망 처리 부분에 다음 한 줄 추가:
///
///   QuestKillReporter.Report(gameObject);
///
/// 이렇게 하면 모든 적이 사망 시 자동으로 QuestManager에 보고됩니다.
/// </summary>
public static class QuestKillReporter
{
    /// <summary>
    /// 적 사망을 QuestManager에 보고합니다.
    /// EnemyHealth2D의 사망 처리에서 호출하세요.
    /// </summary>
    /// <param name="enemyObject">사망한 적의 GameObject입니다.</param>
    public static void Report(GameObject enemyObject)
    {
        if (QuestManager.Instance == null) return;
        if (enemyObject == null) return;

        // 등급 판별
        EnemyGradeTag gradeTag = enemyObject.GetComponent<EnemyGradeTag>();
        EnemyGrade grade = gradeTag != null ? gradeTag.Grade : EnemyGrade.Normal;

        // 적 ID 판별 (오브젝트 이름 기반)
        string enemyId = enemyObject.name;

        QuestManager.Instance.ReportKill(grade, enemyId);

        // 엘리트 드랍 처리
        if (grade == EnemyGrade.Elite)
        {
            EliteDropHandler dropHandler = enemyObject.GetComponent<EliteDropHandler>();
            if (dropHandler != null)
                dropHandler.OnDeath();
        }
    }
}