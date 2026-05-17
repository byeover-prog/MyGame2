using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class UltimateHUDController : MonoBehaviour
{
    [Header("Scene Context")]
    [SerializeField] private GameSceneContext sceneContext;

    // ══════════════════════════════════════════════
    //  참조
    // ══════════════════════════════════════════════

    [Header("편성 참조")]
    [Tooltip("스쿼드 편성 정보입니다. 캐릭터별 궁극기 아이콘을 자동으로 가져올 때 사용합니다.\n" +
             "비워두면 Scene에서 자동으로 찾습니다.")]
    [SerializeField] private SquadLoadout2D loadout;

    // ══════════════════════════════════════════════
    //  R키 - 메인 궁극기
    // ══════════════════════════════════════════════

    [Header("R키 궁극기 (메인 캐릭터)")]
    [Tooltip("메인 캐릭터의 궁극기 컨트롤러입니다. 비워두면 Scene에서 자동 탐색.")]
    [SerializeField] private UltimateController2D ultimateController;

    [Tooltip("R 슬롯에 표시되는 메인 캐릭터의 궁극기 아이콘 Image입니다. (다이아몬드 모양)")]
    [SerializeField] private Image icon_R;

    [Tooltip("R 슬롯 쿨다운 필 Image입니다. (Image Type=Filled, Radial360 추천)")]
    [SerializeField] private Image cooldownFill_R;

    [Tooltip("R 슬롯 쿨다운 숫자 텍스트입니다. (선택)")]
    [SerializeField] private TextMeshProUGUI text_R;

    // ══════════════════════════════════════════════
    //  T키 - 지원 궁극기 (좌우 분할)
    // ══════════════════════════════════════════════

    [Header("T키 궁극기 (지원 캐릭터)")]
    [Tooltip("지원 궁극기 컨트롤러입니다. 비워두면 Scene에서 자동 탐색.")]
    [SerializeField] private SupportUltimateController2D supportController;

    [Tooltip("T 슬롯 왼쪽 - 지원1 캐릭터의 궁극기 아이콘 Image입니다. (다이아몬드 모양)")]
    [SerializeField] private Image icon_T_Left;

    [Tooltip("T 슬롯 오른쪽 - 지원2 캐릭터의 궁극기 아이콘 Image입니다. (다이아몬드 모양)")]
    [SerializeField] private Image icon_T_Right;

    [Tooltip("T 슬롯 공통 쿨다운 필 Image입니다. (좌우 아이콘 전체를 덮는 형태)")]
    [SerializeField] private Image cooldownFill_T;

    [Tooltip("T 슬롯 쿨다운 숫자 텍스트입니다. (선택)")]
    [SerializeField] private TextMeshProUGUI text_T;

    // ══════════════════════════════════════════════
    //  색상 설정
    // ══════════════════════════════════════════════

    [Header("준비 상태 색상")]
    [Tooltip("준비 완료 시 아이콘 색상입니다.")]
    [SerializeField] private Color readyColor = Color.white;

    [Tooltip("쿨다운 중일 때 아이콘 색상(어둡게).")]
    [SerializeField] private Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    // ══════════════════════════════════════════════
    //  유니티 라이프사이클
    // ══════════════════════════════════════════════

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void OnEnable()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged += RefreshIcons;

        RefreshIcons();
    }

    private void Start()
    {
        // 씬 로딩 순서상 OnEnable 시점에 편성이 완성되지 않았을 수 있어 한 번 더 갱신
        RefreshIcons();
    }

    private void OnDisable()
    {
        if (loadout != null)
            loadout.OnLoadoutChanged -= RefreshIcons;
    }

    private void Update()
    {
        UpdateRSlot();
        UpdateTSlot();
    }

    // ══════════════════════════════════════════════
    //  내부 로직
    // ══════════════════════════════════════════════

    private void AutoResolveReferences()
    {
        ResolveSceneContext();

        if (loadout == null && sceneContext != null)
            loadout = sceneContext.GetPlayerComponent<SquadLoadout2D>();

        if (loadout == null)
            loadout = FindFirstObjectByType<SquadLoadout2D>();

        if (ultimateController == null && sceneContext != null)
            ultimateController = sceneContext.GetPlayerComponent<UltimateController2D>();

        if (ultimateController == null)
            ultimateController = FindFirstObjectByType<UltimateController2D>();

        if (supportController == null && sceneContext != null)
            supportController = sceneContext.GetPlayerComponent<SupportUltimateController2D>();

        if (supportController == null)
            supportController = FindFirstObjectByType<SupportUltimateController2D>();
    }

    private void ResolveSceneContext()
    {
        if (sceneContext == null)
            sceneContext = FindFirstObjectByType<GameSceneContext>(FindObjectsInactive.Include);

        if (sceneContext != null)
            sceneContext.ResolveMissingReferences();
    }

    /// <summary>
    /// 편성 정보에서 각 캐릭터의 다이아몬드용 궁극기 아이콘을 가져와 UI에 반영한다.
    /// 다이아몬드 전용 아이콘이 없으면 UltimateSkillIcon으로 자동 fallback.
    /// 편성이 바뀔 때 SquadLoadout2D.OnLoadoutChanged 이벤트로 자동 호출된다.
    /// </summary>
    public void RefreshIcons()
    {
        if (loadout == null)
        {
            return;
        }

        // 메인 → R키 (다이아몬드 슬롯이므로 Diamond 프로퍼티 사용)
        SetIconSprite(icon_R, loadout.Main != null ? loadout.Main.UltimateSkillIconDiamond : null);

        // 지원1 → T키 왼쪽
        SetIconSprite(icon_T_Left, loadout.Support1 != null ? loadout.Support1.UltimateSkillIconDiamond : null);

        // 지원2 → T키 오른쪽
        SetIconSprite(icon_T_Right, loadout.Support2 != null ? loadout.Support2.UltimateSkillIconDiamond : null);
    }

    private static void SetIconSprite(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private void UpdateRSlot()
    {
        float remaining = ultimateController != null ? ultimateController.CooldownRemaining : 0f;
        float total = ultimateController != null ? ultimateController.CooldownTotal : 1f;
        bool isReady = ultimateController != null && ultimateController.IsReady;

        if (cooldownFill_R != null)
            cooldownFill_R.fillAmount = isReady ? 0f : (total > 0f ? remaining / total : 0f);

        if (icon_R != null)
            icon_R.color = isReady ? readyColor : cooldownColor;

        if (text_R != null)
            text_R.text = isReady ? string.Empty : Mathf.CeilToInt(remaining).ToString();
    }

    private void UpdateTSlot()
    {
        float remaining = supportController != null ? supportController.CooldownRemaining : 0f;
        float total = supportController != null ? supportController.CooldownTotal : 1f;
        bool isReady = supportController != null && supportController.IsReady;

        if (cooldownFill_T != null)
            cooldownFill_T.fillAmount = isReady ? 0f : (total > 0f ? remaining / total : 0f);

        Color targetColor = isReady ? readyColor : cooldownColor;

        if (icon_T_Left != null)
            icon_T_Left.color = targetColor;

        if (icon_T_Right != null)
            icon_T_Right.color = targetColor;

        if (text_T != null)
            text_T.text = isReady ? string.Empty : Mathf.CeilToInt(remaining).ToString();
    }
}
