using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookModel : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Renderer _renderer;
    private Tween tween;
    private bool isTweenDone = false;
    private float speed = 1;

    private void Awake()
    {
        smoothCurve = AnimationUltis.CreateInOutBackCurve();
    }

    public void SetColor(Color color)
    {
        // testing
        _renderer.materials[1].SetColor("_BaseColor",color);
    }

    public void SetBaseMap(Texture texture)
    {
        _renderer.materials[1].SetTexture("_MainTex", texture);
    }

    [ContextMenu("De Active Grayscale")]
    public void ActiveGrayScale()
    {
        SetGrayScale(0);
    }

    [ContextMenu("Active Grayscale")]
    public void DeActiveGrayScale()
    {
        SetGrayScale(1);
    }

    public void SetGrayScale(float grayScale)
    {

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

    private void Update()
    {
        if (isTweenDone)
        {
            container.transform.rotation = Quaternion.Lerp(container.transform.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * 5);
        }
    }



    public AnimationCurve smoothCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.3f, -0.2f, 0f, 2f),
        new Keyframe(0.7f, 1.2f, 2f, 0f),
        new Keyframe(1f, 1f, 0f, 0f)
        );

    private IEnumerator StartRotate()
    {
        float duration = 1;
        float elapsedTime = 0;
        while (true)
        {
            elapsedTime += Time.deltaTime * speed;
            float ratio = elapsedTime / duration;
            float smooth = smoothCurve.Evaluate(ratio);

            float currentValue = Mathf.Lerp(0, 360, smooth);
            container.transform.localRotation = Quaternion.Euler(0, currentValue, 0);

            if (elapsedTime >= duration)
            {
                break;
            }
            yield return null;
        }

        yield return null;
    }
}
