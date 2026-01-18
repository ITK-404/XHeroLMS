using System.Collections.Generic;
using UnityEngine;

public class IOSReviewChecker : MonoBehaviour
{
    [SerializeField] private List<GameObject> checkerList = new();
    public void Start()
    {
        bool canShow = AppDataGlobal.isInReviewMode == false;
        foreach (var item in checkerList)
        {
            item.gameObject.SetActive(canShow);
        }
    }
}