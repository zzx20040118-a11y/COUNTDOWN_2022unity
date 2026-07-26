using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableItem : InteractableItemBase
{
    private ItemAnimationController _itemAnimController;
    private float _itemAnimSpeedScale = 1f;
    private bool _hasInteracted = false;

    private void Awake()
    {
        _itemAnimController = GetComponent<ItemAnimationController>();
        // 读取物品动画全局缩放配置
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
        {
            _itemAnimSpeedScale = GameManager.Instance.gameConfig.itemAnimationGlobalScale;
        }
    }

    public override void TryInteract(CatController cat)
    {
        // 已交互过的物品直接结束，不可重复触发
        if (_hasInteracted)
        {
            cat.EndInteract();
            return;
        }

        AnimationClip targetItemAnim = null;
        bool isMarkedInteract = false;

        // 优先级：标记交互 > 直接交互
        if (System.Array.Exists(itemData.interactTypes, t => t == InteractableType.RequireMark))
        {
            if (cat.IsCarryingMarkItem() && cat.GetCarriedMarkItemData() == itemData.requiredMarkItem)
            {
                isMarkedInteract = true;
                targetItemAnim = itemData.markedItemAnim;
            }
        }

        // 不满足标记交互则判断直接交互
        if (!isMarkedInteract && System.Array.Exists(itemData.interactTypes, t => t == InteractableType.Direct))
        {
            targetItemAnim = itemData.directItemAnim;
        }

        // 无有效动画，直接结束交互
        if (targetItemAnim == null)
        {
            cat.EndInteract();
            return;
        }

        // 标记为已交互，不可重复触发
        _hasInteracted = true;

        // 播放物品自身动画（单次播放，停在最后一帧） + 记录全局数据
        _itemAnimController?.PlayInteract(targetItemAnim);
        GlobalDataManager.Instance.AddInteractedItem(itemData.itemId);

        if (isMarkedInteract)
        {
            // 标记交互：猫咪播放吐出动画并消耗标记，结束后自动解锁
            cat.ConsumeCarriedMark(cat.EndInteract);
        }
        else
        {
            // 直接交互：猫咪无动作，按物品动画时长维持交互锁
            StartCoroutine(DirectInteractWaitCoroutine(targetItemAnim, cat));
        }
    }

    /// <summary>
    /// 直接交互等待协程：按物品动画时长锁定交互状态
    /// </summary>
    private IEnumerator DirectInteractWaitCoroutine(AnimationClip itemAnim, CatController cat)
    {
        float duration = itemAnim.length / _itemAnimSpeedScale;
        yield return new WaitForSeconds(duration);
        cat.EndInteract();
    }
}
