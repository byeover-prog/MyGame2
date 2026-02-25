// UTF-8
// 요약: ProjectileBase2D(추상) 을 상속한 "모든 투사체 공용" 구현체.
// - 어떤 무기/스킬이든 ProjectileBase2D를 요구하면 이 컴포넌트를 투사체 프리팹에 붙이면 된다.
// - 동작 차이는 Mode + 파라미터로 인스펙터에서 해결.

using UnityEngine;

[DisallowMultipleComponent]
public sealed class UniversalProjectile2D : ProjectileBase2D
{
    public enum MoveMode
    {
        Straight,    // 직선
        Homing,      // 유도
        Ricochet     // 튕김(리코쳇)
    }

    [Header("이동 모드")]
    [SerializeField] private MoveMode mode = MoveMode.Straight;

    [Header("유도")]
    [Tooltip("유도 회전 속도(클수록 급하게 꺾임)")]
    [SerializeField] private float homingTurnSpeed = 720f;

    [Tooltip("유도 최대 탐지 거리")]
    [SerializeField] private float homingRange = 30f;

    [Header("튕김")]
    [Tooltip("튕김 횟수(0이면 튕김 없음)")]
    [SerializeField] private int ricochetCount = 0;

    [Tooltip("튕김 시 다음 타겟 탐지 거리")]
    [SerializeField] private float ricochetRange = 18f;

    [Header("피격 처리")]
    [Tooltip("관통 여부(관통이면 맞아도 파괴되지 않음)")]
    [SerializeField] private bool pierce = true;

    [Tooltip("관통이 아니면 피격 시 파괴")]
    [SerializeField] private bool destroyOnHit = true;

    private Transform _target;
    private int _remainRicochet;

    protected override void Awake()
    {
        base.Awake();
        _remainRicochet = Mathf.Max(0, ricochetCount);
    }

    private void Update()
    {
        // ProjectileBase2D가 이동을 직접 처리하지 않는 구조라면 여기서 이동을 처리한다.
        // (만약 ProjectileBase2D가 velocity 이동을 이미 한다면, 여기 코드는 "방향 갱신"만 하게 바꾸면 됨)
        switch (mode)
        {
            case MoveMode.Homing:
                UpdateHoming();
                break;

            case MoveMode.Ricochet:
                // 리코쳇은 "맞을 때" 방향이 바뀌므로 여기선 별도 이동 갱신 없음
                break;

            default:
                break;
        }
    }

    private void UpdateHoming()
    {
        if (_target == null)
            _target = FindNearestEnemy(homingRange);

        if (_target == null) return;

        Vector2 to = (Vector2)(_target.position - transform.position);
        if (to.sqrMagnitude < 0.0001f) return;

        Vector2 curDir = transform.right; // 기본: 로컬 +X를 전진 방향으로 가정(프로젝트 관례에 맞춰 수정)
        Vector2 desiredDir = to.normalized;

        float maxDelta = homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime;
        Vector2 newDir = Vector2.MoveTowards(curDir, desiredDir, maxDelta).normalized;

        // 방향 갱신
        float ang = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);

        // 실제 이동은 ProjectileBase2D가 처리한다고 가정(그쪽이 방향을 읽도록 되어있어야 함)
        // 만약 Base가 안 움직이면 여기서 transform.position += (Vector3)(newDir * speed * Time.deltaTime) 형태로 이동을 넣어야 함.
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyMask.value) == 0)
            return;

        // TODO: 여기서 데미지/피격은 네 프로젝트의 EnemyHealth/IDamageable에 맞춰 연결
        // 지금 단계 목적은 "ProjectileBase2D 컴포넌트 요구를 만족 + 발사/수명 흐름이 깨지지 않게"임.

        if (mode == MoveMode.Ricochet && _remainRicochet > 0)
        {
            _remainRicochet--;

            // 다음 타겟 탐색(현재 맞은 대상은 제외)
            var next = FindNearestEnemy(ricochetRange, exclude: other.transform);
            if (next != null)
            {
                Vector2 dir = ((Vector2)(next.position - transform.position)).normalized;
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, ang);
                _target = next;
                return; // 튕겼으니 파괴/관통 판단은 다음 충돌 때
            }
        }

        if (!pierce && destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private Transform FindNearestEnemy(float range, Transform exclude = null)
    {
        // 간단 탐색(최적화는 나중에). Base에 이미 유틸이 있으면 그걸 쓰는 게 더 좋음.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, enemyMask);
        float best = float.PositiveInfinity;
        Transform bestT = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == null) continue;
            if (exclude != null && t == exclude) continue;

            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        return bestT;
    }
}