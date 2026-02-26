// UTF-8
using System.Collections.Generic;
using UnityEngine;

// [구현 원리 요약]
// - 직선 이동 투사체.
// - 동일 적 중복 타격 방지: HashSet<int>에 "루트 인스턴스 ID" 저장.
// - 구버전 스킬이 Init을 많은 인자로 호출해도 컴파일되도록 오버로드를 제공한다.

[DisallowMultipleComponent]
public sealed class ShurikenProjectile2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    [Header("기본값(Init에서 덮어씀)")]
    [SerializeField] private float defaultSpeed = 12f;
    [SerializeField] private float defaultLifeSeconds = 2f;

    private LayerMask _enemyMask;
    private int _damage;
    private float _speed;
    private float _life;
    private float _age;
    private Vector2 _dir;

    private readonly HashSet<int> _hitSet = new HashSet<int>(64);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    // 기본 Init(권장)
    public void Init(LayerMask enemyMask, int damage, float speed, float lifeSeconds, Vector2 dir)
    {
        _enemyMask = enemyMask;
        _damage = Mathf.Max(1, damage);
        _speed = Mathf.Max(0.1f, speed > 0f ? speed : defaultSpeed);
        _life = Mathf.Max(0.05f, lifeSeconds > 0f ? lifeSeconds : defaultLifeSeconds);
        _age = 0f;

        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        _hitSet.Clear();

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
    }

    // 레거시 호환: ShurikenSkill2D가 Init을 9개 인자로 호출하는 경우를 살리기 위한 오버로드
    // (unused 인자는 무시하고, 핵심 파라미터만 기본 Init으로 연결)
    public void Init(
        LayerMask enemyMask,
        int damage,
        float speed,
        float lifeSeconds,
        Vector2 dir,
        int bounceCount,
        int chainCount,
        float searchRadius,
        Transform startTarget
    )
    {
        Init(enemyMask, damage, speed, lifeSeconds, dir);
        // bounce/chain/search/startTarget은 이 단순 투사체에선 사용하지 않음(컴파일 호환 목적)
    }

    private void FixedUpdate()
    {
        _age += Time.fixedDeltaTime;
        if (_age >= _life)
        {
            Destroy(gameObject);
            return;
        }

        if (rb != null)
            rb.linearVelocity = _dir * _speed;
        else
            transform.position += (Vector3)(_dir * (_speed * Time.fixedDeltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!DamageUtil2D.IsInLayerMask(other.gameObject.layer, _enemyMask)) return;

        int id = DamageUtil2D.GetRootInstanceId(other);
        if (_hitSet.Contains(id)) return;

        _hitSet.Add(id);
        DamageUtil2D.ApplyDamage(other, _damage);

        Destroy(gameObject);
    }
}