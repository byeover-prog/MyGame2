using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공용 궁극기 실행기.
// CharacterDefinitionSO에서 데이터를 읽고, Presenter + Resolver를 조합하여 실행.
// 모든 캐릭터가 이 1개 클래스를 공용으로 사용한다.

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

    /// <summary>현재 궁극기가 실행 중인지.</summary>
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
        // GPT 원본에서 누락된 부분: 캐시된 Resolver GameObject를 실제 Destroy
        // Dictionary.Clear()만 하면 고아 GameObject가 씬에 남음
        foreach (var kvp in _resolverCache)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
                Destroy(kvp.Value.gameObject);
        }
        _resolverCache.Clear();
    }

    //  캐릭터 설정 (편성 시스템에서 호출)
    /// <summary>
    /// 현재 메인 캐릭터의 궁극기를 설정한다.
    /// 편성이 바뀔 때마다 호출.
    /// </summary>
    public void SetCharacter(CharacterDefinitionSO charDef)
    {
        if (charDef == null)
        {
            GameLogger.LogWarning("[궁극기 실행기] CharacterDefinitionSO가 null입니다!");
            return;
        }

        // 실행 중이면 안전하게 정리
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
            presenter?.EndPresentation();
            _currentResolver?.OnCastEnd();
            _currentResolver?.SetDamageMultiplier(1f);
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
            return;
        }

        // 캐시에서 가져오거나 새로 생성
        _currentResolver = GetOrCreateResolver(charDef);
        if (_currentResolver == null)
            return;

        SetResolverActive(_currentResolver, true);
        _currentResolver.SetCasterTransform(null);

        GameLogger.Log($"[궁극기 실행기] 캐릭터 설정 완료 | {charDef.DisplayName} 궁극기={_currentData?.DisplayName}");
    }

    /// <summary>
    /// VFX 발사 기준 위치를 오버라이드한다.
    /// 지원 모드에서 지원 비주얼 Transform으로 설정하면
    /// 화살/발도 VFX가 해당 캐릭터 몸에서 나감.
    /// null이면 메인 플레이어로 복원.
    /// </summary>
    public void SetCasterOverride(Transform casterTransform)
    {
        if (_currentResolver != null)
            _currentResolver.SetCasterTransform(casterTransform);
    }
    
    public void Execute(Action onFinished, bool isSupport = false)
    {
        _onFinished = onFinished;

        // 이전 실행이 남아있으면 강제 정리
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
            presenter?.EndPresentation();
            _currentResolver?.OnCastEnd();
            _currentResolver?.SetDamageMultiplier(1f);
        }

        if (_currentData == null || _currentResolver == null)
        {
            Debug.LogError("[궁극기 실행기] 데이터 또는 Resolver가 설정되지 않았습니다!", this);
            onFinished?.Invoke();
            return;
        }

        _routine = StartCoroutine(ExecuteRoutine(isSupport));
    }

    private IEnumerator ExecuteRoutine(bool isSupport)
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

        // 연출 시작
        presenter?.BeginPresentation(_currentData, duration);

        // 궁극기 발동 신호 (Stage0Director 등에서 감지)
        RunSignals.RaiseUltimateUsed();

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
            // hitInterval이 0이면 단발 실행
            _currentResolver.ResolveHit();
        }

        // 종료
        presenter?.EndPresentation();
        _currentResolver.OnCastEnd();
        _currentResolver.SetDamageMultiplier(1f);

        GameLogger.Log($"[궁극기] 시전 종료 | {_currentData.DisplayName}");

        _routine = null;
        _onFinished?.Invoke();
    }
    
    //  Resolver 캐싱

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
                           $"캐릭터={charDef.DisplayName}", this);
            Destroy(resolverObj);
            return null;
        }

        resolver.Init(_currentData, playerTransform, enemyMask);

        if (cacheResolvers)
            _resolverCache[cacheKey] = resolver;

        return resolver;
    }

    private static void SetResolverActive(UltimateResolverBase resolver, bool active)
    {
        if (resolver == null) return;
        if (resolver.gameObject.activeSelf == active) return;
        resolver.gameObject.SetActive(active);
    }

    private static string GetResolverCacheKey(CharacterDefinitionSO charDef)
    {
        if (charDef == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(charDef.CharacterId))
            return charDef.CharacterId;
        return charDef.name;
    }

#if UNITY_EDITOR
    // 에디터에서 궁극기 범위를 기즈모로 표시.
    // 노랑 = hitRadius (적 탐색 범위)
    // 빨강 = secondaryRadius (전파/주변 피해 범위)
    // Player를 선택하면 Scene 뷰에서 보임.
    private void OnDrawGizmosSelected()
    {
        if (_currentData == null) return;

        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

        // hitRadius — 적 탐색 범위
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(center, _currentData.HitRadius);

        // secondaryRadius — 전파/주변 범위
        if (_currentData.SecondaryRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(center, _currentData.SecondaryRadius);
        }
    }
#endif
}