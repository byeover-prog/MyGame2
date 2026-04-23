using _Game.Scripts.Core.Session;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class ClearUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private KillCountSource killCountSource;
    [SerializeField] private SessionGameManager2D SessionManager;
    [SerializeField] private CharacterCatalogSO catalog;

    private VisualElement root;
    private Label gradeValue;
    private Label killValue;
    private Label timeValue;
    private Label nyangGain;
    private Label honryeongGain;
    private Label _areaLabel;
    private Label _timeLabel;
    private Label _nyangTotal;
    private Label _honryeongTotal;
    private Label _currencyNyangValue;
    private Label _currencyHonryeong;
    
    private Label _squadNameMain, _squadName1, _squadName2;
    private Label _squadLvMain, _squadLv1, _squadLv2;
    private VisualElement _squadIconMain, _squadIcon1, _squadIcon2;

    void Start()
    {
        if (uiDocument == null)
        {
            GameLogger.LogWarning("[ClearUIController] UIDocument 미연결 — Start 스킵", this);
            return;
        }

        var visualRoot = uiDocument.rootVisualElement;
        if (visualRoot == null)
        {
            GameLogger.LogWarning("[ClearUIController] rootVisualElement null — Start 스킵", this);
            return;
        }

        root          = visualRoot.Q<VisualElement>("root");
        if (root == null)
        {
            GameLogger.LogWarning("[ClearUIController] 'root' VisualElement 없음 — UXML 확인 필요", this);
            return;
        }

        gradeValue    = root.Q<Label>("grade-value");
        killValue     = root.Q<Label>("kill-value");
        timeValue     = root.Q<Label>("time-value");
        nyangGain     = root.Q<Label>("nyang-gain");
        honryeongGain = root.Q<Label>("honryeong-gain");
        _areaLabel = root.Q<Label>("area-label");
        _timeLabel = root.Q<Label>("time-label");
        _nyangTotal         = root.Q<Label>("nyang-total");
        _honryeongTotal     = root.Q<Label>("honryeong-total");
        _currencyNyangValue = root.Q<Label>("currency-nyang-value");
        _currencyHonryeong  = root.Q<Label>("currency-honryeong");
        
        // squad 라벨
        _squadNameMain   = root.Q<Label>("squad-name-main");
        _squadLvMain     = root.Q<Label>("squad-lv-main");
        _squadIconMain   = root.Q<VisualElement>("squad-icon-main");

        _squadName1      = root.Q<Label>("squad-name-1");
        _squadLv1        = root.Q<Label>("squad-lv-1");
        _squadIcon1 = visualRoot.Q<VisualElement>("squad-icon-1");

        _squadName2      = root.Q<Label>("squad-name-2");
        _squadLv2        = root.Q<Label>("squad-lv-2");
        _squadIcon2 = visualRoot.Q<VisualElement>("squad-icon-2");

        // 버튼 이벤트 연결
        var btnNext  = root.Q<Button>("btn-next");
        var btnRetry = root.Q<Button>("btn-retry");
        var btnBase  = root.Q<Button>("btn-base");

        if (btnNext  != null) btnNext.clicked  += OnNextStage;
        if (btnRetry != null) btnRetry.clicked += OnRetry;
        if (btnBase  != null) btnBase.clicked  += OnGoToBase;

        // 처음엔 숨김
        root.style.display = DisplayStyle.None;
    }
    void Update()
    {
        // F1 누르면 클리어 UI 강제 표시 (테스트용)
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SquadLoadoutRuntime.SetMain("hayeol");
            SquadLoadoutRuntime.SetSupport1("여기에 실제 CharacterId");
            SquadLoadoutRuntime.SetSupport2("여기에 실제 CharacterId");

            ShowClearUI(nyangReward: 1250, honryeongReward: 80, stageName: "경복궁 외곽 폐허");
        }
#endif
    }
    
    public void ShowClearUI(float clearTime, int nyangReward, int honryeongReward, string stageName)
    {
        if (_areaLabel != null)
            _areaLabel.text = stageName;

        if (_timeLabel != null)
        {
            int minutes = Mathf.FloorToInt(clearTime / 60f);
            int seconds = Mathf.FloorToInt(clearTime % 60f);
            _timeLabel.text = $"{minutes:00}:{seconds:00}";
        }
        // KillCountSource에서 킬 카운트 자동으로 가져오기
        int killCount = killCountSource != null ? killCountSource.KillCount : 0;

        // 등급 계산 (기준 조율 필요)
        string grade = CalculateGrade(killCount, clearTime);

        // 데이터 세팅
        killValue.text     = killCount.ToString();
        gradeValue.text    = grade;
        timeValue.text     = FormatTime(clearTime);
        nyangGain.text     = "+" + nyangReward.ToString("N0");
        honryeongGain.text = "+" + honryeongReward.ToString();
        
        int baseNyang  = CurrencyManager.Instance != null ? CurrencyManager.Instance.BaseNyang  : 0;
        int baseSpirit = CurrencyManager.Instance != null ? CurrencyManager.Instance.BaseSpirit : 0;

        if (_nyangTotal != null)
            _nyangTotal.text = baseNyang.ToString("N0");
        if (_honryeongTotal != null)
            _honryeongTotal.text = baseSpirit.ToString("N0");

        if (_currencyNyangValue != null)
            _currencyNyangValue.text = nyangReward.ToString("N0");
        if (_currencyHonryeong != null)
            _currencyHonryeong.text = $"+ 혼령 {honryeongReward}";

        // UI 표시
        root.style.display = DisplayStyle.Flex;
        root.style.opacity = 0f;

        // 페이드인 후 등급 이펙트
        DOTween.To(
            () => root.style.opacity.value,
            x  => root.style.opacity = x,
            1f, 0.4f
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => PlayGradeEffect(grade));
        ApplySquadToUI();
    }

    public void ShowClearUI(int nyangReward, int honryeongReward, string stageName)
    {
        float clearTime = SessionManager != null ? SessionManager.SessionTime : 0f;
        
        ShowClearUI(clearTime, nyangReward, honryeongReward, stageName);
    }
    
    void ApplySquadToUI()
    {
        var loadout = SquadLoadoutRuntime.Current;
        GameLogger.Log($"[Squad] mainId={loadout.mainId}, sup1={loadout.support1Id}, sup2={loadout.support2Id}");
        GameLogger.Log($"[Squad] LevelData={CharacterLevelData.Instance != null}");

        // ID → DefSO 조회
        catalog.TryFindById(loadout.mainId,     out var main);
        catalog.TryFindById(loadout.support1Id, out var sup1);
        catalog.TryFindById(loadout.support2Id, out var sup2);

        // 레벨 조회
        int lvMain = CharacterLevelData.Instance != null ? CharacterLevelData.Instance.GetLevel(loadout.mainId)     : 1;
        int lvSup1 = CharacterLevelData.Instance != null ? CharacterLevelData.Instance.GetLevel(loadout.support1Id) : 1;
        int lvSup2 = CharacterLevelData.Instance != null ? CharacterLevelData.Instance.GetLevel(loadout.support2Id) : 1;

        // 레벨 내림차순 정렬 (메인 고정, 지원1/2만 정렬)
        var supports = new (CharacterDefinitionSO def, int lv)[]
        {
            (sup1, lvSup1),
            (sup2, lvSup2),
        };
        System.Array.Sort(supports, (a, b) => b.lv.CompareTo(a.lv));

        // 메인 세팅
        SetSquadSlot(_squadIconMain, _squadNameMain, _squadLvMain, main, lvMain);

        // 지원 세팅 (정렬 후)
        SetSquadSlot(_squadIcon1, _squadName1, _squadLv1, supports[0].def, supports[0].lv);
        SetSquadSlot(_squadIcon2, _squadName2, _squadLv2, supports[1].def, supports[1].lv);
    }

    void SetSquadSlot(VisualElement icon, Label nameLabel, Label lvLabel,
        CharacterDefinitionSO def, int level)
    {
        if (nameLabel != null)
            nameLabel.text = def != null ? def.DisplayName : "-";

        if (lvLabel != null)
            lvLabel.text = def != null ? $"Lv.{level}" : "";

        if (icon != null)
        {
            var img = def?.Thumbnail != null ? def.Thumbnail : def?.Portrait;
            if (img != null)
                icon.style.backgroundImage = new StyleBackground(img);
        }
    }

    // 등급 계산 (기준은 조율 필요)
    string CalculateGrade(int killCount, float clearTime)
    {
        // 임시 기준 - 나중에 수정
        if (clearTime <= 600f) return "S";
        if (clearTime <= 900f) return "A";
        if (clearTime <= 1200f) return "B";
        return "C";
    }

    // 등급 이펙트
    void PlayGradeEffect(string grade)
    {
        Color brightColor = grade switch
        {
            "S" => new Color(1f,    0.98f, 0.7f),
            "A" => new Color(0.9f,  0.78f, 0.4f),
            "B" => new Color(0.6f,  0.85f, 1f),
            "C" => new Color(0.75f, 0.75f, 0.75f),
            _   => new Color(1f,    0.98f, 0.7f)
        };

        Color normalColor = new Color(0.88f, 0.82f, 0.63f);

        // S등급일 때만 반짝임
        if (grade != "S") return;

        // 크기 팝업
        gradeValue.style.scale = new Scale(new Vector2(0.3f, 0.3f));
        DOTween.To(
            () => gradeValue.style.scale.value.value.x,
            x  => gradeValue.style.scale = new Scale(new Vector2(x, x)),
            1.2f, 0.35f
        ).SetEase(Ease.OutBack)
         .OnComplete(() =>
         {
             DOTween.To(
                 () => gradeValue.style.scale.value.value.x,
                 x  => gradeValue.style.scale = new Scale(new Vector2(x, x)),
                 1f, 0.15f
             ).SetEase(Ease.InOutQuad)
              .OnComplete(() => PlayColorPulse(brightColor, normalColor));
         });
    }

    // 색상 펄스
    void PlayColorPulse(Color brightColor, Color normalColor)
    {
        Sequence colorSeq = DOTween.Sequence();
        colorSeq.Append(
            DOTween.To(
                () => gradeValue.style.color.value,
                x  => gradeValue.style.color = new StyleColor(x),
                brightColor, 0.25f
            ).SetEase(Ease.OutQuad)
        );
        colorSeq.Append(
            DOTween.To(
                () => gradeValue.style.color.value,
                x  => gradeValue.style.color = new StyleColor(x),
                normalColor, 0.4f
            ).SetEase(Ease.InQuad)
        );
        colorSeq.SetLoops(3, LoopType.Restart);
    }

    // 버튼 이벤트
    void OnNextStage()
    {
        CurrencyManager.Instance?.SaveStageClearRewards();
        FadeOutAndDo(() => GameLogger.Log("다음 관문으로 이동"));
        // SceneManager.LoadScene("NextStage");
    }

    void OnRetry()
    {
        CurrencyManager.Instance?.SaveStageClearRewards();
        FadeOutAndDo(() => GameLogger.Log("재도전"));
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGoToBase()
    {
        CurrencyManager.Instance?.SaveStageClearRewards();
        FadeOutAndDo(() => GameLogger.Log("본거지로 귀환"));
        // SceneManager.LoadScene("Base");
    }

    // 페이드 아웃
    void FadeOutAndDo(System.Action callback)
    {
        DOTween.To(
            () => root.style.opacity.value,
            x  => root.style.opacity = x,
            0f, 0.3f
        ).SetEase(Ease.InQuad)
         .OnComplete(() =>
         {
             root.style.display = DisplayStyle.None;
             callback?.Invoke();
         });
    }

    // 유틸
    string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m:00}:{s:00}";
    }

    void OnDestroy()
    {
        DOTween.Kill(gradeValue);
        DOTween.Kill(root);
    }
}