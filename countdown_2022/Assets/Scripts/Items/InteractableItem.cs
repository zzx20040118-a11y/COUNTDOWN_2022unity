using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableItem : InteractableItemBase
{
    private ItemAnimationController _animController;

    private void Awake()
    {
        _animController = GetComponent<ItemAnimationController>();
    }

    public override void TryInteract(CatController cat)
    {
        bool canInteract = false;

        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.Direct))
        {
            if (!cat.IsCarryingMarkItem())
            {
                canInteract = true;
            }
        }

        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.RequireMark))
        {
            if (cat.IsCarryingMarkItem() && cat.GetCarriedMarkItemData() == itemData.requiredMarkItem)
            {
                canInteract = true;
            }
        }

        if (!canInteract)
        {
            cat.EndInteract();
            return;
        }

        ExecuteInteraction();
    }

    private void ExecuteInteraction()
    {
        GlobalDataManager.Instance.AddInteractedItem(itemData.itemId);
        _animController?.PlayInteract(itemData.itemAnim);
    }
}