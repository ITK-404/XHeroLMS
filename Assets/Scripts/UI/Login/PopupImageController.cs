using UnityEngine;
using UnityEngine.UI;

public class PopupImageController : MonoBehaviour
{
    public Button openButton;
    public GameObject imageToShow;
    public GameObject imageToShow2;

    Canvas _canvas;
    RectTransform _btnRect;

    void Start()
    {
        imageToShow.SetActive(false);
        imageToShow2.SetActive(false);

        _btnRect = openButton.GetComponent<RectTransform>();
        _canvas  = openButton.GetComponentInParent<Canvas>();

        openButton.onClick.AddListener(ShowImage);
    }

    void ShowImage()
    {
        imageToShow.SetActive(true);
        imageToShow2.SetActive(true);
    }

    void Update()
    {
        // Nếu image đang tắt thì khỏi check
        if (!imageToShow.activeSelf)
            return;

        // Click chuột / chạm màn hình
        if (Input.GetMouseButtonDown(0))
        {
            // Nếu click trúng chính cái nút openButton thì bỏ qua
            bool clickOnButton = RectTransformUtility.RectangleContainsScreenPoint(
                _btnRect,
                Input.mousePosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera
            );

            if (clickOnButton)
                return;

            // Còn lại: click bất kỳ đâu -> ẩn image
            imageToShow.SetActive(false);
            imageToShow2.SetActive(false);
        }
    }
}
