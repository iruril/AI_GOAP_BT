using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static SwipePanel Instance { get; private set; }

    public event Action<Vector2> OnSwipe;
    public event Action OnSwipeEnd;

    bool dragging;
    Vector2 lastDragPos;

    private void Awake()
    {
        Instance = this;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        lastDragPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        Vector2 delta = eventData.position - lastDragPos;
        lastDragPos = eventData.position;

        OnSwipe?.Invoke(delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        OnSwipeEnd?.Invoke();
    }
}
