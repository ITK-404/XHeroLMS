using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

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
    public CursorGameManager cursorMgr;
    public GameObject player;
    public PlayerState playerState;
    [Header("UI")]
    public LearnUI learnUI;
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

    }

    private void OnDestroy()
    {
        learnUI.OnClickReturnBtn -= PlayerStandup;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && playerState == PlayerState.Sitdown)
        {
            PlayerStandup();
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
        IsStantUp = true;

        videoPlayerControllerPro.ExitFullscreenUI();
        StopAllCoroutines();
        StartCoroutine(WaitForBlendDone(() =>
        {
            Debug.Log("bật lại input");
            if (cursorMgr) cursorMgr.SetUIOpen(false);
            InputBlocker.SetBlocked(false);
        }));
    }

    public void PlayerSitdown(ChairCheckPoint temp)
    {
        // sit down logic
        if(temp == null)
        {
            Debug.Log("Chair Check point bị null");
            return;
        }


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
            IsStantUp = false;

            StartCoroutine(WaitForBlendDone(() =>
            {
                if (cursorMgr) cursorMgr.SetUIOpen(true);
                learnUI.Show();
                
            }));
        }
    }
}
