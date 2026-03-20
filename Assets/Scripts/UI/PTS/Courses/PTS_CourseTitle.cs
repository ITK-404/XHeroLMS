using TMPro;
using UnityEngine;

public class PTS_CourseTitle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;

    private void OnEnable()
    {
        if (CourseDetailStaticStore.CurrentDetail != null)
        {
            UpdateTitleText(CourseDetailStaticStore.CurrentDetail.title);
        }
    }

    private void UpdateTitleText(string text)
    {
        if(titleText)
            titleText.text = text;
    }
}