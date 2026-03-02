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
        percentRatingTmp.text = percent;
        starTmp.text = star;
        ratingBar.value = ratio;
    }
}