using System.Collections.Generic;
using UnityEngine;

public class AnswerButtonManager : MonoBehaviour
{
    [SerializeField] private int totalAnswer = 3;
    [SerializeField] private int maxAnswer = 1;

    [SerializeField] private Transform container;
    [SerializeField] private AnswerButton answerButtonPrefab;
    private List<AnswerButton> answerButtonList = new();

    private List<AnswerButton> answerStack = new();

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
        if (answerStack.Contains(answerButton))
        {
            answerStack.Remove(answerButton);
            answerButton.ActiveSelect(false);
            return;
        }
        
        if (answerStack.Count >= maxAnswer)
        {
            var oldest = answerStack[0];
            answerStack.RemoveAt(0);
            oldest.ActiveSelect(false);
        }

        Debug.Log("Thêm button mới vào và kích hoạt");

        answerButton.ActiveSelect(true);
        answerStack.Add(answerButton);
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