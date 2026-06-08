using UnityEngine;

public class KyMon_WorldSpaceUI : WorldSpaceUI
{
   [SerializeField] private Transform container;
   [SerializeField] private float minSize = 0.9f;
   [SerializeField] private float maxSize = 1f;
   [SerializeField] private Vector3 offset;
   
   private Transform target;
   private Transform player;

   public void SetPlayer(Transform player) => this.player = player;
   public void SetTarget(Transform target) => this.target = target;
   
   protected override Vector3 GetTargetPosition()
   {
      if (target == null) return Vector3.zero;
      return target.transform.position + offset;
   }

   private void Update()
   {
      HandleFollowTarget();
      HandleIconSize();
   }

   [SerializeField] private float minDist = 3f;   // bắt đầu to ra từ đây
   [SerializeField] private float maxDist = 10f;  // đạt maxSize từ đây trở lên

   private void HandleIconSize()
   {
      if (player == null || target == null) return;
      
      float distance = Vector3.Distance(target.transform.position, player.transform.position);
      // Debug.Log($"Taget Distance {distance}");
      // t = 0 khi distance <= minDist, t = 1 khi distance >= maxDist
      float t = Mathf.InverseLerp(minDist, maxDist, distance);

      // Lerp từ maxSize → minSize (thay vì min → max)
      float targetScale = Mathf.Lerp(maxSize, minSize, t);
      container.localScale = Vector3.one * targetScale;
   }
}
