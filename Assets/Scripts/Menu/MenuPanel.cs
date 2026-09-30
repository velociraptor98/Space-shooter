using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One screen of a menu (main, options, pause...). Fades in and out in a few hard steps for a retro feel, and
// remembers which item was selected so coming back to it lands where the player left off.
[RequireComponent(typeof(CanvasGroup))]
public class MenuPanel : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private float fadeTime = 0.12f;
    [SerializeField] private int fadeSteps = 3;
    private CanvasGroup group;
    private GameObject lastSelected;
    private Coroutine fade;

    private CanvasGroup Group => group ? group : group = GetComponent<CanvasGroup>();

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            Group.alpha = 0.0f;
            gameObject.SetActive(true);
        }
        Group.interactable = true;
        Group.blocksRaycasts = true;
        FadeTo(1.0f, false);
        Select();
    }

    public void Hide()
    {
        GameObject selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (selected && selected.transform.IsChildOf(transform))
        {
            lastSelected = selected;
        }
        Group.interactable = false;
        Group.blocksRaycasts = false;
        if (gameObject.activeInHierarchy)
        {
            FadeTo(0.0f, true);
        }
    }

    // Starts the panel over from its first item, for menus reopened fresh (pause, game over).
    public void ResetSelection()
    {
        lastSelected = null;
    }

    public void Select()
    {
        GameObject target = lastSelected && lastSelected.activeInHierarchy ? lastSelected : firstSelected ? firstSelected.gameObject : null;
        if (EventSystem.current && target)
        {
            EventSystem.current.SetSelectedGameObject(target);
        }
    }

    private void FadeTo(float target, bool deactivate)
    {
        if (fade != null)
        {
            StopCoroutine(fade);
        }
        fade = StartCoroutine(Fade(target, deactivate));
    }

    private IEnumerator Fade(float target, bool deactivate)
    {
        float start = Group.alpha;
        // Unscaled, so menus still animate while the game is paused.
        for (float t = 0.0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            float stepped = Mathf.Floor(t / fadeTime * fadeSteps) / fadeSteps;
            Group.alpha = Mathf.Lerp(start, target, stepped);
            yield return null;
        }
        Group.alpha = target;
        fade = null;
        if (deactivate)
        {
            gameObject.SetActive(false);
        }
    }
}
