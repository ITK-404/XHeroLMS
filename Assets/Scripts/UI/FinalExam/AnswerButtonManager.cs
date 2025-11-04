using System.Collections.Generic;
using UnityEngine;

public class AnswerButtonManager : MonoBehaviour
{
    [SerializeField] private int totalAnswer = 3;
    [SerializeField] private int maxAnswer = 1;

    [SerializeField] private Transform container;
    [SerializeField] private AnswerButton answerButtonPrefab;
    private List<AnswerButton> answerButtonList = new();

    private Stack<AnswerButton> answerStack = new();

    private void Start()
    {
        CreateAnswerButtons();
    }

    private void ClearExitButtons()
    {
        foreach (var item in answerButtonList)
        {
            Destroy(item.gameObject);
        }

        answerButtonList.Clear();
    }

    [ContextMenu("Create Answer Buttons")]
    public void CreateAnswerButtons()
    {
        ClearExitButtons();

        bool isMultipleChoice = maxAnswer > 1;
        for (int i = 0; i < totalAnswer; i++)
        {
            var btn = Instantiate(answerButtonPrefab, container);
            btn.OnSelectButton = SelectAnswer;
            btn.ActiveSelect(false);

            answerButtonList.Add(btn);

            if (isMultipleChoice)
            {
                btn.ActiveMultipleChoice();
            }
            else
            {
                btn.ActiveSingleChoice();
            }
        }
    }

    public void SelectAnswer(AnswerButton answerButton)
    {
        
        if (answerStack.Count >= maxAnswer)
        {
            Debug.Log("Đã vượt quá giới hạn và tắt đi button ở cuối");
            var btn = answerStack.Pop();
            btn.ActiveSelect(false);
        }

        Debug.Log("Thêm button mới vào và kích hoạt");

        answerButton.ActiveSelect(true);
        answerStack.Push(answerButton);
    }

    [ContextMenu("Get All Selected Answer")]
    public void GetAllSelectedAnswer()
    {
        foreach (var item in answerStack)
        {
            Debug.Log("Answer Button: ", item.gameObject);
        }
    }
}