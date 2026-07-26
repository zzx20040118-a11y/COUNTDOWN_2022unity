using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableItem : InteractableItemBase
{
    private ItemAnimationController _itemAnimController;

    private void Awake()
    {
        _itemAnimController = GetComponent<ItemAnimationController>();
    }

    public override void TryInteract(CatController cat)
    {
        bool isMarkedInteract = false;
        AnimationClip targetCatAnim = null;
        AnimationClip targetItemAnim = null;

        // 优先级：标记交互 > 直接交互
        // 先判断是否满足标记交互条件
        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.RequireMark))
        {
            if (cat.IsCarryingMarkItem() && cat.GetCarriedMarkItemData() == itemData.requiredMarkItem)
            {
                isMarkedInteract = true;
                targetCatAnim = itemData.markedCatInteractAnim;
                targetItemAnim = itemData.markedItemAnim;
            }
        }

        // 不满足标记交互，再判断直接交互
        if (!isMarkedInteract && System.Array.Exists(itemData.interactTypes, t => t == InteractableType.Direct))
        {
            if (!cat.IsCarryingMarkItem())
            {
                targetCatAnim = itemData.directCatInteractAnim;
                targetItemAnim = itemData.directItemAnim;
            }
        }

        // 两种都不满足，直接结束交互
        if (targetCatAnim == null)
        {
            cat.EndInteract();
            return;
        }

        // 执行交互：播猫咪动画 + 物品动画 + 记录全局数据
        CatAnimationController catAnim = cat.GetComponent<CatAnimationController>();
        catAnim?.PlayItemInteract(targetCatAnim, cat.EndInteract);
        
        _itemAnimController?.PlayInteract(targetItemAnim);
        GlobalDataManager.Instance.AddInteractedItem(itemData.itemId);
    }
}
