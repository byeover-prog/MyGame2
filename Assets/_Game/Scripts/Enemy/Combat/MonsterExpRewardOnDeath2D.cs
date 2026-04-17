using UnityEngine;

/// <summary>
/// 몬스터 사망 시 경험치 오브를 드랍하는 보상 전용 컴포넌트입니다.
///
/// 왜 따로 분리하는가:
/// - EnemyHealth2D는 체력만 관리해야 합니다.
/// - MonsterDeathHandler2D는 공통 사망 후처리만 담당해야 합니다.
/// - 경험치 드랍은 "보상" 책임이므로 별도 컴포넌트가
///   MonsterDeathHandler2D의 Died 이벤트를 구독해 처리하는 편이 구조적으로 맞습니다.
///
/// 현재 단계에서는 expReward를 MonsterDefinitionSO로 통합하지 않았으므로,
/// 경험치 양은 이 컴포넌트의 Inspector 값으로 유지합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterExpRewardOnDeath2D : MonoBehaviour
{
    [Header("1. 필수 연결")]
    [SerializeField, Tooltip("사망 이벤트를 받을 MonsterDeathHandler2D입니다.\n"
                             + "보통 같은 프리팹 루트의 MonsterDeathHandler2D를 연결합니다.")]
    private MonsterDeathHandler2D deathHandler;

    [SerializeField, Tooltip("경험치 오브를 꺼낼 풀입니다.\n"
                             + "비워두면 공유 풀을 자동 생성해 사용합니다.")]
    private ExpOrbPool expOrbPool;

    [SerializeField, Tooltip("드랍할 경험치 오브 프리팹입니다.\n"
                             + "ExpOrb2D가 붙어 있어야 합니다.")]
    private ExpOrb2D expOrbPrefab;

    [Header("2. 드랍 설정")]
    [SerializeField, Min(0), Tooltip("처치 시 지급할 총 경험치 양입니다.\n"
                                     + "현재 단계에서는 MonsterDefinitionSO가 아니라\n"
                                     + "이 컴포넌트에서 별도로 관리합니다.")]
    private int expAmount = 3;

    [SerializeField, Tooltip("총 경험치를 여러 개의 오브로 나눠 드랍할지 여부입니다.\n"
                             + "끄면 오브 1개에 expAmount 전체를 담습니다.")]
    private bool splitIntoMultipleOrbs = false;

    [SerializeField, Min(1), Tooltip("여러 개로 나눌 때 생성할 최대 오브 수입니다.\n"
                                     + "splitIntoMultipleOrbs가 켜진 경우에만 사용합니다.")]
    private int maxOrbs = 6;

    [Header("3. 디버그")]
    [SerializeField, Tooltip("경험치 드랍 로그를 출력할지 여부입니다.")]
    private bool debugLog = false;

    private static ExpOrbPool sharedPool;
    private static bool prewarmedSharedPool;

    private void Reset()
    {
        deathHandler = GetComponent<MonsterDeathHandler2D>();
    }

    private void Awake()
    {
        EnsureExpPool();
        PrewarmIfNeeded();
    }

    private void OnEnable()
    {
        if (deathHandler != null)
            deathHandler.Died += HandleMonsterDied;

        EnsureExpPool();
        PrewarmIfNeeded();
    }

    private void OnDisable()
    {
        if (deathHandler != null)
            deathHandler.Died -= HandleMonsterDied;
    }

    private void HandleMonsterDied(MonsterDeathHandler2D.MonsterDeathContext context)
    {
        DropExp(context.WorldPosition);
    }

    private void EnsureExpPool()
    {
        if (expOrbPool != null)
            return;

        if (sharedPool != null)
        {
            expOrbPool = sharedPool;
            return;
        }

        GameObject go = new GameObject("ExpOrbPool(Shared)");
        sharedPool = go.AddComponent<ExpOrbPool>();
        expOrbPool = sharedPool;
    }

    private void PrewarmIfNeeded()
    {
        if (prewarmedSharedPool)
            return;

        if (expOrbPool == null)
            return;

        if (expOrbPrefab == null)
            return;

        expOrbPool.Prewarm(expOrbPrefab.gameObject, 64);
        prewarmedSharedPool = true;
    }

    private void DropExp(Vector3 worldPosition)
    {
        if (expAmount <= 0)
            return;

        if (expOrbPrefab == null)
            return;

        if (expOrbPool == null)
            return;

        if (!splitIntoMultipleOrbs)
        {
            GameObject go = expOrbPool.Get(expOrbPrefab.gameObject, worldPosition, Quaternion.identity);
            ExpOrb2D orb = go.GetComponent<ExpOrb2D>();

            if (orb != null)
                orb.SetExp(expAmount);

            if (debugLog)
            {
                Debug.Log(
                    $"[MonsterExpRewardOnDeath2D] 단일 오브 드랍 | Exp: {expAmount}",
                    this);
            }

            return;
        }

        int desiredCount = Mathf.Clamp(maxOrbs, 1, 32);
        int remainExp = expAmount;

        for (int i = 0; i < desiredCount; i++)
        {
            int slotsLeft = desiredCount - i;
            int chunk = Mathf.Max(1, remainExp / slotsLeft);

            if (chunk > remainExp)
                chunk = remainExp;

            remainExp -= chunk;

            Vector2 offset = Random.insideUnitCircle * 0.35f;
            Vector3 spawnPosition = worldPosition + (Vector3)offset;

            GameObject go = expOrbPool.Get(expOrbPrefab.gameObject, spawnPosition, Quaternion.identity);
            ExpOrb2D orb = go.GetComponent<ExpOrb2D>();

            if (orb != null)
                orb.SetExp(chunk);

            if (remainExp <= 0)
                break;
        }

        if (debugLog)
        {
            Debug.Log(
                $"[MonsterExpRewardOnDeath2D] 분할 오브 드랍 | Total Exp: {expAmount}",
                this);
        }
    }
}