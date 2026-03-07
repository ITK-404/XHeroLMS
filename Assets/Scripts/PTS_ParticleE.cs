using System;
using System.Collections.Generic;
using UnityEngine;

public class PTS_ParticleE : MonoBehaviour
{
    [SerializeField] private List<ParticleSystem> activeParticleList = new();
    private void Awake()
    {
        //DeActive();
    }

    public void Active()
    {
        Show(true);
    }

    public void DeActive()
    {
        Show(false);
    }

    public void Show(bool isEnable)
    {
        foreach (var item in activeParticleList)
        {
            gameObject.SetActive(isEnable);
        }
    }
    
}
