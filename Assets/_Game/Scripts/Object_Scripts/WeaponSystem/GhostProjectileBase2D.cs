// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - Rigidbody2D/Collider2D 충돌(Trigger)에 의존하지 않고, 코드로만 타격 판정한다.
// - 매 프레임 OverlapCircleNonAlloc로 적을 수집하고, HashSet으로 “같은 타겟 1회만”을 보장한다.
// - 풀 사용 전제: 수명 종료/조건 종료 시 gameObject.SetActive(false)로 반납한다.

namespace _Game.Scripts.Object_Scripts.WeaponSystem.Projectiles_Ghost
{
    [DisallowMultipleComponent]
    public abstract class GhostProjectileBase2D : MonoBehaviour
    {
        [Header("유령형 판정")]
        [Tooltip("적 판정 레이어(Enemy만 포함 권장)")]
        [SerializeField] protected LayerMask enemyMask;

        [Tooltip("타격 판정 반경(유닛)")]
        [Min(0.01f)]
        [SerializeField] protected float hitRadius = 0.25f;

        [Header("기본 수치")]
        [Tooltip("1회 타격 데미지")]
        [Min(0)]
        [SerializeField] protected int damage = 1;

        [Tooltip("이동 속도(유닛/초)")]
        [Min(0f)]
        [SerializeField] protected float speed = 12f;

        [Tooltip("수명(초)")]
        [Min(0.01f)]
        [SerializeField] protected float lifeTime = 2f;

        protected Vector2 moveDir = Vector2.right;
        protected float age;

        // NonAlloc 버퍼 (GC 방지)
        private readonly Collider2D[] _hitBuffer = new Collider2D[32];

        // 같은 타겟 “1회만” 타격 보장
        private readonly HashSet<int> _hitOnceSet = new HashSet<int>(64);

        protected virtual void OnEnable()
        {
            age = 0f;
            _hitOnceSet.Clear();
        }

        public virtual void Launch(Vector2 direction, int dmg, float spd, float life, LayerMask mask, float radius)
        {
            moveDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            damage = Mathf.Max(0, dmg);
            speed = Mathf.Max(0f, spd);
            lifeTime = Mathf.Max(0.01f, life);
            enemyMask = mask;
            hitRadius = Mathf.Max(0.01f, radius);
        }

        protected virtual void Update()
        {
            age += Time.deltaTime;
            if (age >= lifeTime)
            {
                Despawn();
                return;
            }

            transform.position += (Vector3)(moveDir * (speed * Time.deltaTime));
            TickHit();
        }

        protected void TickHit()
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, hitRadius, _hitBuffer, enemyMask);
            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer[i];
                if (col == null) continue;

                int id = col.GetInstanceID();
                if (_hitOnceSet.Contains(id)) continue;

                _hitOnceSet.Add(id);

                if (TryApplyDamage(col, damage))
                    OnHitEnemy(col);
            }
        }

        // 프로젝트 실제 적 체력 스크립트(EnemyHealth2D)에 연결
        protected virtual bool TryApplyDamage(Collider2D enemy, int dmg)
        {
            var hp = enemy.GetComponent<EnemyHealth2D>();
            if (hp == null) return false;

            hp.TakeDamage(dmg);
            return true;
        }

        protected virtual void OnHitEnemy(Collider2D enemy) { }

        protected virtual void Despawn()
        {
            // 풀 반납
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
#endif
    }
}