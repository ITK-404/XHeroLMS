using System;
using System.Collections;
using UnityEngine;

public class UIView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected GameObject root;
    [SerializeField] protected CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] protected float fadeDuration = 0.2f;
    [SerializeField] protected bool useFade = true;
    [SerializeField] protected string targetID;
    protected Coroutine transitionCoroutine;
    
    public Action OnViewOpened;
    public Action OnViewClosed;
    
    public string TargetID
    {
        get => targetID;
    }
    public bool IsShowing { get; private set; }
    public bool IsTransitioning { get; private set; }

    protected virtual void Reset()
    {
        root = gameObject;
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    protected virtual void Awake()
    {
        if (root == null)
            root = gameObject;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public virtual void Show()
    {
        if (IsShowing && !IsTransitioning)
            return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine((IEnumerator)CoShow());
    }

    public virtual void Hide()
    {
        if (!IsShowing && !IsTransitioning)
            return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CoHide());
    }

    protected virtual IEnumerator CoShow()
    {
        IsTransitioning = true;
        root.SetActive(true);

        OnBeforeShow();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (useFade)
        {
            yield return Fade(canvasGroup.alpha, 1f, fadeDuration);
        }
        else
        {
            canvasGroup.alpha = 1f;
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        IsShowing = true;
        IsTransitioning = false;

        OnAfterShow();
    }

    protected virtual IEnumerator CoHide()
    {
        IsTransitioning = true;

        OnBeforeHide();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (useFade)
        {
            yield return Fade(canvasGroup.alpha, 0f, fadeDuration);
        }
        else
        {
            canvasGroup.alpha = 0f;
        }

        root.SetActive(false);

        IsShowing = false;
        IsTransitioning = false;
        
        OnAfterHide();
    }

    protected virtual IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    public void SetFade(float fadeDuration) => this.fadeDuration = fadeDuration;

    protected virtual void OnBeforeShow()
    {
        OnViewOpened?.Invoke();
    }
    protected virtual void OnAfterShow() { }
    protected virtual void OnBeforeHide() { }
    protected virtual void OnAfterHide() {OnViewClosed?.Invoke(); }
}