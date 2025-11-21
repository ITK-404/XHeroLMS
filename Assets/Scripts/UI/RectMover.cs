using UnityEngine;

public class RectMover : MonoBehaviour
{
    public RectTransform target;      // object di chuyển
    public RectTransform container;   // vùng chứa
    public float speed = 200f;

    private Vector2 direction;        // hướng bay

    void Start()
    {
        container = target.parent.GetComponent<RectTransform>();
        RandomizeDirection();
    }

    void Update()
    {
        if (target == null || container == null) return;

        // 1. Di chuyển
        target.anchoredPosition += direction * speed * Time.deltaTime;

        // 2. Lấy bound container
        Rect contRect = container.rect;
        Rect tRect = target.rect;

        Vector2 pos = target.anchoredPosition;

        bool hit = false;

        // Kiểm tra đụng bound X
        if (pos.x - tRect.width * 0.5f < contRect.xMin ||
            pos.x + tRect.width * 0.5f > contRect.xMax)
        {
            hit = true;
            pos.x = Mathf.Clamp(pos.x, 
                contRect.xMin + tRect.width * 0.5f,
                contRect.xMax + -tRect.width * 0.5f);
        }

        // Kiểm tra đụng bound Y
        if (pos.y - tRect.height * 0.5f < contRect.yMin ||
            pos.y + tRect.height * 0.5f > contRect.yMax)
        {
            hit = true;
            pos.y = Mathf.Clamp(pos.y,
                contRect.yMin + tRect.height * 0.5f,
                contRect.yMax - tRect.height * 0.5f);
        }

        // áp lại vị trí đã clamp
        target.anchoredPosition = pos;

        // 3. Nếu đụng cạnh => random hướng mới
        if (hit)
        {
            RandomizeDirection();
        }
    }

    private void RandomizeDirection()
    {
        direction = Random.insideUnitCircle.normalized;
    }
}