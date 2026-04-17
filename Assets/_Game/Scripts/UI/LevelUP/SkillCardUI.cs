using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using _Game.LevelUp;
using DG.Tweening;
using _Game.Skills;
using Sirenix.OdinInspector;

public class SkillCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI text_Name;
    [SerializeField] private TextMeshProUGUI text_Tag;
    [SerializeField] private TextMeshProUGUI text_Desc;
    [SerializeField] private Button button;

    [BoxGroup("호버 연출")]
    [SerializeField] private Image hoverBorder;
    [BoxGroup("호버 연출")]
    [SerializeField] private RectTransform[] hoverTargets;
    [BoxGroup("호버 연출")]
    [SerializeField] private float hoverMoveY = 15f;
    [BoxGroup("호버 연출")]
    [SerializeField] private float hoverDuration = 0.2f;
    
    private Vector2[] _targetOrigins;
    
    [SerializeField] private TextMeshProUGUI text_NewAcquire;
    [SerializeField] private TextMeshProUGUI text_CurrentLevel;
    [SerializeField] private TextMeshProUGUI text_NextLevel;
    [SerializeField] private TextMeshProUGUI text_AddInfo;
    [SerializeField] private GameObject levelArrow;
    
    [SerializeField] private float blinkDuration = 0.6f;
    private Tweener _blinkTween;

    private LevelUpCardData _data;
    private Action<LevelUpCardData> _onSelected;
    private Vector3 _originLocalPos;

    private void Awake()
    {
        _targetOrigins = new Vector2[hoverTargets.Length];
        for (int i = 0; i < hoverTargets.Length; i++)
            _targetOrigins[i] = hoverTargets[i] != null ? hoverTargets[i].anchoredPosition : Vector2.zero;
    }

    public void Setup(LevelUpCardData data, Action<LevelUpCardData> onSelected)
    {
        _originLocalPos = transform.localPosition;
        
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
        for (int i = 0; i < hoverTargets.Length; i++)
        {
            if (hoverTargets[i] != null)
                hoverTargets[i].DOAnchorPosY(_targetOrigins[i].y + hoverMoveY, hoverDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true);
        }
        if (hoverBorder != null)
            hoverBorder.DOFade(1f, hoverDuration).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        for (int i = 0; i < hoverTargets.Length; i++)
        {
            if (hoverTargets[i] != null)
                hoverTargets[i].DOAnchorPosY(_targetOrigins[i].y, hoverDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true);
        }
        if (hoverBorder != null)
            hoverBorder.DOFade(0f, hoverDuration).SetUpdate(true);
    }

    private void ResetHover()
    {
        for (int i = 0; i < hoverTargets.Length; i++)
        {
            if (hoverTargets[i] != null)
                hoverTargets[i].anchoredPosition = _targetOrigins[i];
        }

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