using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PTS_DropdownSearch : Dropdown
{
    [SerializeField] private Transform arrow;
    [SerializeField] private float openRotate = 0;
    [SerializeField] private float closeRotate = 0;
    protected override GameObject CreateDropdownList(GameObject template)
    {
        arrow.DOKill();
        arrow.DORotate(new Vector3(0, 0, openRotate), 0.2f);
        return base.CreateDropdownList(template);
    }

    protected override void DestroyDropdownList(GameObject dropdownList)
    {
        base.DestroyDropdownList(dropdownList);
        arrow.DOKill();
        arrow.DORotate(new Vector3(0, 0, closeRotate), 0.2f);
    }
}
