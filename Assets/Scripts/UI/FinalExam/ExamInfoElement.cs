using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
// [RequireComponent(typeof(Button))]
// [RequireComponent(typeof(Image))]
[ExecuteAlways] // để OnValidate chạy trong Editor
public class ExamInfoElement : MonoBehaviour 
{
    public enum State { Unanswered, Answered, Selected }

    [Header("Sprites")]
    [SerializeField] private CanvasGroup sprUnanswered;
    [SerializeField] private CanvasGroup sprAnswered;
    [SerializeField] private CanvasGroup sprSelected;

    [Header("UI Refs")]
    [SerializeField] private Image  rootButtonImage;   // Image của Button
    [SerializeField] private Button clickable;         // chính Button
    [SerializeField] private TextMeshProUGUI coloredTmp;
    [SerializeField] private TextMeshProUGUI grayTmp;
    [SerializeField] private TextMeshProUGUI gradientTmp;
    [Header("Hoevr")]
    public HoverNavigationUI hoverUnanswered;
    public HoverNavigationUI hoverAnswered;
    // ===== lifecycle =====
    private void Reset()
    {
        clickable       = GetComponent<Button>();
        rootButtonImage = GetComponent<Image>();
    }

#if UNITY_EDITOR
    // private void OnValidate()
    // {
    //     AutoWire();
    //     EnsureRootImageSetup();
    //     // nếu thiếu sprite nào đó, đỡ bị null-ref
    //     if (!Application.isPlaying) ApplyState(State.Unanswered);
    // }
#endif

    private void Awake()
    {
        // AutoWire();
        // EnsureRootImageSetup();
        ApplyState(State.Unanswered);
        
        activeList.Add(sprUnanswered);
        activeList.Add(sprAnswered);
        activeList.Add(sprSelected);

        foreach (var item in activeList)
        {
            item.DOFade(0, 0);
        }
        ActiveHover(false);
    }

    // private void OnEnable()
    // {
    //     EnsureRootImageSetup();
    // }

    // ===== public API =====
    public Button GetButton() => clickable;

    public void SetQuestionIndexText(int index)
    {
        string t = $"Câu\n{index}";
        if (coloredTmp)  coloredTmp.text  = t;
        if (grayTmp)     grayTmp.text     = t;
        if (gradientTmp) gradientTmp.text = t;
    }

    public void SetAnsweredButton()        => ApplyState(State.Answered);
    public void SetUnansweredButton()      => ApplyState(State.Unanswered);
    public void ShowSelectedAnswerButton() => ApplyState(State.Selected);
    private State currentState;
    public void ApplyState(State s)
    {
        // text layers
        if (coloredTmp) coloredTmp.gameObject.SetActive(s == State.Answered);
        if (grayTmp) grayTmp.gameObject.SetActive(s == State.Unanswered);
        if (gradientTmp) gradientTmp.gameObject.SetActive(s == State.Selected);

        // đổi sprite trên Image chính
        if (!rootButtonImage) return;

        switch (s)
        {
            case State.Unanswered:
                ActiveImage(sprUnanswered);
                break;
            case State.Answered:
                ActiveImage(sprAnswered);
                break;
            case State.Selected:
                ActiveImage(sprSelected);
                // turn off hover when selected
                break;
        }
        currentState = s;
    }

    private List<CanvasGroup> activeList = new();
    private void ActiveImage(CanvasGroup activeImage)
    {
        foreach (var item in activeList)
        {
            item.DOKill();
            if (item == activeImage)
            {
                item.DOFade(1, 0.3f);
            }
            else
            {
                item.DOFade(0, 0.3f);
            }
        }
    }

    public void ActiveHover(bool isHover)
    {
        // show hover đi
        switch (currentState)
        {
            case State.Unanswered:
                hoverUnanswered.SetHoverAndHideNormal(isHover);
                hoverAnswered.SetHoverAndHideNormal(false);
                break;
            case State.Answered:
                hoverAnswered.SetHoverAndHideNormal(isHover);
                hoverUnanswered.SetHoverAndHideNormal(false);
                break;
            case State.Selected:
                hoverUnanswered.SetHoverAndHideNormal(false);
                hoverAnswered.SetHoverAndHideNormal(false);
                break;
            default:
                break;
        }
    }
}
