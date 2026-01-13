using System;
using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BookModel : MonoBehaviour
{
    private static float rotationSpeed = 4;
    [SerializeField] private Transform container;
    [SerializeField] private Renderer _renderer;
    private Tween tween;
    
    private float speed = 1;
    private float rotationY = 0f;
    private float defaultRotation;
    
    private bool isMouseDown;
    private bool isTweenDone = false;
    
    public Action OnPlayerClickBook;
    public bool canHover = true;
    private void Awake()
    {
        smoothCurve = AnimationUltis.CreateInOutBackCurve();
    }


    public void SetBaseMap(Texture texture)
    {
        if(texture)
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

    private float lastTimeClicked;
    private void OnMouseEnter()
    {
        if (!canHover) return;
        if (isDragging) return;
        
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
        // container.transform.DOKill();
        // container.transform.DOScale(Vector3.one * 1.1f, 0.5f).SetEase(Ease.InSine);
        StopAllCoroutines();
        StartCoroutine(StartRotate());
    }

    private static bool isDragging = false;
    private void OnMouseExit()
    {
        if (!canHover) return;

        if (container == null)
        {
            Debug.LogError("Container is null");
            return;
        }

        // container.transform.DOKill();
        // container.transform.DOScale(Vector3.one, .6f).SetEase(Ease.OutSine);

        return;
        if (tween != null)
        {
            tween.Kill();
        }

        tween = DOVirtual.Float(speed, 0.2f, 1, (x) => { speed = x; }).SetEase(Ease.OutBack)
            .OnComplete(() => { isTweenDone = true; });
    }

    private void OnMouseUpAsButton()
    {
        if (Time.time - lastTimeClicked > 0.2f)
        {
            return;
        }
        OnPlayerClickBook?.Invoke();
    }


    
    private void OnMouseDrag()
    {
        if (!canHover) return;

        isDragging = true;
        var horizontal = Input.GetAxisRaw("Mouse X");
        rotationY += horizontal * rotationSpeed;
        if (horizontal != 0)
        {
            container.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
        }

        // Debug.Log("Horizontal: " + horizontal);
    }

    private void OnMouseDown()
    {
        Debug.Log("On Mouse Down");
        StopAllCoroutines();
        isMouseDown = true;
        lastTimeClicked = Time.time;
    }
    
    private void OnMouseUp()
    {
        Debug.Log("On Mouse Up");
        isMouseDown = false;
        isDragging = false;
    }

    private void Update()
    {
        FallbackToOriginalRotation();
    }

    private void FallbackToOriginalRotation()
    {
        if (isTweenDone || isMouseDown == false)
        {
            container.transform.localRotation = Quaternion.Lerp(container.transform.localRotation, Quaternion.Euler(0, 0, 0),
                Time.deltaTime * 5);
            rotationY = container.transform.localEulerAngles.y;
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
        float duration = 0.8f;
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