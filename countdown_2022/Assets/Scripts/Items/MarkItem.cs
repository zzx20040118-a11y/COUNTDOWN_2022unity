using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 标记物品：可被猫咪吞下携带，右键可吐出恢复到原始位置
/// </summary>
public class MarkItem : InteractableItemBase
{
    private Vector2 _originalPosition;

    private void Awake()
    {
        // 记录物品初始位置，吐出时精准复位
        _originalPosition = transform.position;
    }

    public override void TryInteract(CatController cat)
    {
        // 猫咪已经携带标记时，无法重复吞下
        if (cat.IsCarryingMarkItem())
        {
            cat.EndInteract();
            return;
        }

        // 执行吞下逻辑
        cat.CarryMarkItem(this, _originalPosition);
    }

    /// <summary>
    /// 外部调用：重置物品到原始位置并显示
    /// </summary>
    public void RestoreToOriginal()
    {
        transform.position = _originalPosition;
        gameObject.SetActive(true);
    }
}