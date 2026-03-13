// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 물리 충돌(Trigger)을 쓰지 않고 OverlapCircleNonAlloc로만 판정한다.
// - 같은 타겟은 HashSet으로 1회만 타격한다.
// - 풀 사용 전제: 수명/조건 종료 시 SetActive(false)로 반납한다.
// - Initialize()를 안 부르면 “안 맞는 것처럼 보이는” 상태가 되므로, 미초기화 감지 로그를 낸다.

namespace _Game.Scripts.Object_Scripts.WeaponSystem.Projectiles_Ghost
{
    [DisallowMultipleComponent]
    public sealed class GhostPiercingOnceProjectile2D : MonoBehaviour
    {
        [Header("판정")]
        [Tooltip("적 레이어 마스크(Enemy만 포함 권장). 런타임 Initialize로 덮어씁니다.")]
        [SerializeField] private LayerMask enemyMask;

        [Tooltip("타격 판정 반경(유닛)")]
        [Min(0.01f)]
        [SerializeField] private float hitRadius = 0.28f;

        [Header("이동/수명")]
        [Tooltip("이동 속도(유닛/초)")]
        [Min(0f)]
        [SerializeField] private float speed = 14f;

        [Tooltip("수명(초)")]
        [Min(0.01f)]
        [SerializeField] private float lifeTime = 2.0f;

        [Header("데미지")]
        [Tooltip("1회 타격 데미지")]
        [Min(0)]
        [SerializeField] private int damage = 1;

        [Header("연출(선택)")]
        [Tooltip("회전 속도(도/초). 0이면 회전 없음")]
        [SerializeField] private float spinDegPerSec = 720f;

        private Vector2 _dir;
        private float _age;

        private bool _initialized;
        private bool _loggedInitError;

        private readonly Collider2D[] _buf = new Collider2D[32];
        private readonly HashSet<int> _hitOnce = new HashSet<int>(64);

        private void OnEnable()
        {
            _age = 0f;
            _hitOnce.Clear();

            _initialized = false;
            _loggedInitError = false;
        }

        public void Initialize(Vector2 dir, int dmg, float spd, float life, LayerMask mask, float radius)
        {
            _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

            damage = Mathf.Max(0, dmg);
            speed = Mathf.Max(0f, spd);
            lifeTime = Mathf.Max(0.01f, life);
            enemyMask = mask;
            hitRadius = Mathf.Max(0.01f, radius);

            // “첫 프레임 이상한 회전” 방지: 방향으로 회전 고정
            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);

            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized)
            {
                // 풀에서 Init 호출 누락을 잡기 위한 안전장치
                if (!_loggedInitError)
                {
                    _loggedInitError = true;
                    Debug.LogWarning("[GhostPiercingOnceProjectile2D] Initialize()가 호출되지 않았습니다. (enemyMask/데미지/속도 누락 가능)", this);
                }

                // 미초기화 상태로 날아다니면 디버깅이 더 어려우니 바로 반납
                gameObject.SetActive(false);
                return;
            }

            _age += Time.deltaTime;
            if (_age >= lifeTime)
            {
                gameObject.SetActive(false);
                return;
            }

            transform.position += (Vector3)(_dir * (speed * Time.deltaTime));

            if (spinDegPerSec != 0f)
                transform.Rotate(0f, 0f, spinDegPerSec * Time.deltaTime);

            TickHit();
        }

        private void TickHit()
        {
            if (enemyMask.value == 0) return; // 마스크 0이면 절대 못 맞음

            int count = Physics2DCompat.OverlapCircleNonAlloc(transform.position, hitRadius, _buf, enemyMask);
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var c = _buf[i];
                if (c == null) continue;

                int id = c.GetInstanceID();
                if (_hitOnce.Contains(id)) continue;

                _hitOnce.Add(id);

                var hp = c.GetComponent<EnemyHealth2D>();
                if (hp == null) continue;

                hp.TakeDamage(damage);
            }
        }
    }
}