using System;
using DG.Tweening;
using UnityEngine;

public class AutoFadeCanvasGroup : MonoBehaviour
{
   private CanvasGroup _canvasGroup;

   private void Awake()
   {
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void OnEnable()
   {
      Fade(1, 0.1f);
   }

   private void OnDisable()
   {
      _canvasGroup.DOKill();
   }

   private void Fade(float fade, float time = 0.1f)
   {
      _canvasGroup.DOKill();
      _canvasGroup.DOFade(fade, time);
   }
}
