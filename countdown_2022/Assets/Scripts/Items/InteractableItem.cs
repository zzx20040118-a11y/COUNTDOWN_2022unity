using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可交互捣乱物品：分为直接交互、需标记交互两类
/// 交互成功后计入全局交互记录，用于结局演出
/// </summary>
public class InteractableItem : InteractableItemBase
{
    public override void TryInteract(CatController cat)
    {
        bool canInteract = false;

        // 校验1：直接交互类型 - 空嘴状态可触发
        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.Direct))
        {
            if (!cat.IsCarryingMarkItem())
            {
                canInteract = true;
            }
        }

        // 校验2：需标记交互类型 - 携带对应标记时可触发
        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.RequireMark))
        {
            if (cat.IsCarryingMarkItem() && cat.GetCarriedMarkItemData() == itemData.requiredMarkItem)
            {
                canInteract = true;
            }
        }

        // 权限不满足，直接结束交互锁，不执行任何效果
        if (!canInteract)
        {
            cat.EndInteract();
            return;
        }

        // 执行交互逻辑
        ExecuteInteraction(cat);
    }

    private void ExecuteInteraction(CatController cat)
    {
        // 计入全局交互记录
        GlobalDataManager.Instance.AddInteractedItem(itemData.itemId);

        // TODO：播放物品自身交互动画、猫咪交互动画
        // 临时占位延时，后续替换为动画事件回调
        cat.Invoke(nameof(CatController.EndInteract), 0.5f);
    }
}