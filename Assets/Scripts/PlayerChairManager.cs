using DG.Tweening;
using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PlayerChairManager : MonoBehaviour
{
    public enum PlayerState
    {
        Free,
        Sitdown
    }
    public static PlayerChairManager Instance;
    public CinemachineBrain brain;
    private ChairCheckPoint[] allCheckPoints;
    private List<ChairCheckPoint> chairList = new();
    public GameObject player;
    public PlayerState playerState;
    [Header("UI")]

    VideoPlayerControllerPro videoPlayerControllerPro;
    CourseListView courseListView;
    [SerializeField] PlayerStandUI playerStandUI;
    [SerializeField] InputCanvas inputCanvas;
    [SerializeField] private CourseExitWayHandler courseExitWayHandler;
    public ChairCheckPoint currentCheckPoint;

    private TutorialBase TutorialBase;

    private void Awake()
    {
        allCheckPoints = GetComponentsInChildren<ChairCheckPoint>();
        Instance = this;


        // videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        // videoPlayerControllerPro = FindObjectOfType<VideoPlayerControllerPro>(includeInactive: true);
        videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);
        courseListView = FindFirstObjectByType<CourseListView>(FindObjectsInactive.Include);
        playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);
        courseExitWayHandler.Show();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private IEnumerator WaitForBlendDone(Action action)
    {
        yield return new WaitForSeconds(2);

        Debug.Log("Chạy callback");
        action?.Invoke();
        action = null;

        yield return null;
    }
    public void PlayerStandup()
    {

        TutorialHandler.Instance.sitdownStandupUI.gameObject.SetActive(false);

        if (TutorialHandler.Instance.CurrentStep == TutorialStepType.Standup)
        {
            TutorialHandler.Instance.Save();
        }

        StopCourseVideoForStandUp();
        Debug.Log("Stand up");
        playerState = PlayerState.Free;
        QuadCinemachineController.Instance.ChangeState(ViewState.Player);
        ShowAllCheckPoints(true);
        // ẩn UI ngay khi bắt đầu đứng dậy
        //playerStandUI.UILearnCanvas.Hide();
        //playerStandUI.HideWatchVideoUI();
        playerStandUI.HideLearningUI();
        videoPlayerControllerPro.ExitFullscreenUI();
        InputBlocker.SetBlocked(false);
        StartBlendCoroutine(() =>
        {
            Debug.Log("bật lại input");
            
            playerStandUI.returnBtn.gameObject.SetActive(true);
            inputCanvas.Show();

            courseExitWayHandler.Show();

            PlayerPanelUI.Instance.ShowUnLoginContainer(true);
        });
    }

    private void StopCourseVideoForStandUp()
    {
        if (courseListView == null)
            courseListView = FindFirstObjectByType<CourseListView>(FindObjectsInactive.Include);

        if (courseListView != null)
        {
            courseListView.StopVideoAndAudioForStandUp();
            return;
        }

        if (videoPlayerControllerPro == null)
            videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);

        videoPlayerControllerPro?.PauseVideoAndAudioForStandUp();
    }

    private void Recalculator()
    {
        var playerForward = brain.transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();
        float bestScore = float.MinValue; // score = dot - khoảng cách có thể được cân nhắc riêng
        ChairCheckPoint temp = null;
        foreach (var item in chairList)
        {
            Vector3 dirToItem = item.transform.position - brain.transform.position;
            dirToItem.y = 0;
            dirToItem.Normalize();

            float dot = Vector3.Dot(playerForward, dirToItem);
            if (dot > 0.5f) // nằm trong tầm nhìn ~60 độ
            {
                float distance = Vector3.Distance(brain.transform.position, item.transform.position);
                float score = dot / distance; // dot cao, distance nhỏ => ưu tiên cao

                if (score > bestScore)
                {
                    bestScore = score;
                    temp = item;
                }
            }
        }
        currentCheckPoint = temp;

    }

    private float timer;
    private float blockTimer = 2.3f;
    private void UpdateBlockTimer() => timer = Time.time;
    private bool CanInteract() => Time.time > timer + blockTimer;
    public void PlayerSitdown()
    {
        // sit down logic
        TutorialHandler.Instance.sitdownStandupUI.gameObject.SetActive(false);
    
        ChairCheckPoint temp = currentCheckPoint;

        if (temp != null)
        {
            playerState = PlayerState.Sitdown;
            Debug.Log("Sit down");
            
            QuadCameraManager.Instance.SetupSitdownCameraByCheckPoint(temp.checkPoint.transform);

            QuadCinemachineController.Instance.ChangeState(ViewState.Sitdown);
            inputCanvas.Hide();

            // ẩn tất cả icon của ghế
            ShowAllCheckPoints(false);
            // d
            StopAllCoroutines();
            InputBlocker.SetBlocked(true);
            
            courseExitWayHandler.Hide();
            
            PlayerPanelUI.Instance.ShowUnLoginContainer(false);
            StartBlendCoroutine(() =>
            {
                // Hiện UI ngay sau khi ngồi xuống hoàn tất
                //playerStandUI.ShowWatchVideoUI();
                //playerStandUI.UILearnCanvas.Show();
                playerStandUI.ShowLearningUI();
                videoPlayerControllerPro.EnterFullscreenUI();
                playerStandUI.returnBtn.gameObject.SetActive(false);
                if (TutorialHandler.Instance.CurrentStep == TutorialStepType.Sitdown)
                {
                    TutorialHandler.Instance.SetCurrentStep(TutorialStepType.OpenLesson);
                }
            });
        }
    }

    
    public void TrySetChair(ChairCheckPoint currentChair)
    {
        if (!chairList.Contains(currentChair))
        {
            chairList.Add(currentChair);
            Recalculator();
        }
    }

    public void TryRemoveChair(ChairCheckPoint removeChair)
    {
        if (chairList.Contains(removeChair))
        {
            chairList.Remove(removeChair);
            Recalculator();
        }
    }
    private Coroutine _blendCoroutine;
    private int _blendToken = 0; // tăng mỗi lần gọi, callback tự check có còn valid không

    private IEnumerator WaitForBlendDone(Action action, int token)
    {
        yield return new WaitForSeconds(2f);

        // Nếu token không còn khớp => có lệnh mới override rồi, bỏ qua
        if (token != _blendToken) yield break;

        Debug.Log("Chạy callback");
        action?.Invoke();
    }

    private void StartBlendCoroutine(Action action)
    {
        if (_blendCoroutine != null)
            StopCoroutine(_blendCoroutine);

        _blendToken++;
        _blendCoroutine = StartCoroutine(WaitForBlendDone(action, _blendToken));
    }
     
    public void OnSitdownUI_Immediate()
    {
        inputCanvas.Hide();
        courseExitWayHandler.Hide();
        PlayerPanelUI.Instance.ShowUnLoginContainer(false);
        playerStandUI.returnBtn.gameObject.SetActive(false);
    }

    private void OnSitdownUI_Deferred()
    {
        playerStandUI.ShowLearningUI();
        videoPlayerControllerPro.EnterFullscreenUI();
    }

    private void OnStandupUI_Immediate()
    {
        playerStandUI.HideLearningUI();
        videoPlayerControllerPro.ExitFullscreenUI();
    }

    public void OnStandupUI_Deferred()
    {
        inputCanvas.Show();
        courseExitWayHandler.Show();
        PlayerPanelUI.Instance.ShowUnLoginContainer(true);
        playerStandUI.returnBtn.gameObject.SetActive(true);
    }

    public void ShowAllCheckPoints(bool b)
    {
        foreach (var item in allCheckPoints)
        {
            item.Show(b);
        }
    }
}
