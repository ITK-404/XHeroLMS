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
    public enum State { Unanswered, Answered, Selected, Correct, InCorrect }

    [Header("Sprites")]
    [SerializeField] private CanvasGroup sprUnanswered;
    [SerializeField] private CanvasGroup sprAnswered;
    [SerializeField] private CanvasGroup sprSelected;
    [SerializeField] private CanvasGroup sprCorrect;
    [SerializeField] private CanvasGroup sprInCorrect;
    [Header("UI Refs")]
    [SerializeField] private Image  rootButtonImage;   // Image của Button
    [SerializeField] private Button clickable;         // chính Button
    [SerializeField] private TextMeshProUGUI coloredTmp;
    [SerializeField] private TextMeshProUGUI grayTmp;
    [SerializeField] private TextMeshProUGUI gradientTmp;
    [SerializeField] private TextMeshProUGUI correctTmp;
    [SerializeField] private TextMeshProUGUI inCorrectTmp;
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
        activeList.Add(sprCorrect);
        activeList.Add(sprInCorrect);

        foreach (var item in activeList)
        {
            item.DOFade(0, 0);
        }
    }

    // private void OnEnable()
    // {
    //     EnsureRootImageSetup();
    // }

    // ===== public API =====
    public Button GetButton() => clickable;

    public void SetQuestionIndexText(int index)
    {
        //string t = $"Câu\n{index}";
        string t = $"{index}";
        if (coloredTmp)  coloredTmp.text  = t;
        if (grayTmp)     grayTmp.text     = t;
        if (gradientTmp) gradientTmp.text = t;
    }

    public void SetAnsweredButton()        => ApplyState(State.Answered);
    public void SetUnansweredButton()      => ApplyState(State.Unanswered);
    public void ShowSelectedAnswerButton() => ApplyState(State.Selected);
    public void ShowCorrectButton() => ApplyState(State.Correct);
    public void ShowInCorrectButton() => ApplyState(State.InCorrect);
    private State currentState;
    public void ApplyState(State s)
    {
        // text layers
        if (coloredTmp) coloredTmp.gameObject.SetActive(s == State.Answered);
        if (grayTmp) grayTmp.gameObject.SetActive(s == State.Unanswered);
        if (gradientTmp) gradientTmp.gameObject.SetActive(s == State.Selected);
        if (gradientTmp) correctTmp.gameObject.SetActive(s == State.Correct);
        if (gradientTmp) inCorrectTmp.gameObject.SetActive(s == State.InCorrect);

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
            case State.Correct: 
                ActiveImage(sprCorrect);
                break;
            case State.InCorrect: 
                ActiveImage(sprInCorrect);
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
}
