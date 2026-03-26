// UTF-8
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 좌격요세 참격 판정 오브젝트.
/// 판정 중심 = 플레이어 + aimDirection × hitForwardOffset (전방 오프셋).
/// 각도 제한으로 전방 부채꼴 판정. VFX 방향과 판정 방향 일치.
/// </summary>
[DisallowMultipleComponent]
public sealed class JwagyeokYoseSlash2D : PooledObject2D
{
    [Header("히트 타이밍")]
    [SerializeField] private float hitDelay = 0.1f;

    [Header("판정 설정")]
    [Tooltip("플레이어에서 판정 중심까지의 전방 오프셋")]
    [SerializeField] private float hitForwardOffset = 1.5f;

    [Tooltip("이 반각(도) 안에 있는 적만 적중. 90 = 전방 반원")]
    [Range(1f, 180f)]
    [SerializeField] private float hitHalfAngle = 75f;

    [Header("베기 VFX")]
    [SerializeField] private GameObject slashVfxPrefab;

    [Tooltip("VFX 전방 오프셋 (hitForwardOffset과 비슷하게 맞추기)")]
    [SerializeField] private float vfxForwardOffset = 1.5f;

    // ── 런타임 ──
    private int _damage;
    private float _radius;
    private float _lifetime;
    private LayerMask _enemyMask;
    private Transform _owner;
    private float _angleDeg;
    private Vector2 _aimDir;

    private float _timer;
    private bool _hasHit;
    private ContactFilter2D _filter;
    private GameObject _activeVfx;

    private readonly List<Collider2D> _hitBuffer = new(32);
    private readonly HashSet<int> _hitIds = new(32);

    /// <summary>
    /// 무기에서 호출. aimDirection = 적 방향 정규화, angleDeg = VFX 회전용.
    /// </summary>
    public void Initialize(int damage, float radius, float lifetime,
                           LayerMask enemyMask, Transform owner,
                           Vector2 aimDirection, float angleDeg)
    {
        _damage    = damage;
        _radius    = radius;
        _lifetime  = lifetime;
        _enemyMask = enemyMask;
        _owner     = owner;
        _angleDeg  = angleDeg;
        _aimDir    = aimDirection.sqrMagnitude > 0.0001f
                     ? aimDirection.normalized : Vector2.right;

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(enemyMask);
        _filter.useLayerMask = true;
        _filter.useTriggers  = true;

        _timer  = 0f;
        _hasHit = false;
        _hitIds.Clear();

        SpawnSlashVfx();
    }

    private void SpawnSlashVfx()
    {
        if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
        if (slashVfxPrefab == null || _owner == null) return;

        Vector3 spawnPos = _owner.position + (Vector3)(_aimDir * vfxForwardOffset);

        // ★ Billboard 파티클은 오브젝트 회전을 무시함
        // → ParticleSystem의 startRotation을 코드에서 직접 설정해야 확실히 돌아감
        _activeVfx = Instantiate(slashVfxPrefab, spawnPos, Quaternion.identity);

        // 모든 파티클 시스템(루트 + 자식)에 회전 적용
        float rotRad = -_angleDeg * Mathf.Deg2Rad; // 파티클은 시계방향이 양수라 부호 반전
        var allPS = _activeVfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allPS)
        {
            var main = ps.main;
            main.startRotation = rotRad;
        }
        Destroy(_activeVfx, _lifetime + 0.5f);
    }

    // ── 라이프사이클 ──

    private void OnEnable()
    {
        _timer  = 0f;
        _hasHit = false;
        _hitIds.Clear();
    }

    private void OnDisable()
    {
        if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 플레이어 + VFX 위치 추적
        if (_owner != null)
        {
            transform.position = _owner.position;
            if (_activeVfx != null)
                _activeVfx.transform.position = _owner.position + (Vector3)(_aimDir * vfxForwardOffset);
        }

        // hitDelay 후 1회 전방 판정
        if (!_hasHit && _timer >= hitDelay)
        {
            PerformSlashDamage();
            _hasHit = true;
        }

        // 수명 만료
        if (_timer >= _lifetime)
        {
            if (_activeVfx != null) { Destroy(_activeVfx); _activeVfx = null; }
            _owner = null;
            ReturnToPool();
        }
    }

    // ── 데미지 판정 — 전방 오프셋 + 각도 제한 ──

    private void PerformSlashDamage()
    {
        if (_owner == null) return;

        // ★ 판정 중심 = 플레이어 + 적 방향 × 오프셋 (전방에서 판정)
        Vector2 hitCenter = (Vector2)_owner.position + _aimDir * hitForwardOffset;

        _hitBuffer.Clear();
        int count = Physics2D.OverlapCircle(hitCenter, _radius, _filter, _hitBuffer);
        if (count == 0) return;

        int hitCount = 0;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _hitBuffer[i];
            if (col == null) continue;

            // ★ 각도 제한 — 적 방향과 조준 방향의 각도차가 hitHalfAngle 이내만 적중
            Vector2 toTarget = ((Vector2)col.bounds.center - (Vector2)_owner.position).normalized;
            float angle = Vector2.Angle(_aimDir, toTarget);
            if (angle > hitHalfAngle) continue;

            int rootId = DamageUtil2D.GetRootId(col);
            if (!_hitIds.Add(rootId)) continue;

            bool applied = DamageUtil2D.TryApplyDamage(col, _damage, DamageElement2D.Dark);
            if (applied) hitCount++;

            // ★ 시작 스킬 시너지: 지원 캐릭터 속성 추가 데미지
            if (applied)
                AttributeSynergyManager2D.TryApplySynergy(col, _damage);
        }

#if UNITY_EDITOR
        if (hitCount > 0)
            GameLogger.Log($"[좌격요세] 전방 베기 적중! {hitCount}명 피해량={_damage}");
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 ownerPos = _owner != null ? _owner.position : transform.position;
        Vector2 aim = _aimDir.sqrMagnitude > 0.0001f ? _aimDir : Vector2.right;
        Vector3 hitCenter = ownerPos + (Vector3)(aim * hitForwardOffset);

        // 판정 원 (보라)
        Gizmos.color = new Color(0.85f, 0.2f, 0.85f, 0.25f);
        Gizmos.DrawWireSphere(hitCenter, _radius > 0f ? _radius : 3f);

        // 전방 각도 범위 (노랑)
        float drawLen = hitForwardOffset + (_radius > 0f ? _radius : 3f);
        Vector3 left  = Quaternion.Euler(0f, 0f, hitHalfAngle)  * (Vector3)aim;
        Vector3 right = Quaternion.Euler(0f, 0f, -hitHalfAngle) * (Vector3)aim;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ownerPos, ownerPos + (Vector3)aim * drawLen);
        Gizmos.DrawLine(ownerPos, ownerPos + left * drawLen);
        Gizmos.DrawLine(ownerPos, ownerPos + right * drawLen);
    }
#endif
}