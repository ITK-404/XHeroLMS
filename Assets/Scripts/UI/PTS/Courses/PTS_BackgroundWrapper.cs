using System;
using UnityEngine;
using UnityEngine.UI;

public enum PTS_Image
{
    Courses,
    Detail
}
public class PTS_BackgroundWrapper : MonoBehaviour
{
    [SerializeField] private Image wrapperImg;

    [SerializeField] private Sprite detailSprite;
    [SerializeField] private Sprite coursesSprite;

    public void Switch(PTS_Image imgType)
    {
        wrapperImg.sprite = imgType == PTS_Image.Courses ? coursesSprite : detailSprite;
    }
    
}