using UnityEngine;

// 플레이어에서 일정 거리 이상 멀어진 적을 자동 정리.

[DisallowMultipleComponent]
public sealed class EnemyAutoDespawn2D : MonoBehaviour
{
    [Header("기준(플레이어)")]
    [SerializeField] private Transform player;

    [Header("정리 거리")]
    [SerializeField] private float despawn_distance = 25f;

    [Header("성능 보호")]
    [Min(1)]
    [Tooltip("거리 체크를 몇 프레임마다 한 번 할지 설정합니다. 3이면 적 300마리 → 프레임당 100마리만 체크.")]
    [SerializeField] private int checkEveryFrames = 3;

    private float despawn_distance_sqr;
    private int _frameOffset;

    private void Awake()
    {
        RebuildCache();
    }

    private void Start()
    {
        ResolvePlayer();
    }

    // OnEnable에서 플레이어 재탐색 (풀에서 재활성화될 때 대비)
    private void OnEnable()
    {
        RebuildCache();
        ResolvePlayer();
    }

    private void Update()
    {
        if (player == null) return;

        // 프레임 분산: 모든 적이 같은 프레임에 거리를 계산하지 않도록 오프셋 분산
        int interval = Mathf.Max(1, checkEveryFrames);
        if (interval > 1)
        {
            // _frameOffset은 InstanceID 기반이므로 적마다 다른 프레임에 체크
            if ((Time.frameCount + _frameOffset) % interval != 0)
                return;
        }

        float d = (player.position - transform.position).sqrMagnitude;
        if (d >= despawn_distance_sqr)
        {
            // Destroy → 풀 반환
            EnemyPoolTag.ReturnToPool(gameObject);
        }
    }

    private void RebuildCache()
    {
        despawn_distance_sqr = despawn_distance * despawn_distance;
        int interval = Mathf.Max(1, checkEveryFrames);
        // InstanceID의 하위 비트를 오프셋으로 사용 → 적마다 분산됨
        _frameOffset = Mathf.Abs(GetInstanceID()) % interval;
    }

    private void ResolvePlayer()
    {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (checkEveryFrames < 1) checkEveryFrames = 1;
        if (despawn_distance < 0f) despawn_distance = 0f;
    }
#endif
}
