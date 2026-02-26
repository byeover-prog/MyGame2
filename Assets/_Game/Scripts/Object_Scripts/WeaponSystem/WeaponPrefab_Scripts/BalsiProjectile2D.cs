// UTF-8
using UnityEngine;

namespace _Game.Scripts.Object_Scripts.WeaponSystem.WeaponPrefab_Scripts
{
    // [구현 원리 요약]
    // - 직선 이동 투사체(물리 힘 X)로 FixedUpdate에서 위치를 갱신한다.
    // - 트리거 충돌 시 Enemy 레이어만 판정하고 데미지를 1회 적용한다.
    // - 관통(pierce) 소진 시 풀로 반납한다.
    [DisallowMultipleComponent]
    public sealed class BalsiProjectile2D : PooledObject2D
    {
        [Header("회전(스프라이트 기준 보정)")]
        [Tooltip("스프라이트가 기본으로 바라보는 각도 보정값(도).\n" +
                 "기본 0=오른쪽(→)이 전방.\n" +
                 "위(↑)가 전방이면 -90 또는 90으로 맞추세요.")]
        [SerializeField] private float _spriteForwardAngleOffsetDeg; // 0 기본값 대입은 불필요(경고 방지)

        // 런타임 상태(풀 재사용 시 Init으로 덮어씀)
        private LayerMask _enemyMask;
        private int _damage;
        private float _speed;
        private float _life;
        private float _age;
        private Vector2 _dir;

        private int _pierceLeft;

        private void OnEnable()
        {
            // 풀 재사용 시 수명 타이머만 리셋(다른 값은 Init에서 갱신)
            _age = 0f;
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

            // 곱셈 순서/불필요 캐스팅 경고를 줄이기 위해 dt를 지역변수로 분리
            transform.position += (Vector3)(_dir * (_speed * dt));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;

            // Enemy 레이어만 판정
            if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask))
                return;

            // 데미지 적용(유틸이 Collider2D 오버로드를 지원해야 함)
            DamageUtil2D.TryApplyDamage(other, _damage);

            // 관통 0이면 즉시 소멸(=풀 반납)
            if (_pierceLeft <= 0)
            {
                ReturnToPool();
                return;
            }

            _pierceLeft--;
        }

        // (선택) 오타 경고(Balsi) 대응: 인스펙터/로그에 표시할 이름을 따로 둔다.
        // 클래스명 변경은 프리팹 참조가 깨질 수 있어 권장하지 않음.
        private const string DisplayNameKorean = "발시 투사체";
    }
}