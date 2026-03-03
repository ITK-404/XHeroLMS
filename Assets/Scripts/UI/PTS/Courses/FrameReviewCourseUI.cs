using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FrameReviewCourseUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI averageRatingTmp;

    [SerializeField] private TextMeshProUGUI ratingCountTmp;

    // list ui thanh tỉ lệ đánh giá
    [SerializeField] private List<RatingDistributionUI> ratingDistributionUis;
    [SerializeField] private List<Image> averageRatingList = new();

    private void Awake()
    {
        SetRatingCount(35);
        SetAverageRating("3.5");
        SetAverageStars(3.5f);
    }

    // New, clearer API: sets the rating count text (e.g. "(123)")
    public void SetRatingCount(int count)
    {
        ratingCountTmp.text = $"Đánh giá {count}";
    }

    // New, clearer API: sets the average rating text (e.g. "4.5")
    public void SetAverageRating(string text)
    {
        averageRatingTmp.text = text;
    }

    // New, clearer API: updates the 5 horizontal star images to reflect the rating (0-5)
    public void SetAverageStars(float rating)
    {
        rating = Mathf.Clamp(rating, 0, 5);
        for (int i = 0; i < averageRatingList.Count; i++)
        {
            float fill = Mathf.Clamp01(rating - i);

            var img = averageRatingList[i];

            img.DOKill();
            img.DOFillAmount(fill, 0.2f);
        }
    }
}