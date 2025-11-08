using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamQuestionInformation : MonoBehaviour
{
    public TMP_Text textTotalQuestions;
    public TMP_Text textTotalDuration;
    public TMP_Text textPassNeed;
    public TMP_Text textTotalQuestion2;
    public TMP_Text textTotalQuestionAnswered;
    public TMP_Text textTimer;
    [Header("Question Element")]
    public ExamInfoElement examInfoElementPrefab;
    public Transform container;

    public Button returnToExamBtn;
    public Button submitBtn;
    public Button closeExamBtn;

    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);
}