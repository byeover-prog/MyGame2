using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class UIButtonPointerProbe : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public void OnPointerDown(PointerEventData eventData)
        => Debug.Log("[Btn_Start] DOWN");

    public void OnPointerUp(PointerEventData eventData)
        => Debug.Log("[Btn_Start] UP");

    public void OnPointerClick(PointerEventData eventData)
        => Debug.Log("[Btn_Start] CLICK");
}