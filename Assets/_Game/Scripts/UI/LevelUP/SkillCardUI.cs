using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using _Game.LevelUp;
using DG.Tweening;

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

        // 초기화
        ResetHover();
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
}