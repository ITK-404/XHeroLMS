using System;
using UnityEngine;

public class ReviewBigArea : MonoBehaviour
{
    [SerializeField] private ReviewBigAreaUI _reviewBigAreaUI;
    [SerializeField] private PlotHandlerUI plotHandlerUI;
    private void Awake()
    {
        _reviewBigAreaUI.Hide();

        plotHandlerUI.OnClickShowReviewBigArea += Show;
    }

    private void OnDestroy()
    {
        plotHandlerUI.OnClickShowReviewBigArea -= Show;
    }

    public void Show()
    {
        var bigArea = AreaDisplayManager.Instance.SelectArea;
        if (bigArea == null)
        {
            Debug.LogError("Big Area is null, please checkout");
            return;
        }
        
        _reviewBigAreaUI.Show(bigArea);
    }
}