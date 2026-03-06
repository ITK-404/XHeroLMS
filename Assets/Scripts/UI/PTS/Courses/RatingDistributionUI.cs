using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RatingDistributionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI percentRatingTmp;
    [SerializeField] private TextMeshProUGUI starTmp;
    [SerializeField] private Slider ratingBar;

    public void SetRating(string percent, string star, float ratio)
    {
        if (percentRatingTmp != null)
            percentRatingTmp.text = percent;

        if (starTmp != null)
            starTmp.text = star;

        if (ratingBar != null)
        {
            ratingBar.minValue = 0f;
            ratingBar.maxValue = 1f;
            ratingBar.wholeNumbers = false;
            ratingBar.value = Mathf.Clamp01(ratio);
        }
    }
}