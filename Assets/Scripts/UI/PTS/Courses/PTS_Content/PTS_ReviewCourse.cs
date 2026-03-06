using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PTS_ReviewCourse : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    [SerializeField] private bool clearOldItems = true;

    [Header("Left - Rating Distribution")]
    [SerializeField] private FrameReviewCourseUI frameReviewCourseUI;
    [SerializeField] private Transform ratingDistributionParent;
    [SerializeField] private RatingDistributionUI ratingDistributionPrefab;

    [Header("Right - Review List")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private ReviewCommentUI reviewPrefab;
    [SerializeField] private Sprite fallbackAvatar;

    private readonly List<RatingDistributionUI> spawnedRatingItems = new();
    private readonly List<ReviewCommentUI> spawnedReviewItems = new();

    private void OnEnable()
    {
        CourseReviewStaticStore.OnChanged += Refresh;

        if (autoRefreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        CourseReviewStaticStore.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (CourseReviewStaticStore.IsLoading)
        {
            ClearAllReviewItems();
            ClearAllRatingDistributionItems();
            ResetSummaryUI();
            return;
        }

        if (!string.IsNullOrEmpty(CourseReviewStaticStore.LastError))
        {
            Debug.LogWarning("[PTS_ReviewCourse] Review error: " + CourseReviewStaticStore.LastError);
            ClearAllReviewItems();
            ClearAllRatingDistributionItems();
            ResetSummaryUI();
            return;
        }

        var storeReviews = CourseReviewStaticStore.Reviews;
        var stats = CourseReviewStaticStore.Statistics;

        if ((storeReviews == null || storeReviews.Count == 0) && stats == null)
        {
            ClearAllReviewItems();
            ClearAllRatingDistributionItems();
            ResetSummaryUI();
            return;
        }

        var reviews = ConvertReviews(storeReviews);

        if (stats != null)
        {
            BuildSummaryAndDistributionFromStatistics(stats);
        }
        else
        {
            BuildSummaryAndDistributionFromReviews(reviews);
        }

        if (reviews == null || reviews.Count == 0)
        {
            ClearAllReviewItems();
            return;
        }

        BuildReviewItems(reviews);
    }

    private List<CourseReviewData> ConvertReviews(List<LmsCourseReviewItem> source)
    {
        if (source == null || source.Count == 0)
            return new List<CourseReviewData>();

        var result = new List<CourseReviewData>();

        for (int i = 0; i < source.Count; i++)
        {
            var r = source[i];
            if (r == null) continue;

            string userName = (r.author != null && !string.IsNullOrEmpty(r.author.fullName))
                ? r.author.fullName
                : "Ẩn danh";

            string avatarUrl = (r.author != null) ? r.author.avatar : "";
            string comment = string.IsNullOrEmpty(r.content) ? "" : r.content;
            int rating = Mathf.Clamp(r.stars, 1, 5);

            result.Add(new CourseReviewData
            {
                userName = userName,
                avatarUrl = avatarUrl,
                comment = comment,
                rating = rating,
                createdAt = !string.IsNullOrEmpty(r.createdAt) ? r.createdAt : r.updatedAt,
                imageUrls = r.files ?? new List<string>()
            });
        }

        return result;
    }

    private void BuildSummaryAndDistributionFromStatistics(ReviewStatistics stats)
    {
        int totalReview = stats != null ? stats.total : 0;
        float average = stats != null ? stats.rate : 0f;

        int count1 = stats?.starCounts?._1 ?? 0;
        int count2 = stats?.starCounts?._2 ?? 0;
        int count3 = stats?.starCounts?._3 ?? 0;
        int count4 = stats?.starCounts?._4 ?? 0;
        int count5 = stats?.starCounts?._5 ?? 0;

        SetSummaryUI(average, totalReview);
        BuildRatingDistribution(count1, count2, count3, count4, count5, totalReview);
    }

    private void BuildSummaryAndDistributionFromReviews(List<CourseReviewData> reviews)
    {
        if (reviews == null || reviews.Count == 0)
        {
            SetSummaryUI(0f, 0);
            BuildRatingDistribution(0, 0, 0, 0, 0, 0);
            return;
        }

        int count1 = 0;
        int count2 = 0;
        int count3 = 0;
        int count4 = 0;
        int count5 = 0;

        float totalStar = 0f;
        int totalReview = reviews.Count;

        for (int i = 0; i < reviews.Count; i++)
        {
            int star = Mathf.Clamp(reviews[i].rating, 1, 5);
            totalStar += star;

            switch (star)
            {
                case 1: count1++; break;
                case 2: count2++; break;
                case 3: count3++; break;
                case 4: count4++; break;
                case 5: count5++; break;
            }
        }

        float average = totalReview > 0 ? totalStar / totalReview : 0f;

        SetSummaryUI(average, totalReview);
        BuildRatingDistribution(count1, count2, count3, count4, count5, totalReview);
    }

    private void SetSummaryUI(float average, int totalReview)
    {
        if (frameReviewCourseUI == null) return;

        frameReviewCourseUI.SetAverageRating(average.ToString("0.0"));
        frameReviewCourseUI.SetRatingCount(totalReview);
        frameReviewCourseUI.SetAverageStars(average);
    }

    private void BuildRatingDistribution(
        int count1,
        int count2,
        int count3,
        int count4,
        int count5,
        int totalReview)
    {
        ClearAllRatingDistributionItems();

        if (ratingDistributionParent == null || ratingDistributionPrefab == null)
            return;

        CreateRatingDistributionItem(5, count5, totalReview);
        CreateRatingDistributionItem(4, count4, totalReview);
        CreateRatingDistributionItem(3, count3, totalReview);
        CreateRatingDistributionItem(2, count2, totalReview);
        CreateRatingDistributionItem(1, count1, totalReview);
    }

    private void CreateRatingDistributionItem(int star, int count, int totalReview)
    {
        var item = Instantiate(ratingDistributionPrefab, ratingDistributionParent);
        spawnedRatingItems.Add(item);

        float ratio = totalReview > 0 ? (float)count / totalReview : 0f;
        string percentText = totalReview > 0 ? $"{Mathf.RoundToInt(ratio * 100f)}%" : "0%";
        string starText = $"{star}.0";

        item.SetRating(percentText, starText, ratio);
    }

    private void BuildReviewItems(List<CourseReviewData> reviews)
    {
        if (clearOldItems)
            ClearAllReviewItems();

        if (contentParent == null || reviewPrefab == null)
            return;

        for (int i = 0; i < reviews.Count; i++)
        {
            var review = reviews[i];
            var item = Instantiate(reviewPrefab, contentParent);
            item.gameObject.SetActive(true);
            spawnedReviewItems.Add(item);

            string displayName = string.IsNullOrEmpty(review.userName) ? "Ẩn danh" : review.userName;
            string displayDate = FormatDate(review.createdAt);
            string displayRating = Mathf.Clamp(review.rating, 1, 5).ToString("0.0");
            string displayComment = string.IsNullOrEmpty(review.comment) ? "" : review.comment;

            item.SetComment(
                displayName,
                displayDate,
                displayRating,
                displayComment,
                review.avatarUrl,
                review.imageUrls,
                fallbackAvatar
            );
        }
    }

    private void ClearAllReviewItems()
    {
        for (int i = 0; i < spawnedReviewItems.Count; i++)
        {
            if (spawnedReviewItems[i] != null)
                Destroy(spawnedReviewItems[i].gameObject);
        }

        spawnedReviewItems.Clear();

        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void ClearAllRatingDistributionItems()
    {
        for (int i = 0; i < spawnedRatingItems.Count; i++)
        {
            if (spawnedRatingItems[i] != null)
                Destroy(spawnedRatingItems[i].gameObject);
        }

        spawnedRatingItems.Clear();

        if (ratingDistributionParent == null) return;

        for (int i = ratingDistributionParent.childCount - 1; i >= 0; i--)
        {
            Destroy(ratingDistributionParent.GetChild(i).gameObject);
        }
    }

    private void ResetSummaryUI()
    {
        if (frameReviewCourseUI != null)
            frameReviewCourseUI.ResetUI();
    }

    private string FormatDate(string isoDate)
    {
        if (string.IsNullOrEmpty(isoDate))
            return "";

        if (DateTime.TryParse(isoDate, out DateTime dt))
            return dt.ToString("dd/MM/yyyy");

        return isoDate;
    }
}

[Serializable]
public class CourseReviewData
{
    public string userName;
    public string avatarUrl;
    public string comment;
    public int rating;
    public string createdAt;
    public List<string> imageUrls;
}