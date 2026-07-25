using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形区域类型
/// 优先级：上层区域 > 阻挡区域
/// </summary>
public enum TerrainType
{
    LowerLayer,  // 下层区域（默认地面，可正常行走站立）
    UpperLayer,  // 上层高台区域，仅可通过跳跃抵达
    Blocked      // 纯阻挡区域，不可站立、不可普通行走穿越
}

/// <summary>
/// 地形区域标记组件
/// 挂载到对应区域的2D碰撞体上，碰撞体需设为Trigger
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TerrainArea : MonoBehaviour
{
    public TerrainType areaType;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }
}