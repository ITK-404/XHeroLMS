using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class AutoReplayController : MonoBehaviour
{
    [FormerlySerializedAs("askForReplayAnimation")] [SerializeField] private SmokeTransitionAnimation smokeTransitionAnimation;

    public IEnumerator WaitForLoading()
    {
        yield return smokeTransitionAnimation.StartTransitionAsync().ToCoroutine();
        LoadingTransition.Load_Scene(SceneManager.GetActiveScene().name);
    }
}