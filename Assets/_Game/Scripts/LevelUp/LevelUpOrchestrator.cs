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
    private float _prevTimeScale = 1f;

    private void OnEnable()
    {
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

        if (pauseTime)
        {
            _prevTimeScale = (Time.timeScale > 0f) ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        if (enableLogs)
            Debug.Log("[LevelUp] OpenRequested", this);

        if (offerService != null) offerService.Bind(runtimeState);
        if (offerService != null) offerService.RequestOffers();
        else Debug.LogError("[LevelUp] OfferService 참조가 없습니다.", this);
    }

    private void HandleOffersReady(Offer[] offers)
    {
        // 후보가 없어서 OffersReady([])가 왔으면, 게임이 멈춘 채로 남지 않게 즉시 닫는다.
        if (!_isOpen) return;

        if (offers == null || offers.Length <= 0)
        {
            if (enableLogs)
                Debug.Log("[LevelUp] OffersReady(0) => Close", this);

            Close();
        }
    }

    private void HandleOfferPicked(Offer picked)
    {
        if (!_isOpen) return;

        try
        {
            if (string.IsNullOrWhiteSpace(picked.id))
            {
                Debug.LogWarning("[LevelUp] OfferPicked 되었지만 id가 비어있습니다.", this);
                return;
            }

            // 1) 런타임 상태 +1 (첫 획득이면 1)
            int newLv = (runtimeState != null)
                ? runtimeState.GrantOrLevelUp(picked.kind, picked.id)
                : 0;

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
        finally
        {
            // 어떤 실패/예외가 있어도 timeScale 복구 + 패널 닫힘 보장
            Close();
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