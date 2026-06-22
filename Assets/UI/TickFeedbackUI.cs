using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TickFeedbackUI : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI messageText;
    [SerializeField] private float autohideDuration = 3f;

    private Coroutine autohideCoroutine;
public void ShowErrors(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0) return;

        if (autohideCoroutine != null)
            StopCoroutine(autohideCoroutine);

        var first = errors[0];

        if (messageText != null)
            messageText.text = first.message;

        if (panel != null)
            panel.SetActive(true);

        autohideCoroutine = StartCoroutine(AutoHide());
    }

    public void Hide()
    {
        if (autohideCoroutine != null)
        {
            StopCoroutine(autohideCoroutine);
            autohideCoroutine = null;
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autohideDuration);
        Hide();
    }
}
