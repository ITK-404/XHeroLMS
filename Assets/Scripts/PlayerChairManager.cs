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
    public PlayerState playerState;
    [Header("UI")] VideoPlayerControllerPro videoPlayerControllerPro;
    CourseListView courseListView;
    [SerializeField] PlayerStandUI playerStandUI;
    public ChairCheckPoint currentCheckPoint;
    private bool ownsCourseGameplayLock;
    [SerializeField] private EnterFocusStateTransition enterFocusStateTransition;
    [SerializeField] private LearningFocusMode learningFocusMode;

    private void Awake()
    {
        allCheckPoints = GetComponentsInChildren<ChairCheckPoint>();
        Instance = this;

        // videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        // videoPlayerControllerPro = FindObjectOfType<VideoPlayerControllerPro>(includeInactive: true);
        ResolveRuntimeReferences();
    }

    private void OnDestroy()
    {
        SetCourseGameplayLock(false);
        Instance = null;
    }

    public void PlayerStandup()
    {
        ResolveRuntimeReferences();
        HideTutorialSitdownStandupUI();

        if (TutorialHandler.Instance != null &&
            TutorialHandler.Instance.CurrentStep == TutorialStepType.Standup)
        {
            TutorialHandler.Instance.Save();
        }

        StandUpStateHandle();

        if (learningFocusMode != null)
            learningFocusMode.Exit();
        else
            ExitLearningFocusFallback();

        StartBlendCoroutine(() =>
        {
            ResolveRuntimeReferences();
            Debug.Log("bật lại input");

            if (enterFocusStateTransition != null)
                enterFocusStateTransition.Exit();
        });
    }


    public void PlayerSitdown()
    {
        ResolveRuntimeReferences();
        // sit down logic, hardcode logic
        HideTutorialSitdownStandupUI();

        ChairCheckPoint temp = currentCheckPoint;

        if (temp == null) return;

        SitDownStateHandle(temp);

        if (enterFocusStateTransition != null)
            enterFocusStateTransition.Enter();
        else
            Debug.LogWarning("[PlayerChairManager] Missing EnterFocusStateTransition while sitting down.");

        StartBlendCoroutine(() =>
        {
            ResolveRuntimeReferences();

            if (learningFocusMode != null)
                learningFocusMode.Enter();
            else
                EnterLearningFocusFallback();

            ResumeCourseVideoAfterSitDown();
            SetTutorialToNextStep();
        });
    }

    private void SetTutorialToNextStep()
    {
        if (TutorialHandler.Instance != null &&
            TutorialHandler.Instance.CurrentStep == TutorialStepType.Sitdown)
        {
            TutorialHandler.Instance.SetCurrentStep(TutorialStepType.OpenLesson);
        }
    }

    private void SitDownStateHandle(ChairCheckPoint temp)
    {
        playerState = PlayerState.Sitdown;
        Debug.Log("Sit down");

        if (QuadCameraManager.Instance != null && temp.checkPoint != null)
            QuadCameraManager.Instance.SetupSitdownCameraByCheckPoint(temp.checkPoint.transform);

        if (QuadCinemachineController.Instance != null)
            QuadCinemachineController.Instance.ChangeState(ViewState.Sitdown);

        ShowAllCheckPoint(false);
        // d
        StopAllCoroutines();
        SetCourseGameplayLock(true);
    }

    private void StandUpStateHandle()
    {
        StopCourseVideoForStandUp();
        Debug.Log("Stand up");
        playerState = PlayerState.Free;
        if (QuadCinemachineController.Instance != null)
            QuadCinemachineController.Instance.ChangeState(ViewState.Player);
        ShowAllCheckPoint(true);
        SetCourseGameplayLock(false);
    }


    public void TrySetChair(ChairCheckPoint currentChair)
    {
        if (!chairList.Contains(currentChair))
        {
            chairList.Add(currentChair);
            FindBestChairCheckPoint();
        }
    }

    public void TryRemoveChair(ChairCheckPoint removeChair)
    {
        if (chairList.Contains(removeChair))
        {
            chairList.Remove(removeChair);
            FindBestChairCheckPoint();
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

    private void SetCourseGameplayLock(bool locked)
    {
        if (ownsCourseGameplayLock == locked)
            return;

        if (locked)
        {
            GameplayLock.Lock(GameplayLockReason.UI, GameplayLockTarget.All);
        }
        else
        {
            GameplayLock.Unlock(GameplayLockReason.UI);
        }

        ownsCourseGameplayLock = locked;
    }

    private void FindBestChairCheckPoint()
    {
        var playerForward = brain.transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        float bestScore = float.MinValue; // score = dot - khoảng cách có thể được cân nhắc riêng
        ChairCheckPoint temp = null;
        Vector3 dirToItem;
        foreach (var item in chairList)
        {
            dirToItem = item.transform.position - brain.transform.position;
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

    private void ResumeCourseVideoAfterSitDown()
    {
        if (courseListView == null)
            courseListView = FindFirstObjectByType<CourseListView>(FindObjectsInactive.Include);

        courseListView?.ResumeVideoAfterSitDownIfNeeded();
    }

    private void ShowAllCheckPoint(bool state)
    {
        if (allCheckPoints == null)
            allCheckPoints = GetComponentsInChildren<ChairCheckPoint>(true);

        foreach (var item in allCheckPoints)
        {
            if (item != null)
                item.Show(state);
        }
    }

    private void ResolveRuntimeReferences()
    {
        if (allCheckPoints == null || allCheckPoints.Length == 0)
            allCheckPoints = GetComponentsInChildren<ChairCheckPoint>(true);

        if (videoPlayerControllerPro == null)
            videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);

        if (courseListView == null)
            courseListView = FindFirstObjectByType<CourseListView>(FindObjectsInactive.Include);

        if (playerStandUI == null)
            playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);

        if (enterFocusStateTransition == null)
            enterFocusStateTransition = FindFirstObjectByType<EnterFocusStateTransition>(FindObjectsInactive.Include);

        if (learningFocusMode == null)
            learningFocusMode = FindFirstObjectByType<LearningFocusMode>(FindObjectsInactive.Include);
    }

    private static void HideTutorialSitdownStandupUI()
    {
        if (TutorialHandler.Instance != null && TutorialHandler.Instance.sitdownStandupUI != null)
            TutorialHandler.Instance.sitdownStandupUI.SetActive(false);
    }

    private void EnterLearningFocusFallback()
    {
        if (playerStandUI != null)
            playerStandUI.ShowLearningUI();

        if (videoPlayerControllerPro != null)
            videoPlayerControllerPro.EnterFullscreenUI();
    }

    private void ExitLearningFocusFallback()
    {
        if (playerStandUI != null)
            playerStandUI.HideLearningUI();

        if (videoPlayerControllerPro != null)
            videoPlayerControllerPro.ExitFullscreenUI();
    }
}
