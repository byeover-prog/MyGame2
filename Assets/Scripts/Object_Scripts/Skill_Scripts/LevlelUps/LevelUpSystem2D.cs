using UnityEngine;

public sealed class LevelUpSystem2D : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private WeaponShooterSystem2D shooter;
    [SerializeField] private LevelUpCardPicker picker;
    [SerializeField] private SkillLevelUpOfferBuilder2D offerBuilder;
    [SerializeField] private SkillLevelRuntimeState2D levelState;

    [Header("UI 루트(캔버스 안 LevelUpPanel)")]
    [SerializeField] private GameObject panelRoot;

    [Header("시간 정지")]
    [SerializeField] private bool pauseTimeWhenOpen = true;

    private void Awake()
    {
        if (shooter == null) shooter = FindFirstObjectByType<WeaponShooterSystem2D>();
        if (picker == null) picker = FindFirstObjectByType<LevelUpCardPicker>();
        if (offerBuilder == null) offerBuilder = FindFirstObjectByType<SkillLevelUpOfferBuilder2D>();
        if (levelState == null) levelState = FindFirstObjectByType<SkillLevelRuntimeState2D>();

        // panelRoot를 비워뒀으면 LevelUpPanelController에서 루트를 가져오거나, 이름으로 찾는다
        if (panelRoot == null)
        {
            var p = FindFirstObjectByType<LevelUpPanelController>();
            if (p != null) panelRoot = p.gameObject;
        }

        // 시작 시 닫아두기(원하면 제거)
        ClosePanel();
    }

    public void TriggerLevelUp()
    {
        if (shooter == null || picker == null || offerBuilder == null || levelState == null) return;

        OpenPanel();

        var offers = offerBuilder.BuildOffers(shooter, levelState, 3);

        if (offers == null || offers.Count == 0)
            Debug.LogWarning("[LevelUpSystem2D] offers가 비었습니다. 트랙/슬롯/레벨 상태를 확인하세요.");

        picker.OpenWithOffers(
            offers,
            onPicked: (card) =>
            {
                levelState.IncreaseLevel(card.slotIndex, 1);
                ClosePanel();
            },
            getDisplayLevel: (slotIndex) =>
            {
                return levelState.GetLevel(slotIndex);
            }
        );
    }

    private void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (pauseTimeWhenOpen) Time.timeScale = 0f;
    }

    private void ClosePanel()
    {
        if (pauseTimeWhenOpen) Time.timeScale = 1f;
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
