using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickInputManager : MonoBehaviour
{
    private Camera _mainCamera;

    private void Awake()
    {
        // 固定相机，启动时一次性缓存主相机实例，无需手动拖拽
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        // 非游玩状态下，直接屏蔽所有鼠标输入
        if (GameManager.Instance.CurrentState != GameState.Playing)
            return;

        CatController cat = CatController.Instance;
        if (cat == null) return;

        // 交互锁定状态：无视所有鼠标指令（左键、右键全部屏蔽）
        // 必须等交互动画完全结束后，才能响应新操作
        if (cat.IsInInteractLock)
            return;

        // 左键：移动 / 移动并交互
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick(cat);
        }

        // 右键：吐出标记物品（恢复到原始坐标，无位置参数）
        if (Input.GetMouseButtonDown(1))
        {
            cat.SpitMarkItem();
        }
    }

    /// <summary>
    /// 左键点击逻辑：识别目标类型并下发对应指令
    /// 优先级：可交互物品 > 空白地面移动
    /// 地形判定、跨层跳跃、邻近点计算全部下沉至 CatController 内部处理
    /// </summary>
    private void HandleLeftClick(CatController catController)
    {
        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        // 命中可交互物品：下发「移动到邻近点 + 到达后触发交互」指令
        if (hit.collider != null)
        {
            InteractableItemBase item = hit.collider.GetComponent<InteractableItemBase>();
            if (item != null)
            {
                catController.MoveAndInteract(item);
                return;
            }
        }

        // 空白区域：下发普通移动指令
        // 阻挡判定、上下层跨层逻辑均由 CatController 内部处理
        catController.MoveToPosition(mouseWorldPos);
    }
}
