using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 코드로 만든 UI가 누르기·끌기·떼기를 받을 수 있게 이벤트를 콜백으로 넘겨준다 (MobileControls에서 사용)
public class PointerRelay : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
{
    public Action<PointerEventData> down;
    public Action<PointerEventData> drag;
    public Action<PointerEventData> up;

    public void OnPointerDown(PointerEventData eventData) => down?.Invoke(eventData);

    // 조이스틱은 조금만 밀어도 바로 반응해야 하므로 끌기 문턱을 없앤다
    public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

    public void OnDrag(PointerEventData eventData) => drag?.Invoke(eventData);

    public void OnPointerUp(PointerEventData eventData) => up?.Invoke(eventData);
}
