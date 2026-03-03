using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PTS_SimpleCourseUI : MonoBehaviour
{
    [SerializeField] private Button panelBtn;
    [SerializeField] private Button directBtn;

    [SerializeField] private Image bgImg;
    private void Awake()
    {
        panelBtn.onClick.AddListener(OnLoadImg);
        directBtn.onClick.AddListener(OnLoadImg);
    }

    private void OnDestroy()
    {
        panelBtn.onClick.RemoveListener(OnLoadImg);
        directBtn.onClick.RemoveListener(OnLoadImg);
    }

    private void OnLoadImg()
    {
        bgImg.DOFade(1, 0.5f).OnComplete(() =>
        {
            bgImg.DOFade(0, 0.3f).SetDelay(0.2f);
        });
    }
}