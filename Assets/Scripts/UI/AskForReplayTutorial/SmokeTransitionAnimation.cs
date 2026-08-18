using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SmokeTransitionAnimation : MonoBehaviour
{
    [SerializeField] private Image[] transitionImages;

    [SerializeField] private float delayPerImage = 0.1f;
    [SerializeField] private float fadeDuration = 0.1f;
    [SerializeField] private Ease fadeEase;

    private void Awake()
    {
        CacheImages();
        HideAll();
    }

    [ContextMenu("Testing")]
    public void POolay()
    {
        StartTransitionAsync().Forget();
    }

    public async UniTask StartTransitionAsync()
    {
        // Đảm bảo mọi lần chạy đều bật GO lên và fade về 0 trước khi tween
        CacheImages();
        HideAll();

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < transitionImages.Length; i++)
        {
            sequence.Insert(
                i * delayPerImage,
                transitionImages[i]
                    .DOFade(1, fadeDuration)
                    .SetEase(fadeEase));
        }

        await sequence.AsyncWaitForCompletion();
    }

    private void CacheImages()
    {
        transitionImages = GetComponentsInChildren<Image>(includeInactive: true);
    }

    private void HideAll()
    {
        foreach (var image in transitionImages)
        {
            image.gameObject.SetActive(true);
            image.DOFade(0, 0);
        }
    }

    private void Test()
    {
        StartTransitionAsync().Forget();
    }
}