using UnityEngine;

public sealed class LevelUpOrchestrator : MonoBehaviour
{
    [SerializeField] private OfferService offerService;
    [SerializeField] private SkillRuntimeState runtimeState;
    [SerializeField] private SkillRunner skillRunner;

    [Header("시간 정지")]
    [SerializeField] private bool pauseTime = true;

    private void OnEnable()
    {
        GameSignals.OfferPicked += HandleOfferPicked;
        GameSignals.LevelUpOpenRequested += HandleOpen;
    }

    private void OnDisable()
    {
        GameSignals.OfferPicked -= HandleOfferPicked;
        GameSignals.LevelUpOpenRequested -= HandleOpen;
    }

    private void HandleOpen()
    {
        if (pauseTime) Time.timeScale = 0f;

        offerService.Bind(runtimeState);
        offerService.RequestOffers(3);
    }

    private void HandleOfferPicked(Offer picked)
    {
        // 런타임 상태 +1 (첫 획득이면 1)
        int newLv = runtimeState.GrantOrLevelUp(picked.id);
        GameSignals.RaiseSkillLevelChanged(picked.id, newLv);

        // 첫 획득이면 프리팹 장착
        if (newLv == 1)
        {
            skillRunner.AttachSkillPrefab(picked.id, picked.skillPrefab);
        }

        // 레벨업 적용(수치 갱신)
        skillRunner.ApplyLevel(picked.id, newLv);

        if (pauseTime) Time.timeScale = 1f;
        GameSignals.RaiseLevelUpClosed();
    }
}