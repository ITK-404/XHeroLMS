using System;
using UnityEngine;

/// <summary>
/// Đơn giản: kéo sẵn 4 GameObject (đã tự set vị trí/anchor/xoay đúng cho
/// từng hướng trong Editor) vào 4 slot bên dưới. Script chỉ bật object
/// đúng hướng và tắt 3 object còn lại.
/// </summary>
public class DirectionalSwitcher : MonoBehaviour
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("Kéo 4 object đã setup sẵn vào đây")] [SerializeField]
    private GameObject upObject;

    [SerializeField] private GameObject downObject;
    [SerializeField] private GameObject leftObject;
    [SerializeField] private GameObject rightObject;

    [Header("Hướng hiện tại")] [SerializeField]
    private Direction direction = Direction.Up;

    [SerializeField] private LabelAutoPlacer placer;
    public Direction CurrentDirection => direction;


    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        if (placer == null) return;
        SetDirection(GetDir());
    }

    private Direction GetDir()
    {
        switch (placer.curDir)
        {
            case LabelAutoPlacer.Direction.Top:
                return Direction.Up;
            case LabelAutoPlacer.Direction.Bottom:
                return Direction.Down;
            case LabelAutoPlacer.Direction.Left:
                return Direction.Left;
            case LabelAutoPlacer.Direction.Right:
                return Direction.Right;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>Đổi hướng lúc runtime.</summary>
    public void SetDirection(Direction newDirection)
    {
        if (newDirection == direction) return;
        direction = newDirection;
        Apply();
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (upObject != null) upObject.SetActive(direction == Direction.Up);
        if (downObject != null) downObject.SetActive(direction == Direction.Down);
        if (leftObject != null) leftObject.SetActive(direction == Direction.Left);
        if (rightObject != null) rightObject.SetActive(direction == Direction.Right);
    }
}