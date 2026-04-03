using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContainClassGroupUI : MonoBehaviour
{
    [SerializeField] private Button attandBtn;
    [SerializeField] private TextMeshProUGUI attendDateTmp;
    [SerializeField] private Transform noteText;

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += UpdateDate;
        UpdateDate();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= UpdateDate;
    }

    public void Show(bool classContainDate)
    {
        gameObject.SetActive(true);

        attandBtn.gameObject.SetActive(!classContainDate);
        noteText.gameObject.SetActive(!classContainDate);
        attendDateTmp.gameObject.SetActive(classContainDate);

        if (classContainDate)
            UpdateDate();
    }

    public void Hide() => gameObject.SetActive(false);

    private void UpdateDate()
    {
        if (attendDateTmp == null) return;

        string date = GetCourseStartDate();

        if (string.IsNullOrWhiteSpace(date))
        {
            attendDateTmp.text = "TẠI ĐÂY";

            // optional: nếu muốn UX rõ hơn
            attandBtn.gameObject.SetActive(true);
            noteText.gameObject.SetActive(true);
            attendDateTmp.gameObject.SetActive(false);
        }
        else
        {
            attendDateTmp.text = date;

            // đảm bảo trạng thái đúng
            attandBtn.gameObject.SetActive(false);
            noteText.gameObject.SetActive(false);
            attendDateTmp.gameObject.SetActive(true);
        }
    }

    private string GetCourseStartDate()
    {
        var detail = CourseDetailStaticStore.CurrentDetail;
        if (detail == null || detail.courseStartDate == null || detail.courseStartDate.Count == 0)
            return null;

        var first = detail.courseStartDate[0];
        if (first == null || first.start == null)
            return null;

        int day = first.start.day;
        int month = first.start.month;
        int year = first.start.year;

        // format đẹp
        return $"{day:00}.{month:00}.{year}";
    }
}