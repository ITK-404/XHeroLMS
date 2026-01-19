using System.Collections.Generic;
using UnityEngine;

public class IOSReviewChecker : MonoBehaviour
{
    [SerializeField] private List<GameObject> checkerList = new();

    private void OnEnable()
    {
        AppDataGlobal.OnReviewModeChanged += Handle;
        Handle(AppDataGlobal.isInReviewMode);
    }

    private void OnDisable()
    {
        AppDataGlobal.OnReviewModeChanged -= Handle;
    }

    private void Handle(bool isInReview)
    {
        bool canShow = !isInReview;
        foreach (var item in checkerList)
        {
            if (item != null) item.SetActive(canShow);
        }
    }
}
