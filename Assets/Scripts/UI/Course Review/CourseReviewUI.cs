using System;
using UnityEngine;

public class CourseReviewUI : MonoBehaviour
{
   public static CourseReviewUI Instance { get; private set; }
   public GameObject container;
   public TabItemManagerUI tabItemManagerUI;
   private void Awake()
   {
      if (Instance != null && Instance != this)
      {
         Destroy(gameObject);
         return;
      }
      Instance = this;
      Hide();
   }

   public void ReviewBook(BookHandler bookHandler)
   {
      // xử lý review
      Show();
      tabItemManagerUI.Hide();
      Debug.Log($"Book Name {bookHandler.book_name}");
      Debug.Log($"Book SKU {bookHandler.book_sku}");
      Debug.Log($"Book Seo {bookHandler.book_seo}");
   }

   private void Show()
   {
      container.gameObject.SetActive(true);
   }

   public void Hide()
   {
      container.gameObject.SetActive(false);
   }
}
