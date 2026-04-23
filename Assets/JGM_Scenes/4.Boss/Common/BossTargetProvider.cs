// UTF-8
using UnityEngine;

// 구현 원리 요약:
// 보스가 추적하거나 공격할 현재 타겟을 관리한다.
// 기본은 Player 태그를 가진 오브젝트를 찾고,
// 이미 유효한 타겟이 있으면 불필요한 재탐색을 줄인다.

[DisallowMultipleComponent]
public sealed class BossTargetProvider : MonoBehaviour
{
    [Header("타겟 설정")]

    [Tooltip("직접 지정할 현재 타겟\n비어 있으면 자동 탐색한다.")]
    [SerializeField] private Transform currentTarget;

    [Tooltip("자동 탐색할 타겟 태그")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("자동 탐색을 다시 시도하는 간격")]
    [Min(0.05f)]
    [SerializeField] private float searchInterval = 0.5f;

    [Tooltip("시작 시 자동으로 타겟 탐색을 수행할지 여부")]
    [SerializeField] private bool findTargetOnEnable = true;

    [Header("디버그")]

    [Tooltip("디버그 로그 출력 여부")]
    [SerializeField] private bool debugLog = false;


    private float searchTimer = 0f;


    private void OnEnable()
    {
        searchTimer = 0f;

        if (findTargetOnEnable)
        {
            RefreshTarget();
        }
    }

    private void Update()
    {
        if (HasValidTarget())
        {
            return;
        }

        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f)
        {
            return;
        }

        searchTimer = searchInterval;
        RefreshTarget();
    }

    public bool HasTarget()
    {
        return HasValidTarget();
    }

    public Transform GetTarget()
    {
        if (HasValidTarget())
        {
            return currentTarget;
        }

        return null;
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
        searchTimer = searchInterval;

        if (debugLog)
        {
            string targetName = currentTarget != null ? currentTarget.name : "null";
            Debug.Log($"[BossTargetProvider] 타겟 수동 지정 | target={targetName}", this);
        }
    }

    public void ClearTarget()
    {
        currentTarget = null;
        searchTimer = 0f;

        if (debugLog)
        {
            Debug.Log("[BossTargetProvider] 타겟 해제", this);
        }
    }

    public void RefreshTarget()
    {
        currentTarget = FindTargetByTag();

        if (debugLog)
        {
            string targetName = currentTarget != null ? currentTarget.name : "null";
            Debug.Log($"[BossTargetProvider] 타겟 탐색 결과 | target={targetName}", this);
        }
    }

    private bool HasValidTarget()
    {
        if (currentTarget == null)
        {
            return false;
        }

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        return true;
    }

    private Transform FindTargetByTag()
    {
        if (string.IsNullOrWhiteSpace(targetTag))
        {
            return null;
        }

        GameObject found = GameObject.FindGameObjectWithTag(targetTag);
        if (found == null)
        {
            return null;
        }

        return found.transform;
    }
}