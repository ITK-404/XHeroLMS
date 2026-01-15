using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviewBigAreaUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI reviewInformationTmp;
    [SerializeField] private GameObject container;
    [SerializeField] private Button closeBgBtn;
    [SerializeField] private Button closeBtn;

    private void Awake()
    {
        closeBgBtn.onClick.AddListener(Hide);
        closeBtn.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        closeBgBtn.onClick.RemoveListener(Hide);
        closeBtn.onClick.RemoveListener(Hide);
    }

    public void Show(BigArea bigArea)
    {
        reviewInformationTmp.text =
            "Khu vực Lớp học là không gian học tập chuyên sâu, nơi học viên có thể tham gia các lớp học phong thủy từ cơ bản đến nâng cao. Tại đây, học viên được tiếp cận kiến thức chuẩn mực, lộ trình rõ ràng và hướng dẫn trực tiếp từ đội ngũ chuyên gia, giúp nâng cao hiểu biết, ứng dụng phong thủy hiệu quả vào đời sống và công việc.";
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}