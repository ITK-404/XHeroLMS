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
        HideAll();
    }
    [ContextMenu("Testing")]
    public void POolay()
    {
        StartTransitionAsync().Forget();
    }
    
    public async UniTask StartTransitionAsync()
    {
        transitionImages = GetComponentsInChildren<Image>(includeInactive: true);
        
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

    private void HideAll()
    {
        foreach (var image in transitionImages)
        {
            image.DOFade(0, 0);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Test();
        }
    }

    private void Test()
    {
        HideAll();
        StartTransitionAsync().Forget();
    }
}
