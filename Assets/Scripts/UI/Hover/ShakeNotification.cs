using System;
using DG.Tweening;
using UnityEngine;

public class ShakeNotification : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float strength = 15f;
    [SerializeField] private int vibrato = 20;
    [SerializeField] private float randomness = 90f;
    [SerializeField] private bool loop = true;
    [SerializeField] private float loopInterval = 1.5f; // pause between each shake loop
    [SerializeField] private bool autoStart = false;
    private Tween _shakeTween;
    private Sequence _loopSequence;
    private Quaternion _originalRotation;

    private void Awake()
    {
        _originalRotation = transform.localRotation;

        if (autoStart)
        {
            StartShake();
        }
    }

    public void StartShake()
    {
        StopShake(); 

        if (loop)
        {
            _loopSequence = DOTween.Sequence();

            _loopSequence
                .Append(CreateShakeTween())
                .AppendInterval(loopInterval)
                .SetLoops(-1, LoopType.Restart); // loop vô tận
        }
        else
        {
            _shakeTween = CreateShakeTween();
        }
    }

    public void StopShake()
    {
        _loopSequence?.Kill();
        _shakeTween?.Kill();

        _loopSequence = null;
        _shakeTween   = null;

        transform.DOLocalRotateQuaternion(_originalRotation, 0.15f)
                 .SetEase(Ease.OutSine);
    }

    private Tween CreateShakeTween()
    {
        transform.localRotation = _originalRotation;

        return transform
            .DOShakeRotation(
                duration:   duration,
                strength:   new Vector3(0f, 0f, strength), 
                vibrato:    vibrato,
                randomness: randomness,
                fadeOut:    true)
            .SetEase(Ease.OutQuad);
    }


    private void OnDisable() => StopShake();
    private void OnDestroy() => StopShake();
}