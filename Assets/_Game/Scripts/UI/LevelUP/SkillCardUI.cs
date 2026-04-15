using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using _Game.LevelUp;
using DG.Tweening;
using _Game.Skills;

public class SkillCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI text_Name;
    [SerializeField] private TextMeshProUGUI text_Tag;
    [SerializeField] private TextMeshProUGUI text_Desc;
    [SerializeField] private Button button;

    [Header("호버 연출")]
    [SerializeField] private Image hoverBorder;       // HoverBorder Image
    [SerializeField] private float hoverMoveY = 15f;  // 올라가는 픽셀
    [SerializeField] private float hoverDuration = 0.2f;
    
    [SerializeField] private TextMeshProUGUI text_NewAcquire;
    [SerializeField] private TextMeshProUGUI text_CurrentLevel;
    [SerializeField] private TextMeshProUGUI text_NextLevel;
    [SerializeField] private TextMeshProUGUI text_AddInfo;
    [SerializeField] private GameObject levelArrow;
    
    [SerializeField] private float blinkDuration = 0.6f;
    private Tweener _blinkTween;

    private LevelUpCardData _data;
    private Action<LevelUpCardData> _onSelected;
    private Vector2 _originPos;

    private void Awake()
    {
        _originPos = GetComponent<RectTransform>().anchoredPosition;
    }

    public void Setup(LevelUpCardData data, Action<LevelUpCardData> onSelected)
    {
        _data = data;
        _onSelected = onSelected;

        text_Name.text = data.Title ?? "";
        text_Tag.text  = data.Tag ?? "";
        text_Desc.text = data.Description ?? "";

        if (icon != null && data.Icon != null)
            icon.sprite = data.Icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onSelected?.Invoke(_data));

        ResetHover();

        _blinkTween?.Kill();

        if (data.CurrentLevel == 0)
        {
            if (text_NewAcquire != null)
            {
                text_NewAcquire.text = "New";
                text_NewAcquire.gameObject.SetActive(true);
                _blinkTween = text_NewAcquire.DOFade(0.2f, blinkDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            }
            if (text_CurrentLevel != null) text_CurrentLevel.gameObject.SetActive(false);
            if (text_NextLevel != null) text_NextLevel.gameObject.SetActive(false);
            if (levelArrow != null) levelArrow.SetActive(false);
        }
        else
        {
            if (text_NewAcquire != null) text_NewAcquire.gameObject.SetActive(false);
            if (text_CurrentLevel != null)
            {
                text_CurrentLevel.gameObject.SetActive(true);
                text_CurrentLevel.text = $"Lv{data.CurrentLevel}";
            }
            if (text_NextLevel != null)
            {
                text_NextLevel.gameObject.SetActive(true);
                text_NextLevel.text = $"Lv{data.NextLevel}";
            }
            if (levelArrow != null) levelArrow.SetActive(true);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var rect = GetComponent<RectTransform>();

        // 위로 올라오기
        rect.DOAnchorPosY(_originPos.y + hoverMoveY, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // 테두리 페이드인
        if (hoverBorder != null)
            hoverBorder.DOFade(1f, hoverDuration).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var rect = GetComponent<RectTransform>();

        // 원래 위치로
        rect.DOAnchorPosY(_originPos.y, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // 테두리 페이드아웃
        if (hoverBorder != null)
            hoverBorder.DOFade(0f, hoverDuration).SetUpdate(true);
    }

    private void ResetHover()
    {
        var rect = GetComponent<RectTransform>();
        rect.anchoredPosition = _originPos;

        if (hoverBorder != null)
        {
            var c = hoverBorder.color;
            c.a = 0f;
            hoverBorder.color = c;
        }
    }
    private void OnDisable()
    {
        _blinkTween?.Kill();
    }
}