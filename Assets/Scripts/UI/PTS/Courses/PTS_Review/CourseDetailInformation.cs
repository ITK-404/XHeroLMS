using System;
using UnityEngine;

public class CourseDetailInformation : MonoBehaviour
{
   public ContainClassGroupUI classGroupUI;
   public InformationType classType;
   public Transform reviewTmp;
   [ContextMenu("Test")]
   private void Awake()
   {
      Show(classType);
   }

   public enum InformationType
   {
      JustInformation,
      ContainClass,
      NotContainClass
   }

   public void Show(InformationType type)
   {
      gameObject.SetActive(true);
      switch (type)
      {
         case InformationType.JustInformation:
            classGroupUI.Hide();
            break;
         case InformationType.ContainClass:
            classGroupUI.Show(classContainDate: true);
            break;
         case InformationType.NotContainClass:
            classGroupUI.Show(classContainDate: false);
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
      
      reviewTmp.gameObject.SetActive(type == InformationType.JustInformation);
   }

   public void Hide()
   {
      gameObject.SetActive(false);
   }
}