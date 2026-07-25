using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemCategory
{
    MarkItem,        // 标记物品：可被吞下/吐出
    InteractableItem // 可交互捣乱物品
}

public enum InteractableType
{
    Direct,          // 仅空嘴状态可交互
    RequireMark      // 仅携带标记状态可交互
}

public enum GameState
{
    MainMenu,
    Playing,
    Ending
}
