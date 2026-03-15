using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class ClearUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

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

    // 클리어 UI 표시
    public void ShowClearUI(int killCount, string grade, float clearTime,
                            int nyangReward, int honryeongReward)
    {
        killValue.text     = killCount.ToString();
        gradeValue.text    = grade;
        timeValue.text     = FormatTime(clearTime);
        nyangGain.text     = "+" + nyangReward.ToString("N0");
        honryeongGain.text = "+" + honryeongReward.ToString();

        root.style.display = DisplayStyle.Flex;
        root.style.opacity = 0f;

        // 전체 페이드인 후 등급 이펙트
        DOTween.To(
            () => root.style.opacity.value,
            x  => root.style.opacity = x,
            1f, 0.4f
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => PlayGradeEffect(grade));
    }

    // 등급 이펙트 (텍스트만)
    void PlayGradeEffect(string grade)
    {
        // 등급별 밝은 색상
        Color brightColor = grade switch
        {
            "S" => new Color(1f,    0.98f, 0.7f),   // 밝은 골드
            "A" => new Color(0.9f,  0.78f, 0.4f),   // 골드
            "B" => new Color(0.6f,  0.85f, 1f),     // 블루
            "C" => new Color(0.75f, 0.75f, 0.75f),  // 그레이
            _   => new Color(1f,    0.98f, 0.7f)
        };

        Color normalColor = new Color(0.88f, 0.82f, 0.63f); // #e8d4a0

        // 1. 크기 팝업
        gradeValue.style.scale = new Scale(new Vector2(0.3f, 0.3f));

        DOTween.To(
            () => gradeValue.style.scale.value.value.x,
            x  => gradeValue.style.scale = new Scale(new Vector2(x, x)),
            1.2f, 0.35f
        ).SetEase(Ease.OutBack)
         .OnComplete(() =>
         {
             // 원래 크기로
             DOTween.To(
                 () => gradeValue.style.scale.value.value.x,
                 x  => gradeValue.style.scale = new Scale(new Vector2(x, x)),
                 1f, 0.15f
             ).SetEase(Ease.InOutQuad)
              .OnComplete(() => PlayColorPulse(brightColor, normalColor));
         });
    }

    // 색상 펄스 (크기 팝업 끝난 후 실행)
    void PlayColorPulse(Color brightColor, Color normalColor)
    {
        Sequence colorSeq = DOTween.Sequence();

        // 밝게
        colorSeq.Append(
            DOTween.To(
                () => gradeValue.style.color.value,
                x  => gradeValue.style.color = new StyleColor(x),
                brightColor, 0.25f
            ).SetEase(Ease.OutQuad)
        );
        // 원래 색으로
        colorSeq.Append(
            DOTween.To(
                () => gradeValue.style.color.value,
                x  => gradeValue.style.color = new StyleColor(x),
                normalColor, 0.4f
            ).SetEase(Ease.InQuad)
        );

        // 3회 반복
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
