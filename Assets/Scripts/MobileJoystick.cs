using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 80f;

    private Vector2 inputVector;

    public Vector2 InputVector => inputVector;
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            Vector2 size = background.sizeDelta;

            Vector2 normalized = new Vector2(
                localPoint.x / (size.x * 0.5f),
                localPoint.y / (size.y * 0.5f)
            );

            inputVector = Vector2.ClampMagnitude(normalized, 1f);
            handle.anchoredPosition = inputVector * handleRange;

            MobileInputState.MoveInput = inputVector;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        MobileInputState.MoveInput = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}