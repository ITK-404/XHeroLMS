using UnityEngine;
using UnityEngine.UI;

public class ExamMatchingElement : MonoBehaviour
{
    public enum ElementSide
    {
        A,  // bên trái
        B   // bên phải
    }

    [Header("Side")]
    public ElementSide side;

    [Header("Matching Points (anchor để nối line)")]
    [SerializeField] private Transform topPoint;    // dùng cho side B
    [SerializeField] private Transform lowerPoint;  // dùng cho side A

    [Header("Sprites")]
    [SerializeField] private Image  matchingImg;
    [SerializeField] private Sprite normalMatching;
    [SerializeField] private Sprite correctMatching;

    [Header("Point Colors")]
    [SerializeField] private Color normalPointColor  = Color.white;
    [SerializeField] private Color correctPointColor = Color.green;

    private void Awake()
    {
        // Trường hợp set side sẵn trong Inspector
        RefreshPointVisibility();
        UpdatePointColor(false);
    }

    /// <summary>
    /// Được gọi từ MatchingElementHandler sau khi gán side.
    /// </summary>
    public void Initialize(ElementSide s)
    {
        side = s;
        RefreshPointVisibility();
        UpdatePointColor(false);
    }

    private void RefreshPointVisibility()
    {
        if (topPoint != null)
            topPoint.gameObject.SetActive(side == ElementSide.B); // chỉ hiện nếu là B (bên phải)

        if (lowerPoint != null)
            lowerPoint.gameObject.SetActive(side == ElementSide.A); // chỉ hiện nếu là A (bên trái)
    }

    /// <summary>Điểm neo để nối line.</summary>
    public Transform GetMatchingPoint()
    {
        return side == ElementSide.A ? lowerPoint : topPoint;
    }

    public void SetNormalMatching()
    {
        if (matchingImg != null && normalMatching != null)
            matchingImg.sprite = normalMatching;

        UpdatePointColor(false);
    }

    public void SetCorrectMatching()
    {
        if (matchingImg != null && correctMatching != null)
            matchingImg.sprite = correctMatching;

        UpdatePointColor(true);
    }

    private void UpdatePointColor(bool isCorrect)
    {
        var color = isCorrect ? correctPointColor : normalPointColor;

        if (topPoint != null)
        {
            var img = topPoint.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        if (lowerPoint != null)
        {
            var img = lowerPoint.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }
}
