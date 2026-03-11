using System;
using UnityEngine;
using UnityEngine.UI;

public class PTS_ButtonGroupHandle : MonoBehaviour
{
    public enum State
    {
        None, 
        Brief,
        Detail
    }

    [SerializeField] private Button shareBtn;
    [SerializeField] private Button buyCourseBtn;
    [SerializeField] private Button goToDetailBtn;
    public Action GoToDetailClickEvent;
    public Action SharedClickEvent;
    public Action BuyCourseClickEvent;
    
    private void Awake()
    {
        goToDetailBtn.onClick.AddListener(GoDetail);
    }

    private void OnDestroy()
    {
        goToDetailBtn.onClick.RemoveListener(GoDetail);
    }

    private void GoDetail()
    {
        GoToDetailClickEvent?.Invoke();
    }

    public void TryShow(State state)
    {
        switch (state)
        {
            case State.None:
                shareBtn.gameObject.SetActive(false);
                buyCourseBtn.gameObject.SetActive(false);
                goToDetailBtn.gameObject.SetActive(false);
                break;
            case State.Brief:
                shareBtn.gameObject.SetActive(false);
                buyCourseBtn.gameObject.SetActive(true);
                goToDetailBtn.gameObject.SetActive(true);
                break;
            case State.Detail:
                shareBtn.gameObject.SetActive(true);
                buyCourseBtn.gameObject.SetActive(true);
                goToDetailBtn.gameObject.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}