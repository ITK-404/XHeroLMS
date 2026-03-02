using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReviewCommentUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameTmp;
    [SerializeField] private TextMeshProUGUI dateCommentTmp;
    [SerializeField] private TextMeshProUGUI ratingTmp;
    [SerializeField] private TextMeshProUGUI commentTmp;
    
    [SerializeField] private Image avatarImg;
    public void SetComment(string name, string date, string rating)
    {
        nameTmp.text = name;
        dateCommentTmp.text = date;
        ratingTmp.text = rating;
    }

}
