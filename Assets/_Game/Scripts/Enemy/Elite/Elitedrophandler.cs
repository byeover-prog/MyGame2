using UnityEngine;

public sealed class EliteDropHandler : MonoBehaviour
{
    [SerializeField] private GameSceneContext sceneContext;

    [Header("Drop Prefabs")]
    [SerializeField] private GameObject treasureChestPrefab;
    [SerializeField] private GameObject highExpSoulPrefab;

    [Header("Reward Settings")]
    [SerializeField] private float chestDropRate = 1f;
    [SerializeField] private int nyangDropAmount = 50;
    [SerializeField] private int highExpSoulCount = 3;

    [Header("Drop Position")]
    [SerializeField] private float scatterRadius = 0.5f;

    private bool _dropped;

    public void OnDeath()
    {
        if (_dropped) return;
        _dropped = true;

        Vector3 pos = transform.position;

        if (treasureChestPrefab != null && Random.value <= chestDropRate)
            Instantiate(treasureChestPrefab, pos, Quaternion.identity);

        if (highExpSoulPrefab != null)
        {
            for (int i = 0; i < highExpSoulCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * scatterRadius;
                Vector3 soulPos = pos + new Vector3(offset.x, offset.y, 0f);
                Instantiate(highExpSoulPrefab, soulPos, Quaternion.identity);
            }
        }

        if (nyangDropAmount > 0)
        {
            PlayerCurrency2D currency = ResolvePlayerCurrency();
            if (currency != null)
                currency.AddGold(nyangDropAmount);
        }

        if (QuestManager.Instance != null)
        {
            EnemyGradeTag gradeTag = GetComponent<EnemyGradeTag>();
            EnemyGrade grade = gradeTag != null ? gradeTag.Grade : EnemyGrade.Elite;
            QuestManager.Instance.ReportKill(grade);
        }

        GameLogger.Log($"[EliteDrop] {gameObject.name} drop complete. nyang={nyangDropAmount}, souls={highExpSoulCount}", this);
    }

    public void ResetDrop()
    {
        _dropped = false;
    }

    private void OnEnable()
    {
        _dropped = false;
    }

    private PlayerCurrency2D ResolvePlayerCurrency()
    {
        if (sceneContext == null)
            sceneContext = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (sceneContext != null)
        {
            PlayerCurrency2D currency = sceneContext.GetPlayerComponent<PlayerCurrency2D>();
            if (currency != null)
                return currency;
        }

        return FindFirstObjectByType<PlayerCurrency2D>();
    }
}
