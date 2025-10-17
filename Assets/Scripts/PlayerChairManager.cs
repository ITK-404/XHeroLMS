using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayerChairManager : MonoBehaviour
{
    public static PlayerChairManager Instance;
    private List<ChairCheckPoint> chairList = new();
    public GameObject player;
    private void Awake()
    {
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
        if (Input.GetKeyDown(KeyCode.F))
        {
            // sit down logic
            ChairCheckPoint temp = null;
            float minDistacne = float.MaxValue;
            foreach(var item in chairList)
            {
                var itemDistance = Vector3.Distance(player.transform.position, item.transform.position);
                if(itemDistance < minDistacne)
                {
                    temp = item;
                    itemDistance = minDistacne;
                }
            }

            if(temp != null)
            {
                Debug.Log("Sit down");
            }
        }
    }
}
