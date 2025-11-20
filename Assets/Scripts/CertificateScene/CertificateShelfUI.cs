using UnityEngine;

public class CertificateShelfUI : MonoBehaviour
{
    [Header("Các slot bằng trên kệ (3 cái)")]
    public CertificateItemUI[] slots;   // size = 3

    /// <summary>
    /// Xóa / ẩn toàn bộ slot trước khi gán dữ liệu mới.
    /// </summary>
    public void ClearSlots()
    {
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s == null) continue;
            s.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gán dữ liệu cho 1 slot cụ thể trên kệ.
    /// </summary>
    public void SetupSlot(
        int index,
        string fullName,
        string certName,
        string createdAt,
        string certImgUrl)
    {
        if (slots == null) return;
        if (index < 0 || index >= slots.Length) return;

        var slot = slots[index];
        if (slot == null) return;

        slot.gameObject.SetActive(true);
        slot.Setup(fullName, certName, createdAt, certImgUrl);
    }
}
