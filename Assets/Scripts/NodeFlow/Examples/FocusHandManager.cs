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
        StartCoroutine(WaitForLoad(targetAnchor, focusText));
    }

    private IEnumerator WaitForLoad(RectTransform targetAnchor, string focusText)
    {
        container.gameObject.SetActive(true);
        anchorPoint.position = targetAnchor.position;
        
        yield return new WaitForSecondsRealtime(0.1f);
        labelAutoPlacer.SetText(focusText);
        labelAutoPlacer.Reposition();
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}