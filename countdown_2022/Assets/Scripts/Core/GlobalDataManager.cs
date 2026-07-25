using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalDataManager : Singleton<GlobalDataManager>
{
    private readonly List<int> _interactedItemIds = new List<int>();

    // 添加已交互物品（自动去重）
    public void AddInteractedItem(int itemId)
    {
        if (!_interactedItemIds.Contains(itemId))
        {
            _interactedItemIds.Add(itemId);
        }
    }

    // 获取所有已交互物品编号（返回副本，防止外部篡改数据）
    public List<int> GetInteractedItemIds()
    {
        return new List<int>(_interactedItemIds);
    }

    // 清空全部游戏数据 - 返回主界面时自动调用
    public void ClearAllGameData()
    {
        _interactedItemIds.Clear();
    }
}