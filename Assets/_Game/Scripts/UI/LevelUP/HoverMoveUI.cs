using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverMoveUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverMoveY = 8f;
    [SerializeField] private float duration = 0.2f;

    private RectTransform _rect;
    private Vector2 _originPos;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originPos = _rect.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _rect.DOAnchorPosY(_originPos.y + hoverMoveY, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _rect.DOAnchorPosY(_originPos.y, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }
}