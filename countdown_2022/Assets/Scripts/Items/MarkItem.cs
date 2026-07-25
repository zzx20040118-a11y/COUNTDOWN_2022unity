using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkItem : InteractableItemBase
{
    private Vector2 _originalPosition;
    private ItemAnimationController _animController;

    private void Awake()
    {
        _originalPosition = transform.position;
        _animController = GetComponent<ItemAnimationController>();
    }

    public override void TryInteract(CatController cat)
    {
        if (cat.IsCarryingMarkItem())
        {
            cat.EndInteract();
            return;
        }
        cat.CarryMarkItem(this, _originalPosition);
    }

    public void RestoreToOriginal()
    {
        transform.position = _originalPosition;
        gameObject.SetActive(true);
        _animController?.PlayAppear(itemData.itemAnim);
    }
}