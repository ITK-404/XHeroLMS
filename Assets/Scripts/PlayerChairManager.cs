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
    public PlayerStandUI playerStandUI;
    public ChairCheckPoint currentCheckPoint;
    
    private void Awake()
    {
        allCheckPoints = GetComponentsInChildren<ChairCheckPoint>();
        Instance = this;


        // videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        // videoPlayerControllerPro = FindObjectOfType<VideoPlayerControllerPro>(includeInactive: true);
        videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);
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
        QuadCinemachineController.Instance.ChangeState(ViewState.Player);
        foreach (var item in allCheckPoints)
        {
            item.Show(true);
        }
        // ẩn UI ngay khi bắt đầu đứng dậy
        playerStandUI.UILearnCanvas.Hide();
        playerStandUI.HideWatchVideoUI();

        videoPlayerControllerPro.ExitFullscreenUI();
        StopAllCoroutines();
        StartCoroutine(WaitForBlendDone(() =>
        {
            Debug.Log("bật lại input");
            InputBlocker.SetBlocked(false);
            
        }));
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
    
    public void PlayerSitdown()
    {
    
        // sit down logic
        
        ChairCheckPoint temp = currentCheckPoint;
     
        if (temp != null)
        {
            playerState = PlayerState.Sitdown;
            Debug.Log("Sit down");
            QuadCameraManager.Instance.ChangeToSitdownCameraState(temp.checkPoint.transform.position);
            QuadCinemachineController.Instance.ChangeState(ViewState.Sitdown);
            
            // ẩn tất cả icon của item
            foreach (var item in allCheckPoints)
            {
                item.Show(false);
            }
            // d
            StopAllCoroutines();
            InputBlocker.SetBlocked(true);
            
            StartCoroutine(WaitForBlendDone(() =>
            {
                // Hiện UI ngay sau khi ngồi xuống hoàn tất
                playerStandUI.ShowWatchVideoUI();
                playerStandUI.UILearnCanvas.Show();
            }));
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

}