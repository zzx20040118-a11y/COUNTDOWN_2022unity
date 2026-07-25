using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有可交互物品的抽象基类
/// 定义统一交互入口，由猫咪到达邻近点后调用
/// </summary>
public abstract class InteractableItemBase : MonoBehaviour
{
    [Header("物品配置")]
    public ItemDataSO itemData;

    /// <summary>
    /// 统一交互入口，猫咪到达后自动调用
    /// 子类重写实现具体交互逻辑
    /// </summary>
    public abstract void TryInteract(CatController cat);
}