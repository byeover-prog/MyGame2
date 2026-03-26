// UTF-8
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 공용 궁극기 실행기.
/// CharacterDefinitionSO에서 데이터를 읽고, Presenter + Resolver를 조합하여 실행.
/// 모든 캐릭터가 이 1개 클래스를 공용으로 사용한다.
///
/// [Hierarchy 위치]
/// Player 오브젝트 아래 자식으로 배치.
///
/// [동작 흐름]
/// SetCharacter(SO) → Execute(onFinished, isSupport)
///   → Resolver.OnCastBegin()
///   → Presenter.BeginPresentation()
///   → 루프: Resolver.ResolveHit() × hitInterval
///   → Presenter.EndPresentation()
///   → Resolver.OnCastEnd()
///   → onFinished
///
/// [캐릭터 교체]
/// 메인 캐릭터가 바뀌면 SetCharacter()만 다시 호출하면 됨.
/// Resolver 프리팹이 자동으로 교체됨.
/// </summary>
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

    // ── 런타임 ──
    private CharacterDefinitionSO _currentCharacter;
    private UltimateDataSO _currentData;
    private UltimateResolverBase _currentResolver;
    private Coroutine _routine;
    private Action _onFinished;

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

    // ═══════════════════════════════════════════════════════
    //  캐릭터 설정 (편성 시스템에서 호출)
    // ═══════════════════════════════════════════════════════

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

        _currentCharacter = charDef;
        _currentData = charDef.UltimateData;

        // 기존 Resolver 정리
        if (_currentResolver != null)
        {
            Destroy(_currentResolver.gameObject);
            _currentResolver = null;
        }

        // 새 Resolver 프리팹 생성
        if (charDef.UltimateResolverPrefab != null)
        {
            GameObject resolverObj = Instantiate(
                charDef.UltimateResolverPrefab,
                transform
            );
            resolverObj.name = $"UltResolver_{charDef.CharacterId}";

            _currentResolver = resolverObj.GetComponent<UltimateResolverBase>();
            if (_currentResolver == null)
            {
                Debug.LogError($"[궁극기 실행기] Resolver 프리팹에 UltimateResolverBase 컴포넌트가 없습니다! " +
                               $"캐릭터={charDef.DisplayName}", this);
                Destroy(resolverObj);
                return;
            }

            _currentResolver.Init(_currentData, playerTransform, enemyMask);

            GameLogger.Log($"[궁극기 실행기] 캐릭터 설정 완료 | {charDef.DisplayName} " +
                      $"궁극기={_currentData?.DisplayName}");
        }
        else
        {
            GameLogger.LogWarning($"[궁극기 실행기] {charDef.DisplayName}의 Resolver 프리팹이 비어있습니다.", this);
        }
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

    // ═══════════════════════════════════════════════════════
    //  실행
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 궁극기 실행. UltimateController2D에서 호출.
    /// </summary>
    /// <param name="onFinished">종료 시 콜백</param>
    /// <param name="isSupport">true면 지원 모드 (데미지 감소)</param>
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

        // 1. 연출 시작
        presenter?.BeginPresentation(_currentData, duration);

        GameLogger.Log($"[궁극기] 시전 시작 | {_currentData.DisplayName} " +
                  $"지원={isSupport} 배율={damageMultiplier:F2} duration={duration}s");

        // 2. 첫 피해 대기
        yield return new WaitForSeconds(hitDelay);

        // 3. 지속 시간 동안 반복 피해
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
            // hitInterval이 0이면 단발 실행 (하린처럼)
            _currentResolver.ResolveHit();
        }

        // 4. 종료
        presenter?.EndPresentation();
        _currentResolver.OnCastEnd();
        _currentResolver.SetDamageMultiplier(1f);

        GameLogger.Log($"[궁극기] 시전 종료 | {_currentData.DisplayName}");

        _routine = null;
        _onFinished?.Invoke();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 궁극기 범위를 기즈모로 표시.
    /// 노랑 = hitRadius (적 탐색 범위)
    /// 빨강 = secondaryRadius (전파/주변 피해 범위)
    /// Player를 선택하면 Scene 뷰에서 보임.
    /// </summary>
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