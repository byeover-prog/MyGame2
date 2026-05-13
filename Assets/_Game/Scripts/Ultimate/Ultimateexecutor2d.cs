using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공용 궁극기 실행기.
// CharacterDefinitionSO에서 데이터를 읽고, Presenter + Resolver를 조합하여 실행.
// 모든 캐릭터가 이 1개 클래스를 공용으로 사용한다.
//
// [1차 패치 — 동시 입력 deadlock 방지]
//  - SetCharacter / Execute 가 bool 반환으로 변경됨.
//  - 실행 중이면 무조건 거부하고 기존 루틴을 죽이지 않는다.
//  - 강제 취소가 필요하면 ForceCancel()을 명시적으로 호출하라.
//  - ExecuteRoutine은 try/finally로 감싸 어떤 경로에서도 _routine이 null로 복구된다.

[DisallowMultipleComponent]
public sealed class UltimateExecutor2D : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("공용 연출 담당. Player 자식에 있는 UltimatePresenter2D.")]
    [SerializeField] private UltimatePresenter2D presenter;

    [Tooltip("플레이어 Transform. 비워두면 자동 탐색.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("적 탐색 레이어")]
    [SerializeField] private LayerMask enemyMask;

    [Header("성능 보호")]
    [Tooltip("한 번 생성한 Resolver를 끄고 켜며 재사용합니다. 반드시 체크하세요.")]
    [SerializeField] private bool cacheResolvers = true;

    [Header("디버그")]
    [Tooltip("궁극기 실행기 상세 로그를 콘솔에 출력합니다. 동시 입력 버그 추적용.")]
    [SerializeField] private bool debugLog = true;

    // 런타임
    private CharacterDefinitionSO _currentCharacter;
    private UltimateDataSO _currentData;
    private UltimateResolverBase _currentResolver;
    private Coroutine _routine;
    private Action _onFinished;

    // CharacterId → Resolver 인스턴스. 한 번 생성 후 SetActive(false)로 보관.
    private readonly Dictionary<string, UltimateResolverBase> _resolverCache
        = new Dictionary<string, UltimateResolverBase>(4);

    /// <summary>현재 설정된 캐릭터 ID. 외부에서 확인용.</summary>
    public string CurrentCharacterId => _currentCharacter != null ? _currentCharacter.CharacterId : null;

    /// <summary>현재 궁극기가 실행 중인지. mutex 역할.</summary>
    public bool IsExecuting => _routine != null;

    private void Awake()
    {
        if (presenter == null)
            presenter = GetComponentInChildren<UltimatePresenter2D>();
        if (playerTransform == null)
            playerTransform = transform.root;
    }

    private void OnDestroy()
    {
        // 캐시된 Resolver GameObject를 실제 Destroy
        foreach (var kvp in _resolverCache)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                Destroy(kvp.Value.gameObject);
        }
        _resolverCache.Clear();
    }

    // ════════════════════════════════════════════════════
    //  캐릭터 설정 — bool 반환으로 변경
    // ════════════════════════════════════════════════════
    /// <summary>
    /// 현재 메인 캐릭터의 궁극기를 설정한다.
    /// 실행 중이면 거부하고 false 반환. 어떤 상태도 변경하지 않는다.
    /// 정상 설정 시 true 반환.
    /// </summary>
    public bool SetCharacter(CharacterDefinitionSO charDef)
    {
        if (charDef == null)
        {
            GameLogger.LogWarning("[궁극기 실행기] CharacterDefinitionSO가 null입니다!");
            return false;
        }

        // ★ 핵심 패치: 실행 중이면 거부. 기존 루틴을 죽이지 않는다.
        if (_routine != null)
        {
            if (debugLog)
                GameLogger.LogWarning($"[궁극기 실행기] SetCharacter 거부 — 이미 실행 중 (요청={charDef.DisplayName})");
            return false;
        }

        _currentCharacter = charDef;
        _currentData = charDef.UltimateData;

        // 이전 Resolver 비활성화 (Destroy하지 않고 캐시에 보관)
        if (_currentResolver != null)
            SetResolverActive(_currentResolver, false);

        if (charDef.UltimateResolverPrefab == null)
        {
            _currentResolver = null;
            GameLogger.LogWarning($"[궁극기 실행기] {charDef.DisplayName}의 Resolver 프리팹이 비어있습니다.", this);
            return false;
        }

        // 캐시에서 가져오거나 새로 생성
        _currentResolver = GetOrCreateResolver(charDef);
        if (_currentResolver == null)
            return false;

        SetResolverActive(_currentResolver, true);
        _currentResolver.SetCasterTransform(null);

        if (debugLog)
            GameLogger.Log($"[궁극기 실행기] 캐릭터 설정 완료 | {charDef.DisplayName} 궁극기={_currentData?.DisplayName}");

        return true;
    }

    /// <summary>
    /// VFX 발사 기준 위치를 오버라이드한다.
    /// 지원 모드에서 지원 비주얼 Transform으로 설정하면
    /// 화살/발도 VFX가 해당 캐릭터 몸에서 나감. null이면 메인 플레이어로 복원.
    /// </summary>
    public void SetCasterOverride(Transform casterTransform)
    {
        if (_currentResolver != null)
            _currentResolver.SetCasterTransform(casterTransform);
    }

    // ════════════════════════════════════════════════════
    //  실행 — bool 반환으로 변경
    // ════════════════════════════════════════════════════
    /// <summary>
    /// 궁극기 실행. 실행 중이면 거부하고 false 반환.
    /// false 반환 시 onFinished는 호출되지 않는다. (호출자가 자기 _isExecuting을 true로 만들지 않게 하기 위함)
    /// 정상 시작 시 true 반환.
    /// </summary>
    public bool Execute(Action onFinished, bool isSupport = false)
    {
        // ★ 핵심 패치: 실행 중이면 거부. 기존 루틴을 죽이지 않는다.
        if (_routine != null)
        {
            if (debugLog)
                GameLogger.LogWarning($"[궁극기 실행기] Execute 거부 — 이미 실행 중 (현재={CurrentCharacterId})");
            return false;
        }

        if (_currentData == null || _currentResolver == null)
        {
            Debug.LogError("[궁극기 실행기] 데이터 또는 Resolver가 설정되지 않았습니다!", this);
            return false;
        }

        _onFinished = onFinished;
        _routine = StartCoroutine(ExecuteRoutine(isSupport));

        if (debugLog)
            GameLogger.Log($"[궁극기 실행기] Execute 시작 | {_currentCharacter?.DisplayName} 지원={isSupport}");

        return true;
    }

    private IEnumerator ExecuteRoutine(bool isSupport)
    {
        // 콜백 캡처 — finally에서도 안전하게 호출
        Action finishedCallback = _onFinished;
        bool castBegan = false;

        // try/finally — 어떤 yield 분기에서 중단되든 _routine 복구 보장
        try
        {
            float duration = _currentData.Duration;
            float hitDelay = _currentData.HitDelay;
            float hitInterval = _currentData.HitInterval;

            // 지원 모드 데미지 배율 적용
            float damageMultiplier = isSupport && _currentCharacter != null
                ? _currentCharacter.SupportDamageMultiplier
                : 1f;

            _currentResolver.SetDamageMultiplier(damageMultiplier);
            _currentResolver.OnCastBegin();
            castBegan = true;

            // 연출 시작
            presenter?.BeginPresentation(_currentData, duration);

            // 궁극기 발동 신호 (Stage0Director 등에서 감지)
            RunSignals.RaiseUltimateUsed();

            if (debugLog)
                GameLogger.Log($"[궁극기] 시전 시작 | {_currentData.DisplayName} " +
                          $"지원={isSupport} 배율={damageMultiplier:F2} duration={duration}s");

            // 첫 피해 대기
            yield return new WaitForSeconds(hitDelay);

            // 지속 시간 동안 반복 피해
            if (hitInterval > 0f)
            {
                float elapsed = hitDelay;
                while (elapsed < duration)
                {
                    _currentResolver.ResolveHit();
                    yield return new WaitForSeconds(hitInterval);
                    elapsed += hitInterval;
                }
            }
            else
            {
                _currentResolver.ResolveHit();
            }

            // 종료 처리
            presenter?.EndPresentation();
            if (castBegan)
            {
                _currentResolver.OnCastEnd();
                castBegan = false;
            }
            _currentResolver.SetDamageMultiplier(1f);

            if (debugLog)
                GameLogger.Log($"[궁극기] 시전 종료 | {_currentData.DisplayName}");
        }
        finally
        {
            //어떤 경로에서 종료되든 _routine은 반드시 null로 복구
            _routine = null;
            _onFinished = null;

            // 비정상 중단(StopCoroutine 등)에도 OnCastEnd 보장
            if (castBegan && _currentResolver != null)
            {
                presenter?.EndPresentation();
                _currentResolver.OnCastEnd();
                _currentResolver.SetDamageMultiplier(1f);
            }
        }

        // 정상 종료 시에만 콜백 호출
        finishedCallback?.Invoke();
    }

    // ════════════════════════════════════════════════════
    //  강제 취소 — 명시적으로만 호출
    // ════════════════════════════════════════════════════
    /// <summary>
    /// 진행 중인 궁극기를 강제로 취소한다. 일반 입력 흐름에서는 절대 호출하지 마라.
    /// 캐릭터 사망, 씬 전환, 디버그 등 명시적 강제 취소가 필요할 때만 사용한다.
    /// 정상 취소 시 true 반환.
    /// </summary>
    public bool ForceCancel()
    {
        if (_routine == null)
            return false;

        StopCoroutine(_routine);
        _routine = null;

        presenter?.EndPresentation();
        if (_currentResolver != null)
        {
            _currentResolver.OnCastEnd();
            _currentResolver.SetDamageMultiplier(1f);
        }

        Action cb = _onFinished;
        _onFinished = null;

        if (debugLog)
            GameLogger.LogWarning("[궁극기 실행기] ForceCancel — 강제 취소");

        // 강제 취소 시에도 호출자에게 종료 통지 (잠금 누수 방지)
        cb?.Invoke();
        return true;
    }

    // ════════════════════════════════════════════════════
    //  ↓↓↓ 아래는 기존 헬퍼 — 이번 패치 무관, 본인 원본과 다르면 본인 것 사용 ↓↓↓
    // ════════════════════════════════════════════════════

    private UltimateResolverBase GetOrCreateResolver(CharacterDefinitionSO charDef)
    {
        if (charDef == null || charDef.UltimateResolverPrefab == null)
            return null;

        string cacheKey = GetResolverCacheKey(charDef);

        // 캐시에 있으면 재사용
        if (cacheResolvers && _resolverCache.TryGetValue(cacheKey, out UltimateResolverBase cached) && cached != null)
            return cached;

        // 없으면 새로 생성
        GameObject resolverObj = Instantiate(charDef.UltimateResolverPrefab, transform);
        resolverObj.name = $"UltResolver_{charDef.CharacterId}";

        UltimateResolverBase resolver = resolverObj.GetComponent<UltimateResolverBase>();
        if (resolver == null)
        {
            Debug.LogError($"[궁극기 실행기] Resolver 프리팹에 UltimateResolverBase 컴포넌트가 없습니다! " +
                           $"({charDef.DisplayName})", this);
            Destroy(resolverObj);
            return null;
        }

        // Resolver 초기화 — 본인 원본의 Init 시그니처에 맞춰 호출되어야 함
        resolver.Init(charDef.UltimateData, playerTransform, enemyMask);

        if (cacheResolvers)
            _resolverCache[cacheKey] = resolver;

        return resolver;
    }

    private static void SetResolverActive(UltimateResolverBase resolver, bool active)
    {
        if (resolver == null) return;
        if (resolver.gameObject != null && resolver.gameObject.activeSelf != active)
            resolver.gameObject.SetActive(active);
    }

    private static string GetResolverCacheKey(CharacterDefinitionSO charDef)
    {
        if (charDef == null) return string.Empty;
        return !string.IsNullOrEmpty(charDef.CharacterId) ? charDef.CharacterId : charDef.name;
    }
}