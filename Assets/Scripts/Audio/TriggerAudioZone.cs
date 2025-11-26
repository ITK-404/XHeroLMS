using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAudioZone : MonoBehaviour
{
    private AudioZoneElement[] audioSources;
    [SerializeField] private float fadeDuration = 0.1f;

    private bool isTrigger = false;
    private void Awake()
    {
        audioSources = GetComponentsInChildren<AudioZoneElement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        // create audio;
        Debug.Log("bắt đầu chạy audio");
        foreach (var item in audioSources)
        {
            item.FadeIn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        // delete audio
        Debug.Log("bắt đầu tắt audio");

        foreach (var item in audioSources)
        {
            item.FadeOut();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }

}
