// [구현 원리]
// PooledObject2D를 상속하여 ProjectilePool2D.Get<T>() 제네릭 제약 충족.
// 정화구 투사체는 2개의 상태로 동작하는 간단한 상태 머신:
//
//   ┌──────────┐    충돌    ┌──────────┐    틱 소진    ┌──────────┐
//   │  추적    │ ────────→  │  부착    │ ──────────→   │  소멸    │
//   │ (Chase)  │           │ (Attach) │              │          │
//   └──────────┘           └──────────┘              └──────────┘
//                               │ 대상 사망
//                               ↓
//                          재탐색 → 추적
//
// [시각 피드백] (코드 기반, 외주 VFX 불필요)
// 1. 틱 데미지 시: 정화구 스프라이트 밝게 번쩍 (흰색 → 원래 색, 0.1초)
// 2. 틱 데미지 시: 대상 적 스프라이트에 초록 틴트 깜빡 (0.12초)
// 3. 부착 중: 정화구 스프라이트 알파값 부드러운 펄스 (숨쉬기 효과)
// 4. 소멸 시: 0.3초 페이드아웃 후 풀 반환
//
// 나중에 외주 VFX가 나오면 tickVfxPrefab/despawnVfxPrefab 슬롯에
// 연결하면 코드 피드백과 함께 더 풍성해짐.
//
// [주의사항]
// - Transform.SetParent 사용 금지 (풀링 깨짐 방지)
// - OnDisable에서 반드시 UnregisterAttach 호출
// - rb.linearVelocity는 Unity 6 전용 API
// - 적 SpriteRenderer 캐싱: GetComponentInChildren 1회만 호출
// ============================================================================
using UnityEngine;

/// <summary>
/// 정화구 투사체. 추적 → 부착 → 지속 피해 → 소멸/재탐색.
/// PooledObject2D를 상속하여 ProjectilePool2D와 호환.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class PurificationOrbProjectile2D : PooledObject2D
{
    // ══════════════════════════════════════════════════════════════
    // 상태 enum
    // ══════════════════════════════════════════════════════════════

    private enum OrbState
    {
        Chase,
        Attached,
        FadingOut,  // 소멸 페이드아웃 중
        Dead,
    }

    // ══════════════════════════════════════════════════════════════
    // Inspector
    // ══════════════════════════════════════════════════════════════

    [Header("추적 설정")]
    [SerializeField, Tooltip("추적 이동 속도입니다.")]
    private float chaseSpeed = 8f;

    [SerializeField, Tooltip("추적 시 회전 속도(도/초)입니다.")]
    private float chaseRotateSpeed = 720f;

    [SerializeField, Tooltip("타겟을 잃은 후 재탐색 범위입니다.")]
    private float reSearchRange = 15f;

    [SerializeField, Tooltip("추적 최대 시간(초). 이 시간 내 부착 못 하면 소멸합니다.")]
    private float maxChaseTime = 5f;

    [Header("부착 설정")]
    [SerializeField, Tooltip("부착 시 대상 중심에서의 오프셋 반경입니다. 0이면 중심 고정.")]
    private float attachOrbitRadius = 0.3f;

    [SerializeField, Tooltip("부착 시 공전 속도(도/초)입니다. 0이면 고정.")]
    private float attachOrbitSpeed = 180f;

    [Header("VFX (외주 프리팹 — 없으면 코드 피드백만 동작)")]
    [SerializeField, Tooltip("정화구 본체에 항상 부착되는 VFX 프리팹입니다. 추적/부착 중 상시 표시.")]
    private GameObject bodyVfxPrefab;

    [SerializeField, Tooltip("틱마다 재생할 데미지 이펙트 프리팹입니다.")]
    private GameObject tickVfxPrefab;

    [SerializeField, Tooltip("소멸 시 재생할 이펙트 프리팹입니다.")]
    private GameObject despawnVfxPrefab;

    [Header("시각 피드백 (코드 기반)")]
    [SerializeField, Tooltip("틱 시 정화구 플래시 색상입니다.")]
    private Color tickFlashColor = Color.white;

    [SerializeField, Tooltip("틱 시 플래시 지속 시간(초)입니다.")]
    private float tickFlashDuration = 0.1f;

    [SerializeField, Tooltip("틱 시 적에게 입힐 틴트 색상입니다.")]
    private Color enemyTintColor = new Color(0.4f, 1f, 0.5f, 1f); // 연한 초록

    [SerializeField, Tooltip("틱 시 적 틴트 지속 시간(초)입니다.")]
    private float enemyTintDuration = 0.12f;

    [SerializeField, Tooltip("소멸 페이드아웃 시간(초)입니다.")]
    private float fadeOutDuration = 0.3f;

    [SerializeField, Tooltip("부착 중 펄스 최소 알파입니다.")]
    private float pulseMinAlpha = 0.5f;

    [SerializeField, Tooltip("부착 중 펄스 속도입니다.")]
    private float pulseSpeed = 3f;

    [Header("비주얼")]
    [SerializeField]
    private SpriteRenderer orbSprite;

    [Header("디버그")]
    [SerializeField]
    private bool debugLog;

    // ══════════════════════════════════════════════════════════════
    // 런타임 상태
    // ══════════════════════════════════════════════════════════════

    private OrbState    _state;
    private Transform   _target;
    private Rigidbody2D _rb;
    private Collider2D  _collider;

    // 초기화 파라미터
    private LayerMask _enemyMask;
    private int       _tickDamage;
    private int       _remainingTicks;
    private float     _tickInterval;
    private float     _damageMultiplier;
    private int       _attachOrder;

    // 타이머
    private float _tickTimer;
    private float _chaseTimer;

    // 부착 공전
    private float _orbitAngle;

    // 시각 피드백 상태
    private Color _orbOriginalColor;
    private float _flashTimer;
    private bool  _isFlashing;

    private SpriteRenderer _enemySpriteRenderer;
    private Color _enemyOriginalColor;
    private float _enemyTintTimer;
    private bool  _isEnemyTinted;

    private float _fadeOutTimer;
    private float _pulseTimer;

    // 적 탐색용 (GC 0)
    private readonly Collider2D[] _searchHits = new Collider2D[32];
    private ContactFilter2D _searchFilter;
    private bool _filterReady;

    // 부착된 적 참조
    private GameObject _attachedEnemy;

    // ★ v3: 재탐색 쓰로틀 (매 프레임 FindPriorityTarget 방지)
    private float _reSearchCooldown;

    // 본체 VFX 인스턴스
    private GameObject _bodyVfxInstance;
    private ParticleSystem _bodyParticles;

    // ══════════════════════════════════════════════════════════════
    // 초기화
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// PurificationOrbWeapon2D에서 발사 시 호출.
    /// </summary>
    public void Init(LayerMask enemyMask, Transform target,
                     int tickDamage, int tickCount, float tickInterval, float speed)
    {
        _enemyMask      = enemyMask;
        _target         = target;
        _tickDamage     = tickDamage;
        _remainingTicks = tickCount;
        _tickInterval   = tickInterval;
        chaseSpeed      = speed;

        _state           = OrbState.Chase;
        _chaseTimer      = 0f;
        _tickTimer       = 0f;
        _orbitAngle      = 0f;
        _attachedEnemy   = null;
        _attachOrder     = 0;
        _damageMultiplier = 1f;
        _reSearchCooldown = 0f;

        // 시각 피드백 리셋
        _flashTimer      = 0f;
        _isFlashing      = false;
        _enemyTintTimer  = 0f;
        _isEnemyTinted   = false;
        _fadeOutTimer    = 0f;
        _pulseTimer      = 0f;
        _enemySpriteRenderer = null;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;

        if (_collider == null) _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        _collider.enabled = true;

        // 스프라이트 원본 색상 저장 + 초기화
        if (orbSprite != null)
        {
            _orbOriginalColor = orbSprite.color;
            _orbOriginalColor.a = 1f;
            orbSprite.color = _orbOriginalColor;
        }

        // ★ 본체 VFX 생성 (항상 투사체에 부착)
        SpawnBodyVfx();

        if (debugLog)
            CombatLog.Log($"[정화구] 초기화 완료 — 틱 데미지:{tickDamage}, 틱 횟수:{tickCount}, 타겟:{(target != null ? target.name : "없음")}");
    }

    // ══════════════════════════════════════════════════════════════
    // Unity 라이프사이클
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void OnDisable()
    {
        // 적 틴트 복원
        RestoreEnemyTint();

        // 본체 VFX 정리
        CleanupBodyVfx();

        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }
        _state = OrbState.Dead;
        _rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        // 시각 피드백 업데이트 (모든 상태에서)
        UpdateFlashEffect();
        UpdateEnemyTintEffect();

        switch (_state)
        {
            case OrbState.Chase:
                UpdateChase();
                break;
            case OrbState.Attached:
                UpdateAttached();
                break;
            case OrbState.FadingOut:
                UpdateFadeOut();
                break;
            case OrbState.Dead:
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 시각 피드백: 정화구 플래시
    // ══════════════════════════════════════════════════════════════

    /// <summary>틱 데미지 시 정화구 스프라이트를 밝게 번쩍인다.</summary>
    private void TriggerFlash()
    {
        if (orbSprite == null) return;
        _isFlashing = true;
        _flashTimer = tickFlashDuration;
        orbSprite.color = tickFlashColor;
    }

    private void UpdateFlashEffect()
    {
        if (!_isFlashing || orbSprite == null) return;

        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _isFlashing = false;
            orbSprite.color = _orbOriginalColor;
            return;
        }

        // 플래시 색상 → 원본 색상으로 부드럽게 전환
        float t = _flashTimer / tickFlashDuration;
        orbSprite.color = Color.Lerp(_orbOriginalColor, tickFlashColor, t);
    }

    // ══════════════════════════════════════════════════════════════
    // 시각 피드백: 적 스프라이트 틴트
    // ══════════════════════════════════════════════════════════════

    /// <summary>틱 데미지 시 적 스프라이트에 색 틴트를 입힌다.</summary>
    private void TriggerEnemyTint()
    {
        if (_target == null) return;

        // 한 번만 캐싱
        if (_enemySpriteRenderer == null)
        {
            _enemySpriteRenderer = _target.GetComponentInChildren<SpriteRenderer>();
            if (_enemySpriteRenderer == null) return;
            _enemyOriginalColor = _enemySpriteRenderer.color;
        }

        _isEnemyTinted = true;
        _enemyTintTimer = enemyTintDuration;
        _enemySpriteRenderer.color = enemyTintColor;
    }

    private void UpdateEnemyTintEffect()
    {
        if (!_isEnemyTinted || _enemySpriteRenderer == null) return;

        _enemyTintTimer -= Time.deltaTime;
        if (_enemyTintTimer <= 0f)
        {
            _isEnemyTinted = false;
            _enemySpriteRenderer.color = _enemyOriginalColor;
            return;
        }

        float t = _enemyTintTimer / enemyTintDuration;
        _enemySpriteRenderer.color = Color.Lerp(_enemyOriginalColor, enemyTintColor, t);
    }

    /// <summary>적 틴트를 즉시 원래 색으로 복원한다.</summary>
    private void RestoreEnemyTint()
    {
        if (_isEnemyTinted && _enemySpriteRenderer != null)
        {
            _enemySpriteRenderer.color = _enemyOriginalColor;
            _isEnemyTinted = false;
        }
        _enemySpriteRenderer = null;
    }

    // ══════════════════════════════════════════════════════════════
    // 시각 피드백: 부착 중 펄스 (숨쉬기 효과)
    // ══════════════════════════════════════════════════════════════

    private void UpdatePulse()
    {
        if (orbSprite == null) return;
        if (_isFlashing) return; // 플래시 중에는 펄스 스킵

        _pulseTimer += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(pulseMinAlpha, 1f, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f);
        Color c = _orbOriginalColor;
        c.a = alpha;
        orbSprite.color = c;
    }

    // ══════════════════════════════════════════════════════════════
    // 시각 피드백: 소멸 페이드아웃
    // ══════════════════════════════════════════════════════════════

    /// <summary>본체 VFX를 생성하여 투사체 자식으로 부착한다.</summary>
    private void SpawnBodyVfx()
    {
        // 기존 인스턴스가 있으면 재활용 (풀 반환 후 재사용 시)
        if (_bodyVfxInstance != null)
        {
            _bodyVfxInstance.SetActive(true);
            if (_bodyParticles != null)
            {
                _bodyParticles.Clear();
                _bodyParticles.Play();
            }
            return;
        }

        if (bodyVfxPrefab == null) return;

        _bodyVfxInstance = Instantiate(bodyVfxPrefab, transform);
        _bodyVfxInstance.transform.localPosition = Vector3.zero;
        _bodyVfxInstance.transform.localRotation = Quaternion.identity;

        // ParticleSystem을 루핑으로 강제 설정 (정화구는 지속형이라)
        _bodyParticles = _bodyVfxInstance.GetComponentInChildren<ParticleSystem>();
        if (_bodyParticles != null)
        {
            var main = _bodyParticles.main;
            main.loop = true;
            _bodyParticles.Play();
        }
    }

    /// <summary>본체 VFX를 정지/비활성화한다. (풀 반환 시)</summary>
    private void CleanupBodyVfx()
    {
        if (_bodyVfxInstance == null) return;

        if (_bodyParticles != null)
            _bodyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        _bodyVfxInstance.SetActive(false);
    }

    private void UpdateFadeOut()
    {
        _fadeOutTimer += Time.deltaTime;
        float t = _fadeOutTimer / fadeOutDuration;

        if (orbSprite != null)
        {
            Color c = _orbOriginalColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            orbSprite.color = c;
        }

        // 스케일도 살짝 줄이기
        float scale = Mathf.Lerp(1f, 0.3f, t);
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Sign(transform.localScale.x) * scale * 0.5f : scale * 0.5f,
            scale * 0.5f,
            1f);

        if (t >= 1f)
        {
            _state = OrbState.Dead;
            gameObject.SetActive(false);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 상태 1: 추적 (Chase)
    // ══════════════════════════════════════════════════════════════

    private void UpdateChase()
    {
        _chaseTimer += Time.deltaTime;

        if (_chaseTimer >= maxChaseTime)
        {
            if (debugLog) CombatLog.Log("[정화구] 추적 시간 초과 → 소멸");
            Die();
            return;
        }

        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            _target = FindPriorityTarget();
            if (_target == null)
            {
                if (debugLog) CombatLog.Log("[정화구] 재탐색 실패 → 소멸");
                Die();
                return;
            }
            if (debugLog) CombatLog.Log($"[정화구] 재탐색 성공 → {_target.name}");
        }

        // ★ v3: 재탐색 쓰로틀 (0.25초 간격으로만 재탐색)
        if (!PurificationOrbAttachTracker.CanAttachTo(_target.gameObject))
        {
            _reSearchCooldown -= Time.deltaTime;
            if (_reSearchCooldown > 0f) {} // 쿨다운 중 — 기존 타겟 유지하며 추적 계속
            else
            {
                _reSearchCooldown = 0.25f;
                Transform alt = FindPriorityTarget(_target.gameObject);
                if (alt != null)
                {
                    _target = alt;
                    if (debugLog) CombatLog.Log($"[정화구] 부착 불가 대상 → 대체 타겟: {alt.name}");
                }
            }
        }

        Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist < 0.15f)
        {
            TryAttachToTarget();
            return;
        }

        Vector2 desired = toTarget.normalized * chaseSpeed;
        Vector2 current = _rb.linearVelocity;
        float maxDelta = chaseRotateSpeed * Mathf.Deg2Rad * Time.deltaTime * chaseSpeed;
        _rb.linearVelocity = Vector2.MoveTowards(current, desired, maxDelta);

        float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != OrbState.Chase) return;
        if (_target == null) return;

        if (other.transform == _target || other.transform.root == _target.root)
        {
            TryAttachToTarget();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 부착 시도
    // ══════════════════════════════════════════════════════════════

    private void TryAttachToTarget()
    {
        if (_target == null || _state != OrbState.Chase) return;

        GameObject enemyGo = _target.gameObject;

        if (!PurificationOrbAttachTracker.CanAttachTo(enemyGo))
        {
            if (debugLog) CombatLog.Log($"[정화구] 부착 거부됨 (최대 초과) → {enemyGo.name}");
            _target = FindPriorityTarget(enemyGo);
            if (_target == null)
            {
                Die();
            }
            return;
        }

        _attachOrder = PurificationOrbAttachTracker.GetAttachCount(enemyGo);
        PurificationOrbAttachTracker.RegisterAttach(enemyGo);
        _attachedEnemy = enemyGo;
        _damageMultiplier = PurificationOrbAttachTracker.GetDamageMultiplier(_attachOrder);

        _state = OrbState.Attached;
        _rb.linearVelocity = Vector2.zero;
        _collider.enabled = false;
        _tickTimer = 0f;
        _orbitAngle = Random.Range(0f, 360f);
        _pulseTimer = 0f;

        // 적 SpriteRenderer 캐싱 (부착 시 1회)
        _enemySpriteRenderer = _target.GetComponentInChildren<SpriteRenderer>();
        if (_enemySpriteRenderer != null)
            _enemyOriginalColor = _enemySpriteRenderer.color;

        if (debugLog)
            CombatLog.Log($"[정화구] 부착 성공 → {enemyGo.name} (순서:{_attachOrder}, 배율:{_damageMultiplier:F1})");
    }

    // ══════════════════════════════════════════════════════════════
    // 상태 2: 부착 (Attached)
    // ══════════════════════════════════════════════════════════════

    private void UpdateAttached()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            if (debugLog) CombatLog.Log("[정화구] 부착 대상 사망 → 재탐색");
            RestoreEnemyTint();
            DetachAndReSearch();
            return;
        }

        // 공전 위치 동기화
        if (attachOrbitRadius > 0.01f && attachOrbitSpeed > 0f)
        {
            _orbitAngle += attachOrbitSpeed * Time.deltaTime;
            float rad = _orbitAngle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * attachOrbitRadius;
            transform.position = (Vector2)_target.position + offset;
        }
        else
        {
            transform.position = _target.position;
        }

        // ★ 부착 중 펄스 (숨쉬기 효과)
        UpdatePulse();

        // 틱 타이머
        _tickTimer += Time.deltaTime;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ApplyTickDamage();
            _remainingTicks--;

            if (debugLog)
                CombatLog.Log($"[정화구] 틱 피해 적용 → 남은 틱:{_remainingTicks}");

            if (_remainingTicks <= 0)
            {
                if (debugLog) CombatLog.Log("[정화구] 틱 소진 → 소멸");
                RestoreEnemyTint();
                Die();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 데미지 적용
    // ══════════════════════════════════════════════════════════════

    private void ApplyTickDamage()
    {
        if (_target == null) return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(_tickDamage * _damageMultiplier));

        DamageUtil2D.TryApplyDamage(_target.gameObject, finalDamage);

        // ★ 코드 기반 시각 피드백
        TriggerFlash();       // 정화구 번쩍
        TriggerEnemyTint();   // 적 초록 틴트

        // 외주 VFX (있으면 추가로 재생)
        if (tickVfxPrefab != null)
        {
            VFXSpawner.Spawn(tickVfxPrefab, _target.position, Quaternion.identity);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 부착 해제 + 재탐색
    // ══════════════════════════════════════════════════════════════

    private void DetachAndReSearch()
    {
        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }

        if (_remainingTicks > 0)
        {
            _target = FindPriorityTarget();
            if (_target != null)
            {
                _state = OrbState.Chase;
                _collider.enabled = true;
                _chaseTimer = 0f;
                _enemySpriteRenderer = null; // 새 타겟에서 다시 캐싱
                if (debugLog) CombatLog.Log($"[정화구] 재탐색 성공 → {_target.name}");
                return;
            }
        }

        Die();
    }

    // ══════════════════════════════════════════════════════════════
    // 소멸
    // ══════════════════════════════════════════════════════════════

    private void Die()
    {
        if (_state == OrbState.Dead || _state == OrbState.FadingOut) return;

        // 부착 해제
        RestoreEnemyTint();
        if (_attachedEnemy != null)
        {
            PurificationOrbAttachTracker.UnregisterAttach(_attachedEnemy);
            _attachedEnemy = null;
        }

        _rb.linearVelocity = Vector2.zero;

        // ★ 본체 VFX 파티클 발사 중지 (페이드아웃 동안 자연 소멸)
        if (_bodyParticles != null)
            _bodyParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 외주 VFX
        if (despawnVfxPrefab != null)
        {
            VFXSpawner.Spawn(despawnVfxPrefab, transform.position, Quaternion.identity);
        }

        // ★ 페이드아웃 시작 (즉시 비활성화 대신)
        _state = OrbState.FadingOut;
        _fadeOutTimer = 0f;
    }

    // ══════════════════════════════════════════════════════════════
    // 우선순위 기반 타겟 탐색
    // ══════════════════════════════════════════════════════════════

    private Transform FindPriorityTarget(GameObject exclude = null)
    {
        EnsureFilter();

        int count = Physics2D.OverlapCircle(
            (Vector2)transform.position, reSearchRange, _searchFilter, _searchHits);

        if (count == 0) return null;

        Transform bestTarget = null;
        EnemyGrade bestGrade = (EnemyGrade)999;
        float bestDistSq = float.PositiveInfinity;
        Vector2 myPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _searchHits[i];
            if (hit == null) continue;

            GameObject enemyGo = hit.gameObject;

            if (exclude != null && enemyGo == exclude) continue;
            if (!enemyGo.activeInHierarchy) continue;
            if (!PurificationOrbAttachTracker.CanAttachTo(enemyGo)) continue;

            // ★ v3: TryGetComponent (GetComponent + null 체크보다 빠름, GC 없음)
            // 대부분의 일반 몬스터는 EnemyGradeTag가 없어서 false로 즉시 리턴 (빠른 경로)
            EnemyGrade grade = EnemyGrade.Normal;
            if (enemyGo.TryGetComponent<EnemyGradeTag>(out var gradeTag))
                grade = gradeTag.Grade;

            float distSq = ((Vector2)enemyGo.transform.position - myPos).sqrMagnitude;

            if (grade < bestGrade || (grade == bestGrade && distSq < bestDistSq))
            {
                bestTarget = enemyGo.transform;
                bestGrade = grade;
                bestDistSq = distSq;
            }
        }

        return bestTarget;
    }

    private void EnsureFilter()
    {
        if (_filterReady) return;
        _searchFilter = new ContactFilter2D();
        _searchFilter.SetLayerMask(_enemyMask);
        _searchFilter.useTriggers = true;
        _filterReady = true;
    }
}