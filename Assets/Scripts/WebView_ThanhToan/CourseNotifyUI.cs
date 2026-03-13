using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CourseNotifyUI : MonoBehaviour
{
    [SerializeField] private Image circleVfx;

    [Header("Rotate")]
    [SerializeField] private float rotateDuration = 1f;

    [Header("Fade")]
    [SerializeField] private float fadeStart = 0f;
    [SerializeField] private float fadeEnd = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Tween rotateTween;
    private Tween fadeTween;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        
        SetAlpha(fadeStart);

        // rotateTween = _rectTransform
        //     .DORotate(new Vector3(0, 0, -360f), rotateDuration, RotateMode.FastBeyond360)
        //     .SetEase(Ease.Linear)
        //     .SetLoops(-1);

        fadeTween = circleVfx
            .DOFade(fadeEnd, fadeDuration)
            .From(fadeStart)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void SetAlpha(float value)
    {
        var c = circleVfx.color;
        c.a = value;
        circleVfx.color = c;
    }

    private void OnDestroy()
    {
        rotateTween?.Kill();
        fadeTween?.Kill();
    }
}