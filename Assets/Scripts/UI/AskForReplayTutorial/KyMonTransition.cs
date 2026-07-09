using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KyMonTransition : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image[] images;
    [SerializeField] private RectTransform[] containers;

    [Header("Transition")]
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float distancePerImage = 3f;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Event")]
    [SerializeField] private UnityEvent onFinished;

    private BaseInput baseInput;

    private int currentIndex;
    private float currentDistance;
    private bool isTransitioning;
    private bool finished;

    private void Awake()
    {
        baseInput = FindAnyObjectByType<BaseInput>();

        for (int i = 0; i < images.Length; i++)
        {
            containers[i].localScale = Vector3.one;
            images[i].color = new Color(1, 1, 1, i == 0 ? 1 : 0);
        }
    }

    private void Update()
    {
        if (finished || isTransitioning || images.Length == 0)
            return;

        Vector2 move = baseInput.MoveVector;

        if (move.y <= 0)
            return;

        currentDistance += move.y * Time.deltaTime;

        float t = Mathf.Clamp01(currentDistance / distancePerImage);

        containers[currentIndex].localScale =
            Vector3.one * Mathf.Lerp(1f, maxScale, t);

        if (t >= 1f)
        {
            TransitionToNext();
        }
    }

    private void TransitionToNext()
    {
        isTransitioning = true;

        if (currentIndex >= images.Length - 1)
        {
            finished = true;
            onFinished?.Invoke();
            return;
        }

        Image currentImage = images[currentIndex];
        Image nextImage = images[currentIndex + 1];

        RectTransform currentContainer = containers[currentIndex];
        RectTransform nextContainer = containers[currentIndex + 1];

        nextContainer.localScale = Vector3.one;
        nextImage.color = new Color(1, 1, 1, 0);

        DOTween.Sequence()
            .Join(currentImage.DOFade(0f, fadeDuration))
            .Join(nextImage.DOFade(1f, fadeDuration))
            .OnComplete(() =>
            {
                currentContainer.localScale = Vector3.one;

                currentIndex++;
                currentDistance = 0f;
                isTransitioning = false;
            });
    }
}