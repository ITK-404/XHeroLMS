using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverAnswerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private AnswerButton answerButton;
    private Button btn;
    private void Awake()
    {
        answerButton = GetComponentInParent<AnswerButton>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        // component này được gắn vào trong button nên chắc chắn button không null
        if (GetComponent<Button>().interactable == false)
        {
            return;
        }
        // Bỏ qua khi đã chọn hoặc đang xem lại đáp án
        if (answerButton.isSelect)
        {
            return;
        }

        if (answerButton.IsOnReviewAnswer)
        {
            return;
        }

        answerButton.SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (answerButton.isSelect)
        {
            return;
        }

        if (answerButton.IsOnReviewAnswer)
        {
            return;
        }
        answerButton.SetHover(false);
    }
}