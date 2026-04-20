using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BingjuSpikeArea2D : PooledObject2D
{
    [Header("비주얼")]
    [Tooltip("빙주 비주얼 컴포넌트입니다.")]
    [SerializeField] private Component visualBehaviour;

    [Tooltip("빙주 시각용 Transform. 이 Transform 이 루트와 같으면(자식 없는 구조) 안전 모드(스케일/플래시만)로 동작. " +
             "자식이면 낙하/회전 연출 포함.")]
    [SerializeField] private Transform visualTransform;

    [Tooltip("빙주 시각용 SpriteRenderer. 알파/플래시에 사용합니다.")]
    [SerializeField] private SpriteRenderer visualSpriteRenderer;

    [Tooltip("SpriteSkillVisual 기준 반경입니다.")]
    [SerializeField] private float visualBaseRadius = 1f;

    [Header("예고 연출 (자식 구조에서만 적용)")]
    [Tooltip("예고 시작 시 최소 스케일입니다.")]
    [SerializeField] private float minScale = 0.05f;

    [Tooltip("위에서 떨어지는 연출의 시작 높이(y 오프셋). 루트 구조에서는 무시됩니다.")]
    [SerializeField] private float fallHeight = 3f;

    [Header("착탄 연출 (항상 적용)")]
    [Tooltip("착탄 시 스케일 오버슈트 배율입니다. 1.25 = 125% 까지 튀어오름.")]
    [SerializeField] private float impactOvershoot = 1.4f;

    [Tooltip("착탄 오버슈트 유지 시간(초)입니다.")]
    [SerializeField] private float impactOvershootDuration = 0.15f;

    [Tooltip("착탄 시 플래시 밝기(흰색 강도).")]
    [Range(0f, 1f)]
    [SerializeField] private float impactFlashIntensity = 0.85f;

    [Tooltip("착탄 플래시 지속 시간(초).")]
    [SerializeField] private float impactFlashDuration = 0.1f;

    [Header("유지 연출 (자식 구조에서만 적용)")]
    [Tooltip("유지 단계에서 초당 회전 각도(도). 루트 구조에서는 무시됩니다.")]
    [SerializeField] private float holdRotationDegPerSec = 120f;

    [Tooltip("유지 단계의 알파 페이드 시작 비율.")]
    [Range(0f, 1f)]
    [SerializeField] private float fadeStartRatio = 0.65f;

    private ISkillVisual _visual;
    private LayerMask _enemyMask;
    private DamageElement2D _element;
    private int _damage;
    private float _hitRadius;
    private float _armDelay;
    private float _lifetime;
    private float _age;
    private float _frostDuration;
    private float _frostSlowMultiplier;
    private bool _fired;
    private bool _debugLog;
    private Vector3 _impactPoint;

    private EnemyRegistryMember2D _trackedEnemy;

    // 시각 연출 상태
    private float _impactTime;
    private Color _baseSpriteColor;
    private bool _hasBaseSpriteColor;
    private Quaternion _baseVisualRotation;
    private bool _visualIsRoot; // 루트 감지 플래그

    private readonly List<EnemyRegistryMember2D> _targets = new List<EnemyRegistryMember2D>(16);

    private static bool s_diagAwakeLogged;
    private static bool s_diagFireLogged;

    private void Awake()
    {
        ResolveVisual();
        CacheBaseVisualTransform();

        if (!s_diagAwakeLogged)
        {
            Debug.Log(
                $"[빙주Spike 진단] Awake | visual={(_visual == null ? "★NULL★" : _visual.GetType().Name)} " +
                $"| visualIsRoot={_visualIsRoot} " +
                $"(Root=자식없음: 낙하/회전 스킵, 스케일+플래시만 / Child=자식있음: 풀 연출)",
                this);

            if (_visual == null)
                Debug.LogError("[빙주Spike 진단] ISkillVisual 없음.", this);

            s_diagAwakeLogged = true;
        }
    }

    private void OnDisable()
    {
        if (_visual != null)
            _visual.Stop();

        // 자식 구조일 때만 Transform 복구
        if (!_visualIsRoot && visualTransform != null)
        {
            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = _baseVisualRotation;
        }

        // 스프라이트 색 복구 (구조 무관)
        if (_hasBaseSpriteColor && visualSpriteRenderer != null)
            visualSpriteRenderer.color = _baseSpriteColor;

        _trackedEnemy = null;
    }

    public void Init(
        LayerMask enemyMask,
        DamageElement2D damageElement,
        int damage,
        float hitRadius,
        float armDelay,
        float lifetime,
        Vector3 impactPoint,
        float frostDuration,
        float frostSlowMultiplier,
        bool enableLog,
        EnemyRegistryMember2D trackedEnemy = null)
    {
        _enemyMask = enemyMask;
        _element = damageElement;
        _damage = Mathf.Max(1, damage);
        _hitRadius = Mathf.Max(0.1f, hitRadius);
        _armDelay = Mathf.Max(0.05f, armDelay);
        _lifetime = Mathf.Max(_armDelay, lifetime);
        _impactPoint = impactPoint;
        _frostDuration = Mathf.Max(0f, frostDuration);
        _frostSlowMultiplier = frostSlowMultiplier;
        _fired = false;
        _age = 0f;
        _impactTime = 0f;
        _debugLog = enableLog;
        _trackedEnemy = trackedEnemy;

        transform.SetParent(null, true);
        transform.position = _impactPoint;

        ResolveVisual();
        CacheBaseVisualTransform();

        if (_visual != null)
        {
            _visual.Play(_impactPoint);
            _visual.UpdatePosition(_impactPoint);
            _visual.UpdateScale(minScale);
        }

        // 자식 구조일 때만 Transform 초기화
        if (!_visualIsRoot && visualTransform != null)
            visualTransform.localRotation = _baseVisualRotation;

        if (_hasBaseSpriteColor && visualSpriteRenderer != null)
            visualSpriteRenderer.color = _baseSpriteColor;
    }

    private void Update()
    {
        _age += Time.deltaTime;

        // 예고 중 추적 대상 위치로 impactPoint 갱신
        if (!_fired && _trackedEnemy != null && _trackedEnemy.IsValidTarget)
        {
            _impactPoint = _trackedEnemy.Transform != null
                ? _trackedEnemy.Transform.position
                : (Vector3)_trackedEnemy.Position;
            transform.position = _impactPoint;
        }

        if (_visual != null)
            _visual.UpdatePosition(_impactPoint);

        // 예고 단계
        if (!_fired)
        {
            float rawT = Mathf.Clamp01(_age / _armDelay);

            // ease-out-cubic
            float easeT = 1f - Mathf.Pow(1f - rawT, 3f);

            float targetScale = _hitRadius / Mathf.Max(0.01f, visualBaseRadius);
            float scale = Mathf.Lerp(minScale, targetScale, easeT);

            if (_visual != null)
                _visual.UpdateScale(scale);

            // 낙하 연출 — 자식 구조에서만
            if (!_visualIsRoot && visualTransform != null)
            {
                float yOffset = (1f - easeT) * fallHeight;
                visualTransform.localPosition = new Vector3(0f, yOffset, 0f);
            }

            if (_age >= _armDelay)
            {
                Fire();
                _fired = true;
                _impactTime = _age;

                if (!_visualIsRoot && visualTransform != null)
                    visualTransform.localPosition = Vector3.zero;

                // 착탄 플래시 (구조 무관 — SpriteRenderer.color만 건드리므로 안전)
                if (_hasBaseSpriteColor && visualSpriteRenderer != null && impactFlashIntensity > 0f)
                {
                    Color flashed = Color.Lerp(_baseSpriteColor, Color.white, impactFlashIntensity);
                    visualSpriteRenderer.color = flashed;
                }

                if (_visual != null)
                    _visual.PlayImpact(_impactPoint);
            }
        }
        // 착탄 + 유지 단계
        else
        {
            float sinceImpact = _age - _impactTime;
            float targetScale = _hitRadius / Mathf.Max(0.01f, visualBaseRadius);

            // 스케일 오버슈트 (구조 무관 — _visual.UpdateScale 통해 적용, 안전)
            float overshootScale = targetScale;
            if (sinceImpact <= impactOvershootDuration && impactOvershootDuration > 0f)
            {
                float overT = Mathf.Clamp01(sinceImpact / impactOvershootDuration);
                float pulse = 4f * overT * (1f - overT); // 0~1 포물선 peak=1
                overshootScale = targetScale * (1f + (impactOvershoot - 1f) * pulse);
            }

            if (_visual != null)
                _visual.UpdateScale(overshootScale);

            // 회전 연출 — 자식 구조에서만
            if (!_visualIsRoot && visualTransform != null && Mathf.Abs(holdRotationDegPerSec) > 0.01f)
            {
                visualTransform.Rotate(0f, 0f, holdRotationDegPerSec * Time.deltaTime, Space.Self);
            }

            // 플래시 -> 페이드 아웃 (구조 무관 — color만 건드림)
            if (_hasBaseSpriteColor && visualSpriteRenderer != null)
            {
                if (impactFlashDuration > 0f && sinceImpact < impactFlashDuration)
                {
                    float flashT = sinceImpact / impactFlashDuration;
                    Color flashed = Color.Lerp(_baseSpriteColor, Color.white, impactFlashIntensity);
                    visualSpriteRenderer.color = Color.Lerp(flashed, _baseSpriteColor, flashT);
                }
                else if (_lifetime > 0f)
                {
                    float lifeRatio = _age / _lifetime;
                    if (lifeRatio > fadeStartRatio)
                    {
                        float fadeT = Mathf.InverseLerp(fadeStartRatio, 1f, lifeRatio);
                        Color c = _baseSpriteColor;
                        c.a = Mathf.Lerp(_baseSpriteColor.a, 0f, fadeT);
                        visualSpriteRenderer.color = c;
                    }
                    else
                    {
                        visualSpriteRenderer.color = _baseSpriteColor;
                    }
                }
            }
        }

        if (_age >= _lifetime)
            ReturnToPool();
    }

    private void Fire()
    {
        _targets.Clear();
        float sqrR = _hitRadius * _hitRadius;
        IReadOnlyList<EnemyRegistryMember2D> members = EnemyRegistry2D.Members;
        for (int i = 0; i < members.Count; i++)
        {
            EnemyRegistryMember2D e = members[i];
            if (e == null || !e.IsValidTarget) continue;
            if ((e.Position - (Vector2)_impactPoint).sqrMagnitude > sqrR) continue;
            _targets.Add(e);
        }

        if (_trackedEnemy != null && _trackedEnemy.IsValidTarget && !_targets.Contains(_trackedEnemy))
            _targets.Add(_trackedEnemy);

        if (_targets.Count == 0)
        {
            if (!s_diagFireLogged)
            {
                Debug.LogWarning("[빙주Spike 진단] Fire 종료: 반경 내 적 0", this);
                s_diagFireLogged = true;
            }
            return;
        }

        int appliedCount = 0;
        int failCount = 0;
        StatusEffectInfo frostInfo = StatusEffectInfo.Frost(_frostDuration, _frostSlowMultiplier);

        for (int i = 0; i < _targets.Count; i++)
        {
            EnemyRegistryMember2D target = _targets[i];
            if (target == null || !target.IsValidTarget) continue;

            bool applied = DamageUtil2D.TryApplyDamage(target.gameObject, _damage, _element);
            if (applied) appliedCount++;
            else failCount++;

            IStatusReceiver[] receivers = target.GetComponentsInChildren<IStatusReceiver>(true);
            if (receivers != null)
            {
                for (int j = 0; j < receivers.Length; j++)
                {
                    if (receivers[j] != null)
                        receivers[j].TryApplyStatus(frostInfo);
                }
            }
        }

        _targets.Clear();

        if (!s_diagFireLogged)
        {
            Debug.Log(
                $"[빙주Spike 진단] Fire 완료(AOE) | 대상={appliedCount}명 피격(실패={failCount}) | " +
                $"dmg={_damage} | hitRadius={_hitRadius}",
                this);
            s_diagFireLogged = true;
        }

        if (_debugLog)
            CombatLog.Log($"[빙주] AOE 적중 {appliedCount}명 dmg={_damage}", this);
    }

    private void ResolveVisual()
    {
        if (visualBehaviour is ISkillVisual cachedVisual)
            _visual = cachedVisual;

        if (_visual == null)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ISkillVisual found)
                {
                    _visual = found;
                    visualBehaviour = behaviours[i];
                    break;
                }
            }
        }

        if (visualTransform == null && visualSpriteRenderer == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                visualSpriteRenderer = sr;
                visualTransform = sr.transform;
            }
        }
        else if (visualTransform != null && visualSpriteRenderer == null)
        {
            visualSpriteRenderer = visualTransform.GetComponentInChildren<SpriteRenderer>(true);
        }
        else if (visualSpriteRenderer != null && visualTransform == null)
        {
            visualTransform = visualSpriteRenderer.transform;
        }
    }

    private void CacheBaseVisualTransform()
    {
        // 루트 감지 — visualTransform 이 자기 자신(또는 null)이면 루트 구조
        _visualIsRoot = (visualTransform == null || visualTransform == transform);

        if (visualTransform != null)
            _baseVisualRotation = visualTransform.localRotation;

        if (visualSpriteRenderer != null)
        {
            _baseSpriteColor = visualSpriteRenderer.color;
            _hasBaseSpriteColor = true;
        }
    }
}