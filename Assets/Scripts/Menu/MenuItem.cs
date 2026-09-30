using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The look and sound of one menu entry (button, slider or toggle row). Selected items light up their label,
// show the highlight bar and a blinking cursor. Hovering with the mouse selects, so mouse, keyboard and
// gamepad all share the one highlight.
[RequireComponent(typeof(Selectable))]
public class MenuItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    public enum Sound { Confirm, Back, None }

    [SerializeField] private Text label;
    [SerializeField] private Graphic highlight;
    [SerializeField] private Image cursor;
    [SerializeField] private Color normalColor = new Color32(148, 176, 194, 255);
    [SerializeField] private Color selectedColor = new Color32(244, 244, 244, 255);
    [SerializeField] private Sound clickSound = Sound.Confirm;
    // Keeps the cursor clear of the label's left edge; set false for rows where the cursor has a fixed spot.
    [SerializeField] private bool cursorFollowsLabel = true;
    [SerializeField] private float cursorGap = 8.0f;
    [SerializeField] private float blinkRate = 4.0f;
    private Selectable selectable;
    private MenuNavigator navigator;
    private Vector2 cursorHome;
    private bool selected;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        navigator = GetComponentInParent<MenuNavigator>(true);
        if (cursor)
        {
            cursorHome = cursor.rectTransform.anchoredPosition;
        }
        SetSelected(false);
    }

    private void OnDisable()
    {
        SetSelected(false);
    }

    private void Update()
    {
        if (selected && cursor)
        {
            // Nudge the cursor a pixel back and forth, like an arcade menu pointer.
            float nudge = Mathf.Floor(Time.unscaledTime * blinkRate) % 2.0f;
            cursor.rectTransform.anchoredPosition = CursorPosition() + Vector2.left * nudge;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetSelected(true);
        if (navigator)
        {
            navigator.PlayMove();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable.IsInteractable() && EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Clicked();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Clicked();
        }
    }

    private void Clicked()
    {
        if (!navigator || !selectable.IsInteractable())
        {
            return;
        }
        if (clickSound == Sound.Confirm)
        {
            navigator.PlayConfirm();
        }
        else if (clickSound == Sound.Back)
        {
            navigator.PlayBack();
        }
    }

    private void SetSelected(bool value)
    {
        selected = value;
        if (label)
        {
            label.color = value ? selectedColor : normalColor;
        }
        if (highlight)
        {
            highlight.enabled = value;
        }
        if (cursor)
        {
            cursor.enabled = value;
            cursor.rectTransform.anchoredPosition = CursorPosition();
        }
    }

    private Vector2 CursorPosition()
    {
        if (!cursorFollowsLabel || !label)
        {
            return cursorHome;
        }
        // Centred labels vary in width, so park the cursor just left of the text itself.
        float left = label.rectTransform.anchoredPosition.x - Mathf.Ceil(label.preferredWidth * 0.5f);
        RectTransform rect = cursor.rectTransform;
        return new Vector2(Mathf.Round(left - cursorGap - rect.rect.width * (1.0f - rect.pivot.x)), cursorHome.y);
    }
}
