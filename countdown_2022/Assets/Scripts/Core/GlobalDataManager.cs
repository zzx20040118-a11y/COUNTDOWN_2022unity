using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalDataManager : Singleton<GlobalDataManager>
{
    private readonly List<int> _interactedItemIds = new List<int>();
    private int _currentScore = 0;

    /// <summary>
    /// 添加已交互物品（自动去重），成功添加时积分+1
    /// </summary>
    public void AddInteractedItem(int itemId)
    {
        if (!_interactedItemIds.Contains(itemId))
        {
            _interactedItemIds.Add(itemId);
            _currentScore++;
        }
    }

    /// <summary>
    /// 获取当前积分
    /// </summary>
    public int GetCurrentScore()
    {
        return _currentScore;
    }

    /// <summary>
    /// 获取所有已交互物品编号（返回副本，防止外部篡改数据）
    /// </summary>
    public List<int> GetInteractedItemIds()
    {
        return new List<int>(_interactedItemIds);
    }

    /// <summary>
    /// 清空全部游戏数据 - 返回主界面时自动调用
    /// </summary>
    public void ClearAllGameData()
    {
        _interactedItemIds.Clear();
        _currentScore = 0;
    }
}
