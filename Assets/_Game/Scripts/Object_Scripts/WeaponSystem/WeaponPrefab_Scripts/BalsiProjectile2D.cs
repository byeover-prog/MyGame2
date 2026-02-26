// UTF-8
using UnityEngine;

namespace _Game.Scripts.Object_Scripts.WeaponSystem.WeaponPrefab_Scripts
{
    // [구현 원리 요약]
    // - Rigidbody2D.linearVelocity로 이동(Transform 직접 이동 금지: 트리거 누락 방지).
    // - 트리거 충돌 시 Enemy 레이어만 판정하고 데미지를 1회 적용한다.
    // - 데미지 적용은 "콜라이더 오브젝트"에 없을 수 있으므로 부모(루트)까지 탐색해 우회 적용한다.
    // - 관통(pierce) 소진 시 풀로 반납한다.
    [DisallowMultipleComponent]
    public sealed class BalsiProjectile2D : PooledObject2D
    {
        [Header("회전(스프라이트 기준 보정)")]
        [Tooltip("스프라이트가 기본으로 바라보는 각도 보정값(도).\n" +
                 "기본 0=오른쪽(→)이 전방.\n" +
                 "위(↑)가 전방이면 -90 또는 90으로 맞추세요.")]
        [SerializeField] private float _spriteForwardAngleOffsetDeg;

        [Header("디버그")]
        [Tooltip("안 맞을 때 원인(트리거 미발생/레이어 필터/데미지 실패)을 로그로 출력")]
        [SerializeField] private bool _debugLog = false;

        [Header("컴포넌트")]
        [Tooltip("없으면 자동으로 GetComponent<Rigidbody2D>()")]
        [SerializeField] private Rigidbody2D _rb;

        // 런타임 상태(풀 재사용 시 Init으로 덮어씀)
        private LayerMask _enemyMask;
        private int _damage;
        private float _speed;
        private float _life;
        private float _age;
        private Vector2 _dir;
        private int _pierceLeft;

        private void Awake()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _age = 0f;

            // 풀 재사용 시 이전 속도 잔존 방지
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
        }

        public void Init(LayerMask mask, int dmg, float spd, float lifeSeconds, Vector2 direction, int pierceCount)
        {
            _enemyMask = mask;
            _damage = Mathf.Max(1, dmg);
            _speed = Mathf.Max(0.1f, spd);
            _life = Mathf.Max(0.05f, lifeSeconds);

            _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _pierceLeft = Mathf.Max(0, pierceCount);

            ApplyRotation(_dir);

            // ✅ 물리 이동으로 통일(트리거 누락 방지)
            if (_rb != null)
            {
                // 권장: GravityScale=0, FreezeRotation Z, Collision Detection=Continuous(가능하면)
                _rb.linearVelocity = _dir * _speed;
            }
        }

        private void ApplyRotation(in Vector2 d)
        {
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + _spriteForwardAngleOffsetDeg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _age += dt;

            if (_age >= _life)
            {
                ReturnToPool();
                return;
            }

            // RB가 있으면 velocity 유지(외부에서 건드려도 원복)
            if (_rb != null)
                _rb.linearVelocity = _dir * _speed;
            else
                transform.position += (Vector3)(_dir * (_speed * dt));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            // Enemy 레이어만 판정
            if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask))
            {
                if (_debugLog)
                {
                    Debug.Log($"[BalsiProjectile2D] 레이어 필터로 무시됨: other={other.name} layer={LayerMask.LayerToName(other.gameObject.layer)}", this);
                }
                return;
            }

            // 1) 기존 유틸 시도
            bool applied = DamageUtil2D.TryApplyDamage(other, _damage);

            // 2) 유틸이 "콜라이더 오브젝트"만 보고 실패하는 케이스 대비: 부모(루트)로 한 번 더 시도
            if (!applied)
            {
                var parent = other.GetComponentInParent<Collider2D>();
                if (parent != null && parent != other)
                    applied = DamageUtil2D.TryApplyDamage(parent, _damage);

                // 3) 그래도 실패하면, 루트 Transform 기준으로도 한 번 더(유틸이 Transform/GO 오버로드가 있으면 여기서 바꿔도 됨)
                if (!applied)
                {
                    // 프로젝트 유틸에 따라 아래 라인은 필요 없을 수 있음.
                    // applied = DamageUtil2D.TryApplyDamage(other.transform.root, _damage);
                }
            }

            if (_debugLog && !applied)
            {
                Debug.LogWarning($"[BalsiProjectile2D] 트리거는 왔지만 데미지 적용 실패: other={other.name} (체력/피격 컴포넌트가 부모/루트에 있는지 확인)", this);
            }

            // 관통 0이면 즉시 소멸(=풀 반납)
            if (_pierceLeft <= 0)
            {
                ReturnToPool();
                return;
            }

            _pierceLeft--;
        }

        private const string DisplayNameKorean = "발시 투사체";
    }
}