using System.Collections;
using UnityEngine;

public class AutoPlayBGM : MonoBehaviour
{
    private void Awake()
    {
        AudioManager.Instance.CreateSound(null);
    }
}