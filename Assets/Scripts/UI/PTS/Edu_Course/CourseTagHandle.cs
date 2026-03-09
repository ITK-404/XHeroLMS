using System;
using UnityEngine;
using UnityEngine.UI;
public enum CourseTag
{
    Offline,
    Online,
    Zoom
}
public class CourseTagHandle : MonoBehaviour
{
    [SerializeField] private Image offlineImg;
    [SerializeField] private Image onlineImg;
    [SerializeField] private Image zoomImg;

    [SerializeField] private CourseTag _tag;

    private void Awake()
    {
        Show(_tag);
    }

    public void Show(CourseTag newTag)
    {
        _tag = newTag;
      
        offlineImg.gameObject.SetActive(false);
        onlineImg.gameObject.SetActive(false);
        zoomImg.gameObject.SetActive(false);
      
        var image = GetImage(newTag);
        image.gameObject.SetActive(true);
    }

    [ContextMenu("Update Current Tag")]
    private void UpdateCurrentTag()
    {
        Show(_tag);
    }

    private Image GetImage(CourseTag courseTag)
    {
        switch (courseTag)
        {
            case CourseTag.Offline:
                return offlineImg;
            case CourseTag.Online:
                return onlineImg;
            case CourseTag.Zoom:
                return zoomImg;
        }

        return null;
    }
}