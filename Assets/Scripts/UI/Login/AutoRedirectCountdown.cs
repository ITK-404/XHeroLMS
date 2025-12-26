using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AutoRedirectCountdown : MonoBehaviour
{
    [Header("UI Refs")]
    public TextMeshProUGUI messageText;     // TMP để hiện câu thông báo
    public Button okButton;                  // Nút OK
    public GameObject currentPanel;          // Panel hiện tại (sẽ ẩn)
    public GameObject imageToHide;           // Ảnh/logo cần ẩn cùng panel (nếu có)
    public GameObject loginPanel;            // Panel Login sẽ hiện

    [Header("Config")]
    [Min(0)] public int startSeconds = 5;
    [Tooltip("Chuỗi template, dùng {0} cho số giây")]
    [TextArea(2, 3)]
    public string messageTemplate = "Tuyệt vời! Quý học viên đã đổi mật khẩu thành công.\nGiờ thì đăng nhập lại để trải nghiệm tiếp nhé.";

    [Tooltip("Tự chạy đếm ngược ngay khi bật script")]
    public bool autoStart = true;

    Coroutine _countdownCo;

    void OnEnable()
    {
        if (okButton != null)
        {
            okButton.onClick.RemoveListener(OnClickOk);
            okButton.onClick.AddListener(OnClickOk);
        }

        if (autoStart) Begin();
    }

    void OnDisable()
    {
        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = null;
        if (okButton != null) okButton.onClick.RemoveListener(OnClickOk);
    }

    public void Begin()
    {
        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = StartCoroutine(CountdownRoutine(Mathf.Max(0, startSeconds)));
    }

    void OnClickOk()
    {
        // Nhấn OK thì chuyển ngay
        DoTransition();
    }

    IEnumerator CountdownRoutine(int seconds)
    {
        int remaining = seconds;

        // Cập nhật lần đầu
        UpdateMessage(remaining);

        // Đếm ngược theo thời gian thực (không phụ thuộc Time.timeScale)
        while (remaining > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
            UpdateMessage(remaining);
        }
        DoTransition();
    }

    void UpdateMessage(int secondsLeft)
    {
        if (messageText != null)
        {
            messageText.text = string.Format(messageTemplate);
        }
    }

    void DoTransition()
    {
        // Ngừng đếm nếu còn
        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = null;

        // Ẩn panel hiện tại & image
        if (currentPanel != null) currentPanel.SetActive(false);
        if (imageToHide != null) imageToHide.SetActive(false);

        // Hiện panel login
        if (loginPanel != null) loginPanel.SetActive(true);
    }
}
