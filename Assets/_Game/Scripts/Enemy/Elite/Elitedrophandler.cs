using UnityEngine;

public sealed class EliteDropHandler : MonoBehaviour
{
    [Header("드랍 프리팹")]
    [Tooltip("보물상자 프리팹입니다.")]
    [SerializeField] private GameObject treasureChestPrefab;

    [Tooltip("고경험치 영혼 프리팹입니다.")]
    [SerializeField] private GameObject highExpSoulPrefab;

    
#pragma warning disable 0414
    [Header("보상 설정")]
    [Tooltip("경험치 배율 (일반 대비)입니다.")]
    [SerializeField] private float expMultiplier = 5f;
#pragma warning restore 0414

    [Tooltip("보물상자 드랍 확률 (0~1)입니다.")]
    [SerializeField] private float chestDropRate = 1f;

    [Tooltip("냥 드랍량입니다.")]
    [SerializeField] private int nyangDropAmount = 50;

    [Tooltip("고경험치 영혼 드랍 수입니다.")]
    [SerializeField] private int highExpSoulCount = 3;

    [Header("드랍 위치")]
    [Tooltip("드랍 아이템 흩뿌림 반경입니다.")]
    [SerializeField] private float scatterRadius = 0.5f;

    // ─── 런타임 ───
    private bool _dropped;

    /// <summary>
    /// 엘리트 사망 시 호출합니다.
    /// EnemyHealth2D의 사망 처리에서 호출하거나 이벤트로 구독합니다.
    /// </summary>
    public void OnDeath()
    {
        if (_dropped) return;
        _dropped = true;

        Vector3 pos = transform.position;

        // 보물상자 드랍
        if (treasureChestPrefab != null && Random.value <= chestDropRate)
        {
            Instantiate(treasureChestPrefab, pos, Quaternion.identity);
        }

        // 고경험치 영혼 드랍
        if (highExpSoulPrefab != null)
        {
            for (int i = 0; i < highExpSoulCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 soulPos = pos + new Vector3(offset.x, offset.y, 0f);
                Instantiate(highExpSoulPrefab, soulPos, Quaternion.identity);
            }
        }

        // 냥 즉시 지급
        if (nyangDropAmount > 0)
        {
            PlayerCurrency2D currency = FindFirstObjectByType<PlayerCurrency2D>();
            if (currency != null)
                currency.AddGold(nyangDropAmount);
        }

        // QuestManager에 엘리트 처치 보고
        if (QuestManager.Instance != null)
        {
            EnemyGradeTag gradeTag = GetComponent<EnemyGradeTag>();
            EnemyGrade grade = gradeTag != null ? gradeTag.Grade : EnemyGrade.Elite;
            QuestManager.Instance.ReportKill(grade);
        }

        GameLogger.Log($"[EliteDrop] {gameObject.name} 드랍 완료 — 냥:{nyangDropAmount}, 영혼:{highExpSoulCount}");
    }

    /// <summary>풀링 환경에서 재사용 시 드랍 상태를 초기화합니다.</summary>
    public void ResetDrop()
    {
        _dropped = false;
    }

    void OnEnable()
    {
        _dropped = false;
    }
}