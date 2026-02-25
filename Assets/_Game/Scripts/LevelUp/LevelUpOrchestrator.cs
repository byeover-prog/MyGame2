// UTF-8
using UnityEngine;

/// <summary>
/// 레벨업 오픈/픽/클로즈 흐름만 담당(SRP).
/// - 시간정지는 여기서만(Time.timeScale)
/// - 오퍼 생성은 OfferService
/// - 상태는 SkillRuntimeState
/// - 프리팹 장착/레벨 적용은 SkillRunner
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOrchestrator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private OfferService offerService;
    [SerializeField] private SkillRuntimeState runtimeState;
    [SerializeField] private SkillRunner skillRunner;

    [Header("시간 정지")]
    [SerializeField] private bool pauseTime = true;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = true;

    private bool _isOpen;
    private bool _isPicking;
    private float _prevTimeScale = 1f;

    private void Awake()
    {
        // 실수 방지: 인스펙터 누락 시 자동 탐색
        if (offerService == null) offerService = FindFirstObjectByType<OfferService>();
        if (runtimeState == null) runtimeState = FindFirstObjectByType<SkillRuntimeState>();
        if (skillRunner == null) skillRunner = FindFirstObjectByType<SkillRunner>();
    }

    private void OnEnable()
    {
        // GameSignals 이벤트 이름은 네 코드에 맞춰 이미 확정된 상태
        GameSignals.LevelUpOpenRequested += HandleOpen;
        GameSignals.OfferPicked += HandleOfferPicked;
        GameSignals.OffersReady += HandleOffersReady;
    }

    private void OnDisable()
    {
        GameSignals.LevelUpOpenRequested -= HandleOpen;
        GameSignals.OfferPicked -= HandleOfferPicked;
        GameSignals.OffersReady -= HandleOffersReady;
    }

    private void HandleOpen()
    {
        if (_isOpen) return;

        _isOpen = true;
        _isPicking = false;

        if (pauseTime)
        {
            // 이미 0이면 원래 값이 무엇이었는지 알 수 없으니 1로 가정
            _prevTimeScale = (Time.timeScale > 0f) ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        if (enableLogs)
            Debug.Log("[LevelUp] OpenRequested", this);

        if (offerService == null)
        {
            Debug.LogError("[LevelUp] OfferService 참조가 없습니다.", this);
            Close(); // 멈춤 방지
            return;
        }

        // 오퍼 생성 서비스에 상태 연결 후 요청
        offerService.Bind(runtimeState);
        offerService.RequestOffers();
    }

    private void HandleOffersReady(Offer[] offers)
    {
        if (!_isOpen) return;

        // 후보가 없으면 즉시 닫아서 TimeScale이 남지 않게 한다.
        if (offers == null || offers.Length <= 0)
        {
            if (enableLogs)
                Debug.Log("[LevelUp] OffersReady(0) => Close", this);

            Close();
            return;
        }

        // offers > 0 인 경우:
        // - 패널 표시/카드 바인딩은 LevelUpPanelController/OfferPanelView가 처리(신호 기반)한다고 가정
        // - 여기서는 닫지 않는다.
        if (enableLogs)
            Debug.Log($"[LevelUp] OffersReady => {offers.Length}장", this);
    }

    private void HandleOfferPicked(Offer picked)
    {
        if (!_isOpen) return;
        if (_isPicking) return; // 중복 클릭 방지
        _isPicking = true;

        try
        {
            if (string.IsNullOrWhiteSpace(picked.id))
            {
                Debug.LogWarning("[LevelUp] OfferPicked 되었지만 id가 비어있습니다.", this);
                return;
            }

            // 1) 런타임 상태 +1 (첫 획득이면 1)
            // runtimeState가 없다면 최소 동작 보장: 첫 장착 레벨=1로 처리
            int newLv = 1;
            if (runtimeState != null)
                newLv = runtimeState.GrantOrLevelUp(picked.kind, picked.id);

            GameSignals.RaiseSkillLevelChanged(picked.id, newLv);

            if (enableLogs)
                Debug.Log($"[LevelUp] Picked '{picked.id}' => Lv.{newLv}", this);

            // 2) 첫 획득이면 프리팹 장착
            if (newLv == 1 && picked.prefab != null && skillRunner != null)
            {
                skillRunner.AttachSkillPrefab(picked.id, picked.prefab);

                if (enableLogs)
                    Debug.Log($"[LevelUp] Attach '{picked.id}'", this);
            }

            // 3) 레벨업 적용(수치 갱신)
            if (skillRunner != null)
                skillRunner.ApplyLevel(picked.id, newLv);

            if (enableLogs)
                Debug.Log($"[LevelUp] ApplyLevel '{picked.id}' => Lv.{newLv}", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelUp] HandleOfferPicked 예외: {e.Message}\n{e.StackTrace}", this);
        }
        finally
        {
            // 어떤 실패/예외가 있어도 timeScale 복구 + 패널 닫힘 보장
            Close();
            _isPicking = false;
        }
    }

    private void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (pauseTime)
        {
            float restore = (_prevTimeScale > 0f) ? _prevTimeScale : 1f;
            Time.timeScale = restore;
        }

        if (enableLogs)
            Debug.Log("[LevelUp] Closed", this);

        GameSignals.RaiseLevelUpClosed();
    }
}