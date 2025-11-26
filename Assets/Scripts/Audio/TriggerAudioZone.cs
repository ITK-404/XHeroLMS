using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAudioZone : MonoBehaviour
{
    private AudioSource[] audioSources;

    private void Awake()
    {
        audioSources = GetComponentsInChildren<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // create audio;
        foreach (var item in audioSources)
        {
            item.Play();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // delete audio
        foreach (var item in audioSources)
        {
            item.Pause();
        }
    }

}