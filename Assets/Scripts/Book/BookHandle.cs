using DG.Tweening;
using System.Collections;
using UnityEngine;

public class BookHandle : MonoBehaviour
{
    [SerializeField] private Transform container;

    private void Awake()
    {
        CreateInOutBackCurve();
    }

    private void OnMouseEnter()
    {
        if (container == null)
        {
            Debug.LogError("Container is null");
            return;
        }
        isTweenDone = false;
        Debug.Log("On Mouse Enter");
        //container.DOKill();
        //container.DORotate(new Vector3(0, 360, 0), 2, RotateMode.FastBeyond360).SetEase(Ease.InOutBack);
        speed = 1;
        container.transform.DOKill();
        container.transform.DOScale(Vector3.one * 1.1f, 1).SetEase(Ease.InSine);
        StopAllCoroutines();
        StartCoroutine(StartRotate());
    }
    private void OnMouseExit()
    {
        if (container == null)
        {
            Debug.LogError("Container is null");
            return;
        }
        container.transform.DOKill();
        container.transform.DOScale(Vector3.one, 1).SetEase(Ease.OutSine);
        return;
        if (tween != null)
        {
            tween.Kill();
        }
        tween = DOVirtual.Float(speed, 0.2f, 1, (x) =>
        {
            speed = x;
        }).SetEase(Ease.OutBack).OnComplete(() =>
        {
            isTweenDone = true;
            
        });
        
    }

    private bool isTweenDone = false;
    private void Update()
    {
        if (isTweenDone)
        {
            container.transform.rotation = Quaternion.Lerp(container.transform.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * 5);
        }
    }

    private Tween tween;


    public AnimationCurve smoothCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.3f, -0.2f, 0f, 2f),
        new Keyframe(0.7f, 1.2f, 2f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
        );

    private IEnumerator StartRotate()
    {
    
        float duration = 2;
        float elapsedTime = 0;
        while (true)
        {
            elapsedTime += Time.deltaTime * speed;
            float ratio = elapsedTime / duration;
            float smooth = smoothCurve.Evaluate(ratio);

            float currentValue = Mathf.Lerp(0, 360, smooth);
            Debug.Log("ratio value: " + ratio);
            container.transform.localRotation = Quaternion.Euler(0, currentValue, 0);

            if (elapsedTime >= duration)
            {
                break;
            }
            yield return null;
        }

        yield return null;
    }

    private float speed = 1;

    public void CreateInOutBackCurve()
    {
        smoothCurve = new AnimationCurve();

        // Constants for back easing
        float c1 = 1.70158f;
        float c2 = c1 * 1.525f;

        // Tạo nhiều keyframes để curve mượt mà hơn
        int keyCount = 50;

        for (int i = 0; i <= keyCount; i++)
        {
            float t = i / (float)keyCount;
            float value;

            // InOutBack easing formula
            if (t < 0.5f)
            {
                value = (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2;
            }
            else
            {
                value = (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
            }

            // Add keyframe
            Keyframe key = new Keyframe(t, value);

            // Set tangents to smooth (optional, có thể điều chỉnh)
            key.inTangent = 0;
            key.outTangent = 0;
            key.weightedMode = WeightedMode.None;

            smoothCurve.AddKey(key);
        }

        // Smooth all tangents
        for (int i = 0; i < smoothCurve.keys.Length; i++)
        {
            smoothCurve.SmoothTangents(i, 0);
        }
    }
}
