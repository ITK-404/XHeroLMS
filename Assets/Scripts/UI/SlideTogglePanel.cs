using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlideTogglePanel : MonoBehaviour
{
    [Header("UI")]
    public Button btnOn;
    public Button btnOff;
    public RectTransform parent;

    [Header("Position X")]
    public float xOn = -483.7233f; // vị trí panel "mở" (đi vào)
    public float xOff = 500f;      // vị trí panel "đóng" (đi ra)

    [Header("Animation")]
    public float duration = 1f;

    private Coroutine moveCo;

    private void Awake()
    {
        if (btnOn)  btnOn.onClick.AddListener(OnClickOn);
        if (btnOff) btnOff.onClick.AddListener(OnClickOff);
    }

    private void Start()
    {
        // Mặc định: btn_on bật, panel ở x=500
        SetXInstant(xOff);
        if (btnOn) btnOn.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (btnOn)  btnOn.onClick.RemoveListener(OnClickOn);
        if (btnOff) btnOff.onClick.RemoveListener(OnClickOff);
    }

    private void OnClickOn()
    {
        if (btnOn) btnOn.gameObject.SetActive(false); // ẩn ngay khi click
        MoveToX(xOn, onComplete: null);
    }

    private void OnClickOff()
    {
        // chạy ra x=500, xong mới hiện lại btn_on
        MoveToX(xOff, onComplete: () =>
        {
            if (btnOn) btnOn.gameObject.SetActive(true);
        });
    }

    private void MoveToX(float targetX, System.Action onComplete)
    {
        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = StartCoroutine(MoveXCoroutine(targetX, onComplete));
    }

    private IEnumerator MoveXCoroutine(float targetX, System.Action onComplete)
    {
        float startX = parent.anchoredPosition.x;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = (duration <= 0f) ? 1f : Mathf.Clamp01(t / duration);
            float x = Mathf.Lerp(startX, targetX, k);
            parent.anchoredPosition = new Vector2(x, parent.anchoredPosition.y);
            yield return null;
        }

        parent.anchoredPosition = new Vector2(targetX, parent.anchoredPosition.y);
        moveCo = null;

        onComplete?.Invoke();
    }

    private void SetXInstant(float x)
    {
        parent.anchoredPosition = new Vector2(x, parent.anchoredPosition.y);
    }
}
