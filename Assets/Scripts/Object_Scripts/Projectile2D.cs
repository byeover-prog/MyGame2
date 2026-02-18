using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Projectile2D : PooledObject, IProjectile2D
{
    [Header("투사체")]
    [SerializeField] private Rigidbody2D rb;

    [Header("이동")]
    [SerializeField, Tooltip("투사체 속도")]
    private float speed = 10f;

    [Header("수명")]
    [SerializeField, Tooltip("자동 반납 시간(초)")]
    private float lifetime = 2.5f;

    [SerializeField, Tooltip("0이면 첫 타격 후 반납, 1이면 1회 관통 후 반납")]
    private int pierceCount = 0;

    [Header("피해(기본값)")]
    [SerializeField, Tooltip("Launch/Fire에서 데미지를 넘기지 않으면 이 값을 사용")]
    private int baseDamage = 10;

    [Header("타겟 마스크(기본값)")]
    [SerializeField, Tooltip("Launch에서 마스크를 넘기지 않으면 이 값을 사용")]
    private LayerMask defaultTargetMask;

    [Header("회전(스프라이트 방향 보정)")]
    [Tooltip("기본 그림이 오른쪽=0, 위쪽=+90 정도가 보통")]
    [SerializeField] private float rotationOffsetDeg = 0f;

    [Tooltip("체크하면 루트(콜라이더/리짓바디 포함)까지 회전, 해제하면 스프라이트(비주얼)만 회전")]
    [SerializeField] private bool rotateWholeObject = false;

    [Tooltip("비주얼만 회전할 때, 회전시킬 대상(보통 SpriteRenderer가 붙은 오브젝트). 비우면 자동으로 SpriteRenderer Transform을 찾음")]
    [SerializeField] private Transform visualTransform;

    [Header("디버그")]
    [SerializeField] private bool debugLog = false;

    private int _damage;
    private float _dieAt;
    private LayerMask _targetMask;
    private int _pierceLeft;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        TryAutoAssignVisual();
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        speed = Mathf.Max(0f, speed);
        lifetime = Mathf.Max(0.05f, lifetime);
        baseDamage = Mathf.Max(1, baseDamage);
        pierceCount = Mathf.Max(0, pierceCount);

        TryAutoAssignVisual();
    }

    private void TryAutoAssignVisual()
    {
        if (visualTransform != null) return;

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) visualTransform = sr.transform;
    }

    public override void OnPoolGet()
    {
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (rotateWholeObject)
            transform.rotation = Quaternion.identity;
        else if (visualTransform != null)
            visualTransform.rotation = Quaternion.identity;
    }

    public override void OnPoolRelease()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    public void Launch(Vector2 dir, int damage, LayerMask targetMask)
    {
        int finalDamage = (damage > 0) ? damage : baseDamage;
        LayerMask finalMask = (targetMask.value != 0) ? targetMask : defaultTargetMask;

        LaunchInternal(dir, finalDamage, finalMask);
    }

    public void Setup(Vector2 dir)
    {
        LaunchInternal(dir, baseDamage, defaultTargetMask);
    }

    public void Fire(Vector2 dirNormalized, int damageOverride)
    {
        int finalDamage = (damageOverride > 0) ? damageOverride : baseDamage;
        LaunchInternal(dirNormalized, finalDamage, defaultTargetMask);
    }

    private void LaunchInternal(Vector2 dir, int damageValue, LayerMask targetMask)
    {
        _pierceLeft = Mathf.Max(0, pierceCount);

        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        _damage = Mathf.Max(1, damageValue);
        _targetMask = targetMask;
        _dieAt = Time.time + lifetime;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
        }

        ApplyRotation(dir);

        if (debugLog)
            Debug.Log($"[Projectile2D] Launch dir={dir} speed={speed} dmg={_damage} mask={_targetMask.value} pierce={_pierceLeft}", this);
    }

    private void ApplyRotation(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle + rotationOffsetDeg);

        if (rotateWholeObject)
        {
            transform.rotation = rot;
            return;
        }

        if (visualTransform != null)
            visualTransform.rotation = rot;
    }

    private void FixedUpdate()
    {
        if (Time.time >= _dieAt)
            ReleaseToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // 레이어 마스크 필터(지정된 경우에만)
        if (_targetMask.value != 0)
        {
            int otherLayerBit = 1 << other.gameObject.layer;
            if ((otherLayerBit & _targetMask.value) == 0)
                return;
        }

        // 데미지: EnemyHealth2D가 없으면 "투사체가 맞아도 무시" (UI/장식 콜라이더 fail-safe)
        var hp = other.GetComponentInParent<EnemyHealth2D>();
        if (hp != null)
        {
            hp.TakeDamage(_damage);
        }
        else
        {
            return;
        }

        if (_pierceLeft <= 0)
        {
            ReleaseToPool();
            return;
        }

        _pierceLeft -= 1;
    }
}
