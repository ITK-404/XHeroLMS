using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExamMatchingElement : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum ElementSide
    {
        A,
        B
    }

    public ElementSide side;

    [Header("Matching Points")]
    [SerializeField] private Transform topPoint;    // dùng cho Side B
    [SerializeField] private Transform lowerPoint;  // dùng cho Side A
    [SerializeField] private Image matchingImg;
    public ExamMatchingElement ConnectedElement { get; private set; }

    private LineRenderer currentLine;

    private void Awake()
    {
        if (side == ElementSide.A)
        {
            topPoint.gameObject.SetActive(false);
        }
        else
        {
            lowerPoint.gameObject.SetActive(false);
        }
    }

    public Transform GetMatchingPoint()
    {
        return side == ElementSide.A ? lowerPoint : topPoint;
    }

    #region Drag & Drop Events

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (EventSystem.current == null) return;

        var pointer = new PointerEventData(EventSystem.current) { position = eventData.position };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        ExamMatchingElement target = null;

        foreach (var result in results)
        {
            target = result.gameObject.GetComponentInParent<ExamMatchingElement>();
            if (target != null && target != this && target.side != side)
            {
                break;
            }
            target = null;
        }

        if (target != null)
        {
            MatchingElementHandler.Instance.TryConnect(this, target);
        }
        else
        {
            // Huy ket noi
            MatchingElementHandler.Instance.Disconnect(this);
        }
    }

    #endregion

    #region Connection Management

    // Gọi từ Handler khi kết nối thành công
    public void SetConnection(ExamMatchingElement other, LineRenderer line)
    {
        if (ConnectedElement != null && ConnectedElement != other)
        {
            MatchingElementHandler.Instance.DisconnectPair(this, ConnectedElement);
        }

        ConnectedElement = other;
        currentLine = line;

        UpdateLinePosition();
    }

    public void ClearConnection()
    {
        ConnectedElement = null;

        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }
    }

    private void UpdateLinePosition()
    {
        if (currentLine == null || ConnectedElement == null) return;

        Vector3 posA = GetMatchingPoint().position;
        Vector3 posB = ConnectedElement.GetMatchingPoint().position;
       
        float zOffset = -0.5f; 

        posA.z += zOffset;
        posB.z += zOffset;
        
        currentLine.SetPosition(0, posA);
        currentLine.SetPosition(1, posB);
    }

    private void LateUpdate()
    {
        if (currentLine != null && ConnectedElement != null)
        {
            UpdateLinePosition();
        }
    }

    #endregion
}
