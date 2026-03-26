using UnityEngine;

/// <summary>
/// 레벨업 흐름의 실제 적용만 담당합니다.
/// - 열기 요청 수신
/// - 오퍼 4장 요청
/// - 카드 선택 적용
/// - 닫힘 신호 발행
/// </summary>
[DisallowMultipleComponent]
public sealed class LevelUpOrchestrator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private OfferService offerService;
    [SerializeField] private SkillRuntimeState runtimeState;
    [SerializeField] private SkillRunner skillRunner;

    [Header("레벨업 설정")]
    [SerializeField, Min(1)] private int offerCount = 4;
    [SerializeField] private bool pauseTime = true;

    [Header("디버그")]
    [SerializeField] private bool enableLogs = true;

    private bool isOpen;
    private bool isPicking;
    private bool pauseAcquired;

    private void Awake()
    {
        if (offerService == null) offerService = FindFirstObjectByType<OfferService>();
        if (runtimeState == null) runtimeState = FindFirstObjectByType<SkillRuntimeState>();
        if (skillRunner == null) skillRunner = FindFirstObjectByType<SkillRunner>();
    }

    private void OnEnable()
    {
        GameSignals.LevelUpOpenRequested += HandleOpen;
        GameSignals.OffersReady += HandleOffersReady;
        GameSignals.OfferPicked += HandleOfferPicked;
    }

    private void OnDisable()
    {
        GameSignals.LevelUpOpenRequested -= HandleOpen;
        GameSignals.OffersReady -= HandleOffersReady;
        GameSignals.OfferPicked -= HandleOfferPicked;
        ReleasePause();
    }

    private void HandleOpen()
    {
        if (isOpen)
            return;

        isOpen = true;
        isPicking = false;

        if (pauseTime)
            AcquirePause();

        if (offerService == null)
        {
            Debug.LogError("[LevelUp] OfferService 참조가 없습니다.", this);
            Close();
            return;
        }

        offerService.Bind(runtimeState);
        offerService.RequestOffers(offerCount);

        if (enableLogs)
            Debug.Log($"[LevelUp] OpenRequested => {offerCount}장 요청", this);
    }

    private void HandleOffersReady(Offer[] offers)
    {
        if (!isOpen)
            return;

        if (offers == null || offers.Length <= 0)
        {
            if (enableLogs)
                Debug.Log("[LevelUp] 후보가 없어 즉시 닫습니다.", this);

            Close();
            return;
        }

        if (enableLogs)
            Debug.Log($"[LevelUp] OffersReady => {offers.Length}장", this);
    }

    private void HandleOfferPicked(Offer picked)
    {
        if (!isOpen)
            return;

        if (isPicking)
            return;

        isPicking = true;

        try
        {
            if (string.IsNullOrWhiteSpace(picked.id))
            {
                Debug.LogWarning("[LevelUp] 선택된 카드 id가 비어 있습니다.", this);
                return;
            }

            int newLevel = 1;
            if (runtimeState != null)
                newLevel = runtimeState.GrantOrLevelUp(picked.kind, picked.id);

            if (newLevel <= 0)
            {
                Debug.LogWarning($"[LevelUp] 카드 적용 실패 => {picked.id}", this);
                return;
            }

            GameSignals.RaiseSkillLevelChanged(picked.id, newLevel);

            if (newLevel == 1 && picked.prefab != null && skillRunner != null)
                skillRunner.AttachSkillPrefab(picked.id, picked.prefab);

            if (skillRunner != null)
                skillRunner.ApplyLevel(picked.id, newLevel);

            if (enableLogs)
                Debug.Log($"[LevelUp] Picked '{picked.id}' => Lv.{newLevel}", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LevelUp] HandleOfferPicked 예외: {e.Message}\n{e.StackTrace}", this);
        }
        finally
        {
            Close();
            isPicking = false;
        }
    }

    private void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        ReleasePause();
        GameSignals.RaiseLevelUpClosed();

        if (enableLogs)
            Debug.Log("[LevelUp] Closed", this);
    }

    private void AcquirePause()
    {
        if (pauseAcquired)
            return;

        GamePauseGate2D.Acquire(this);
        pauseAcquired = true;
    }

    private void ReleasePause()
    {
        if (!pauseAcquired)
            return;

        GamePauseGate2D.Release(this);
        pauseAcquired = false;
    }
}