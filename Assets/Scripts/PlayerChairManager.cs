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
    private List<ChairCheckPoint> chairList = new();
    private ChairCheckPoint[] allCheckPoints;
    public CursorGameManager cursorMgr;
    public GameObject player;
    public PlayerState playerState;

    private void Awake()
    {
        allCheckPoints = GetComponentsInChildren<ChairCheckPoint>();
        Instance = this;
    }

    public void TrySetChair(ChairCheckPoint currentChair)
    {
        if (!chairList.Contains(currentChair))
        {
            chairList.Add(currentChair);
        }
    }

    public void TryRemoveChair(ChairCheckPoint removeChair)
    {
        if (chairList.Contains(removeChair))
        {
            chairList.Remove(removeChair);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && playerState == PlayerState.Free)
        {
            PlayerSitdown();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && playerState == PlayerState.Sitdown)
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
    private void PlayerStandup()
    {
        Debug.Log("Stand up");
        playerState = PlayerState.Free;
        sitdownCamera.Priority = 0;

        foreach (var item in allCheckPoints)
        {
            item.Show(true);
        }

        StopAllCoroutines();
        StartCoroutine(WaitForBlendDone(() =>
        {
            Debug.Log("bật lại input");
            if (cursorMgr) cursorMgr.SetUIOpen(false);
            InputBlocker.SetBlocked(false);
        }));
    }

    private void PlayerSitdown()
    {
        // sit down logic
        var playerForward = brain.transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        ChairCheckPoint temp = null;
        float bestScore = float.MinValue; // score = dot - khoảng cách có thể được cân nhắc riêng

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
                if (cursorMgr) cursorMgr.SetUIOpen(true);
            }));
        }
    }
}
