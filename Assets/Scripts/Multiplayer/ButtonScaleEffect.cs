using UnityEngine;
using UnityEngine.UI;
public class ButtonScaleEffect : MonoBehaviour
{
    private Vector3 originalScale;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void OnPointerEnter()
    {
        if (button != null && button.interactable)
            transform.localScale = originalScale * 1.1f;
    }

    public void OnPointerExit()
    {
        if (button != null)
            transform.localScale = originalScale;
    }

    private void OnClick()
    {
        transform.localScale = originalScale;
    }
}