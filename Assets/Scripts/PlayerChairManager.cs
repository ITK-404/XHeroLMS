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
    public CinemachineCamera sitdownCamera;
    public CinemachineBrain brain;
    private ChairCheckPoint[] allCheckPoints;
    private List<ChairCheckPoint> chairList = new();
    public GameObject player;
    public PlayerState playerState;
    [Header("UI")]
    public LearnUI learnUI;
    public Canvas watchVideoCanvas;
    public Button sitdownBtn;
    public Button standupBtn;
    public static bool IsStantUp = false;

    VideoPlayerControllerPro videoPlayerControllerPro;

    private void Awake()
    {
        allCheckPoints = GetComponentsInChildren<ChairCheckPoint>();
        Instance = this;

        learnUI.OnClickReturnBtn += PlayerStandup;

        // videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        // videoPlayerControllerPro = FindObjectOfType<VideoPlayerControllerPro>(includeInactive: true);
        videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);
        watchVideoCanvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        learnUI.OnClickReturnBtn -= PlayerStandup;
    }

    private void Update()
    {
        if(currentCheckPoint == null)
        {
            sitdownBtn.gameObject.SetActive(false);
            standupBtn.gameObject.SetActive(false);
        }
        
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
        Debug.Log("Stand up");
        playerState = PlayerState.Free;
        sitdownCamera.Priority = 0;

        foreach (var item in allCheckPoints)
        {
            item.Show(true);
        }
        learnUI.Hide();
        watchVideoCanvas.gameObject.SetActive(false);
        IsStantUp = true;

        videoPlayerControllerPro.ExitFullscreenUI();
        StopAllCoroutines();
        StartCoroutine(WaitForBlendDone(() =>
        {
            Debug.Log("bật lại input");
            InputBlocker.SetBlocked(false);
            ShowSitdownButton();
        }));
    }

    public ChairCheckPoint currentCheckPoint;
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
    public void PlayerSitdown()
    {
        // sit down logic
   

        ChairCheckPoint temp = currentCheckPoint;
     
        if (temp != null)
        {
            playerState = PlayerState.Sitdown;
            Debug.Log("Sit down");
            sitdownCamera.transform.position = temp.checkPoint.transform.position;
            sitdownCamera.Priority = 2;

            foreach (var item in allCheckPoints)
            {
                item.Show(false);
            }

            StopAllCoroutines();
            InputBlocker.SetBlocked(true);

            StartCoroutine(WaitForBlendDone(() =>
            {
                ShowStandUpButton();
                watchVideoCanvas.gameObject.SetActive(true);
                learnUI.Show();
            }));
        }
    }


    public void ShowSitdownButton()
    {
        sitdownBtn.gameObject.SetActive(true);
        standupBtn.gameObject.SetActive(false);
    }

    public void ShowStandUpButton()
    {
        sitdownBtn.gameObject.SetActive(false);
        standupBtn.gameObject.SetActive(true);
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

}
