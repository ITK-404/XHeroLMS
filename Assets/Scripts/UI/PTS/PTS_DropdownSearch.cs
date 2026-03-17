using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PTS_DropdownSearch : TMP_Dropdown
{
    [SerializeField] private Transform arrow;

    protected override void Awake()
    {
        base.Awake();
        arrow = transform.Find("Arrow");
    }

    protected override GameObject CreateDropdownList(GameObject template)
    {
        arrow.DOKill();
        arrow.DORotate(new Vector3(0, 0, 180), 0.2f);
        return base.CreateDropdownList(template);
    }

    protected override void DestroyDropdownList(GameObject dropdownList)
    {
        base.DestroyDropdownList(dropdownList);
        arrow.DOKill();
        arrow.DORotate(new Vector3(0, 0, 0), 0.2f);
    }
}
