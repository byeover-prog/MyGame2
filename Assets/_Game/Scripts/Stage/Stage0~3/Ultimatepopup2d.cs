// UTF-8
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 궁극기 유도 팝업 UI.
///
/// [Hierarchy 구조]
/// Canvas (Screen Space - Camera, Sort Order 60)
/// └─ UltimatePopupPanel
///     ├─ BG (Image, 반투명 검정)
///     ├─ MainText (TMP — "지금이야!")
///     └─ SubText  (TMP — "궁극기로 쓸어버려라!")
///
/// [Inspector 연결]
/// - popupPanel : UltimatePopupPanel 오브젝트
/// - mainText   : MainText TMP
/// - subText    : SubText TMP
/// </summary>
public class UltimatePopup2D : MonoBehaviour
{
    [Header("=== UI 연결 ===")]
    [Tooltip("팝업 루트 패널")]
    [SerializeField] GameObject popupPanel;

    [Tooltip("메인 텍스트 (예: 지금이야!)")]
    [SerializeField] TextMeshProUGUI mainText;

    [Tooltip("서브 텍스트 (예: 궁극기로 쓸어버려라!)")]
    [SerializeField] TextMeshProUGUI subText;

    [Header("=== 텍스트 내용 ===")]
    [SerializeField] string mainMessage = "지금이야!";
    [SerializeField] string subMessage  = "궁극기로 쓸어버려라!";

    [Header("=== 연출 설정 ===")]
    [Tooltip("페이드 인 시간 (초)")]
    [SerializeField] float fadeInDuration = 0.4f;

    [Tooltip("표시 후 펄스 효과 여부")]
    [SerializeField] bool pulseEffect = true;

    [Tooltip("펄스 속도")]
    [SerializeField] float pulseSpeed = 2.0f;

    // ─── 내부 상태 ──────────────────────────────────

    CanvasGroup _canvasGroup;
    bool _showing;
    Coroutine _pulseCoroutine;

    // ─── 유니티 ─────────────────────────────────────

    void Awake()
    {
        // CanvasGroup 자동 생성
        if (popupPanel != null)
        {
            _canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = popupPanel.AddComponent<CanvasGroup>();
        }

        // 시작 시 숨김
        SetAlpha(0f);
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    void Start()
    {
        // 텍스트 초기 설정
        if (mainText != null) mainText.text = mainMessage;
        if (subText  != null) subText.text  = subMessage;
    }

    // ─── 외부 API ────────────────────────────────────

    /// <summary>팝업을 표시한다.</summary>
    public void Show()
    {
        if (_showing) return;
        _showing = true;

        if (popupPanel != null) popupPanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    /// <summary>팝업을 숨긴다.</summary>
    public void Hide()
    {
        if (!_showing) return;
        _showing = false;

        StopAllCoroutines();
        SetAlpha(0f);
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    // ─── 내부 연출 ───────────────────────────────────

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        SetAlpha(1f);

        if (pulseEffect)
            _pulseCoroutine = StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        while (true)
        {
            float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI);
            SetAlpha(alpha);
            yield return null;
        }
    }

    void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }
}