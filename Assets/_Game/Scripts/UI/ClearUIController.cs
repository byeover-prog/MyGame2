using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class ClearUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private KillCountSource killCountSource;

    private VisualElement root;
    private Label gradeValue;
    private Label killValue;
    private Label timeValue;
    private Label nyangGain;
    private Label honryeongGain;

    void Start()
    {
        root          = uiDocument.rootVisualElement.Q<VisualElement>("root");
        gradeValue    = root.Q<Label>("grade-value");
        killValue     = root.Q<Label>("kill-value");
        timeValue     = root.Q<Label>("time-value");
        nyangGain     = root.Q<Label>("nyang-gain");
        honryeongGain = root.Q<Label>("honryeong-gain");

        // 버튼 이벤트 연결
        root.Q<Button>("btn-next").clicked  += OnNextStage;
        root.Q<Button>("btn-retry").clicked += OnRetry;
        root.Q<Button>("btn-base").clicked  += OnGoToBase;

        // 처음엔 숨김
        root.style.display = DisplayStyle.None;
    }
    void Update()
    {
        // F1 누르면 클리어 UI 강제 표시 (테스트용)
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ShowClearUI(clearTime: 1458f, nyangReward: 1250, honryeongReward: 80);
        }
#endif
    }

    // 외부에서 호출
    // GameManager나 스테이지 클리어 조건에서 이렇게 호출
    // clearUIController.ShowClearUI(clearTime: 1458f, nyangReward: 1250, honryeongReward: 80);
    public void ShowClearUI(float clearTime, int nyangReward, int honryeongReward)
    {
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
        FadeOutAndDo(() => Debug.Log("다음 관문으로 이동"));
        // SceneManager.LoadScene("NextStage");
    }

    void OnRetry()
    {
        FadeOutAndDo(() => Debug.Log("재도전"));
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnGoToBase()
    {
        FadeOutAndDo(() => Debug.Log("본거지로 귀환"));
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
