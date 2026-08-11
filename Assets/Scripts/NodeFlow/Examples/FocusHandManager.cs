using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FocusHandManager : MonoBehaviour
{
    public static FocusHandManager Instance;
    
    [SerializeField] private LabelAutoPlacer labelAutoPlacer;
    [SerializeField] private RectTransform anchorPoint;
    [SerializeField] private GameObject container;
    [SerializeField] private Vector2 minAnchorSize = new Vector2(40f, 40f);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Instance = null;
    }   
    

    public void SetToTargetRect(RectTransform targetAnchor, string focusText)
    {
        this.targetAnchor = targetAnchor;
        StartCoroutine(WaitForLoad(targetAnchor, focusText));
    }

    private RectTransform targetAnchor;

    private void LateUpdate()
    {
        if (targetAnchor == null) return;
        anchorPoint.position = targetAnchor.position;
        UpdateAnchorSize();
    }

    private void UpdateAnchorSize()
    {
        Vector2 targetSize = targetAnchor.rect.size;
        anchorPoint.sizeDelta = new Vector2(
            Mathf.Max(targetSize.x, minAnchorSize.x),
            Mathf.Max(targetSize.y, minAnchorSize.y)
        );
    }

    private IEnumerator WaitForLoad(RectTransform targetAnchor, string focusText)
    {
        container.gameObject.SetActive(true);
        anchorPoint.position = targetAnchor.position;
        UpdateAnchorSize();
        
        yield return new WaitForSecondsRealtime(0.2f);
        labelAutoPlacer.SetText(focusText);
        labelAutoPlacer.Reposition();
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}