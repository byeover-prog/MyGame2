// UTF-8
using UnityEngine;

// [구현 원리 요약]
// - 물리 충돌(Trigger) 없이 유도 이동 + 거리 조건으로만 타격한다.
// - 한 타겟을 hitsPerTarget 횟수만큼 hitInterval마다 타격한다.
// - 타겟이 죽거나 비활성(풀 반납)되면 즉시 새 타겟을 획득한다.
// - Initialize() 누락을 감지해 바로 반납한다(풀 상태 오염 방지).

namespace _Game.Scripts.Object_Scripts.WeaponSystem.Projectiles_Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostHomingMultiHitProjectile2D : MonoBehaviour
    {
        [Header("유도/이동")]
        [Tooltip("유도 반응 속도(클수록 급격히 꺾임)")]
        [Min(0f)]
        [SerializeField] private float turnSpeed = 14f;

        [Tooltip("이동 속도(유닛/초)")]
        [Min(0f)]
        [SerializeField] private float speed = 10f;

        [Header("타격")]
        [Tooltip("한 타겟을 몇 번 때릴지(강화마다 증가)")]
        [Min(1)]
        [SerializeField] private int hitsPerTarget = 3;

        [Tooltip("타격 간격(초)")]
        [Min(0.01f)]
        [SerializeField] private float hitInterval = 0.12f;

        [Tooltip("타격 사거리(반경)")]
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.40f;

        [Tooltip("적 레이어 마스크(Enemy만 포함 권장). 런타임 Initialize로 덮어씁니다.")]
        [SerializeField] private LayerMask enemyMask;

        [Header("수치/수명")]
        [Tooltip("1회 타격 데미지")]
        [Min(0)]
        [SerializeField] private int damage = 1;

        [Tooltip("수명(초)")]
        [Min(0.01f)]
        [SerializeField] private float lifeTime = 4.0f;

        [Header("타겟 탐색")]
        [Tooltip("타겟 탐색 반경(유닛). 화면 크기에 맞게 조절")]
        [Min(0.1f)]
        [SerializeField] private float acquireRange = 30f;

        private float _age;
        private float _hitTimer;

        private Transform _target;
        private int _remainHits;

        private bool _initialized;
        private bool _loggedInitError;

        private readonly Collider2D[] _buf = new Collider2D[48];

        private void OnEnable()
        {
            _age = 0f;
            _hitTimer = 0f;
            _target = null;
            _remainHits = 0;

            _initialized = false;
            _loggedInitError = false;
        }

        public void Initialize(Vector2 initialDir, int dmg, float spd, float life, LayerMask mask, float radius, int hits, float interval)
        {
            damage = Mathf.Max(0, dmg);
            speed = Mathf.Max(0f, spd);
            lifeTime = Mathf.Max(0.01f, life);
            enemyMask = mask;
            hitRadius = Mathf.Max(0.01f, radius);
            hitsPerTarget = Mathf.Max(1, hits);
            hitInterval = Mathf.Max(0.01f, interval);

            // 초기 진행 방향 강제(풀에서 이전 회전값이 남는 문제 제거)
            if (initialDir.sqrMagnitude < 0.0001f) initialDir = Vector2.right;
            initialDir.Normalize();
            float ang = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);

            _remainHits = hitsPerTarget;
            AcquireTarget();

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                if (!_loggedInitError)
                {
                    _loggedInitError = true;
                    Debug.LogWarning("[GhostHomingMultiHitProjectile2D] Initialize()가 호출되지 않았습니다. (마스크/데미지/회전 초기화 누락 가능)", this);
                }
                gameObject.SetActive(false);
                return;
            }

            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                gameObject.SetActive(false);
                return;
            }

            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                _target = null;
                AcquireTarget();
            }

            // 유도 이동: transform.right를 진행 방향으로 사용
            Vector2 dir = (Vector2)transform.right;

            if (_target != null)
            {
                Vector2 to = (Vector2)_target.position - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.0001f)
                {
                    Vector2 desired = to.normalized;
                    dir = Vector2.Lerp(dir, desired, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime)).normalized;

                    float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, ang);
                }
            }

            transform.position += (Vector3)(dir * (speed * Time.deltaTime));

            // 타격 틱
            _hitTimer += Time.deltaTime;
            if (_hitTimer >= hitInterval)
            {
                _hitTimer = 0f;
                TryHitTick();
            }
        }

        private void TryHitTick()
        {
            if (_target == null) return;

            var hp = _target.GetComponent<EnemyHealth2D>();
            if (hp == null)
            {
                _target = null;
                AcquireTarget();
                return;
            }

            // 거리 기반 타격(유령형)
            float sqr = ((Vector2)_target.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr > hitRadius * hitRadius) return;

            hp.TakeDamage(damage);

            _remainHits--;
            if (_remainHits <= 0 || hp.IsDead)
            {
                _target = null;
                _remainHits = hitsPerTarget;
                AcquireTarget();
            }
        }

        private void AcquireTarget()
        {
            if (enemyMask.value == 0) return;

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, acquireRange, _buf, enemyMask);
            if (count <= 0)
            {
                _target = null;
                return;
            }

            float best = float.MaxValue;
            Transform bestT = null;

            for (int i = 0; i < count; i++)
            {
                var c = _buf[i];
                if (c == null) continue;

                float d = ((Vector2)c.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestT = c.transform;
                }
            }

            _target = bestT;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, hitRadius);
            Gizmos.DrawWireSphere(transform.position, acquireRange);
        }
#endif
    }
}