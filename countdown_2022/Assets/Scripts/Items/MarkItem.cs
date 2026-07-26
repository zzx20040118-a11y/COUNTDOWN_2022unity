using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkItem : InteractableItemBase
{
    private Vector2 _originalPosition;

    private void Awake()
    {
        _originalPosition = transform.position;
    }

    public override void TryInteract(CatController cat)
    {
        // 已携带标记时直接结束交互，不重复拾取
        if (cat.IsCarryingMarkItem())
        {
            cat.EndInteract();
            return;
        }
        cat.CarryMarkItem(this, _originalPosition);
    }

    /// <summary>
    /// 复位到原始位置并显示（备用接口，外部可调用）
    /// </summary>
    public void RestoreToOriginal()
    {
        transform.position = _originalPosition;
        gameObject.SetActive(true);
    }
}
