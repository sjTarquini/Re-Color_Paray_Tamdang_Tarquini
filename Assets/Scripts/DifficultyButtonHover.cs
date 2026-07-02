using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class DifficultyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.8f, 0.9f, 1f, 1f);
    [SerializeField] private float colorDuration = 0.2f;

    [Header("Label")]
    [SerializeField] private TextMeshProUGUI difficultyLabel;
    [SerializeField] private float fadeDuration = 0.3f;

    private Image buttonImage;
    private Coroutine colorCoroutine;
    private Coroutine fadeCoroutine;

    public bool IsSelected { get; private set; } = false;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();

        if (difficultyLabel != null)
        {
            Color c = difficultyLabel.color;
            c.a = 0f;
            difficultyLabel.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsSelected) return; // already locked in, ignore hover

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(hoverColor));

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeLabel(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsSelected) return; // stay highlighted if selected

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(normalColor));

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeLabel(0f));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var multiplayerManager = FindObjectOfType<M_DifficultyButtonManager>();
        if (multiplayerManager != null)
            multiplayerManager.SelectButton(this);
        else if (DifficultyButtonManager.Instance != null)
            DifficultyButtonManager.Instance.SelectButton(this);
    }
    public void Select()
    {
        IsSelected = true;

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(hoverColor));

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeLabel(1f));
    }

    // Called by the manager to deactivate when another is selected
    public void Deselect()
    {
        IsSelected = false;

        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(LerpColor(normalColor));

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeLabel(0f));
    }

    private IEnumerator LerpColor(Color targetColor)
    {
        Color startColor = buttonImage.color;
        float elapsed = 0f;

        while (elapsed < colorDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / colorDuration);
            buttonImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        buttonImage.color = targetColor;
    }

    private IEnumerator FadeLabel(float targetAlpha)
    {
        float startAlpha = difficultyLabel.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);

            Color c = difficultyLabel.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            difficultyLabel.color = c;

            yield return null;
        }

        Color final = difficultyLabel.color;
        final.a = targetAlpha;
        difficultyLabel.color = final;
    }
}