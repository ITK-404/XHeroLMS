using UnityEngine;

public class ExamManager : MonoBehaviour
{
    public ExamData ExamData;
    public InformationExam InformationExam;
    public AnswerButtonManager answerButtonManager;
    public ExamCounter examCounter;
    private void Awake()
    {
        InformationExam.OnStartButtonClick += StartExam;
    }

    private void OnDestroy()
    {
        InformationExam.OnStartButtonClick -= StartExam;
    }

    private void Start()
    {
        ShowExamUI();
    }

    private void ShowExamUI()
    {
        InformationExam.Show();
        answerButtonManager.Hide();
        LoadUI();
    }

    private void StartExam()
    {
        answerButtonManager.Show();
        InformationExam.Hide();
        examCounter.StartCounter();
    }

    public void LoadUI()
    {
        InformationExam.SetExamData(ExamData);
        examCounter.SetData(ExamData);
    }
}