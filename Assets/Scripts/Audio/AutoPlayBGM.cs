using System.Collections;
using UnityEngine;

public class AutoPlayBGM : MonoBehaviour
{
    private static bool initDone = false;
    private void Awake()
    {
        if (!initDone)
        {
            initDone = true;
            var audioManager = AudioManager.Instance;
        }
    }

}