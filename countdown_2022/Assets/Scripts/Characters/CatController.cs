using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatController : MonoBehaviour
{
    public static CatController Instance { get; private set; }

    #region 基础参数
    [Header("移动参数")]
    [Tooltip("普通行走速度，单位：单位/秒")]
    public float moveSpeed = 3f;

    [Header("跳跃参数")]
    [Tooltip("跳跃抛物线最高点高度")]
    public float jumpHeight = 1.5f;
    [Tooltip("跳跃全程耗时，单位：秒")]
    public float jumpDuration = 0.6f;
    public AudioClip jumpAudio;

    [Header("交互邻近偏移")]
    [Tooltip("计算物品邻近点时，向猫咪方向的偏移距离，避免贴脸")]
    public float interactOffset = 0.3f;
    #endregion

    #region 状态与目标
    public bool IsInInteractLock { get; private set; }

    private enum CatState
    {
        Idle,       // 空闲
        Moving,     // 普通移动中
        Jumping,    // 跳跃中
        Interacting // 交互锁定中
    }

    private CatState _currentState;
    private Vector2 _currentMoveTarget;
    private InteractableItemBase _pendingInteractItem;
    private Coroutine _currentMoveCoroutine; // 用于中断当前移动任务，实现指令覆盖
    #endregion

    #region 标记物品携带数据
    private InteractableItemBase _carriedMarkItem;
    private Vector2 _markItemOriginalPos;
    #endregion

    #region 组件引用
    private AudioSource _audioSource;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _audioSource = GetComponent<AudioSource>();
        _currentState = CatState.Idle;
    }

    private void Update()
    {
        if (_currentState == CatState.Interacting || _currentState == CatState.Jumping)
            return;

        if (_currentState == CatState.Moving)
        {
            ProcessNormalMovement();
        }
    }

    #region 对外公共接口
    /// <summary>
    /// 移动到指定世界坐标，自动判断是否跨层
    /// 移动中可被新指令覆盖
    /// </summary>
    public void MoveToPosition(Vector2 targetPos)
    {
        if (_currentState == CatState.Interacting) return;

        // 中断旧的移动任务，实现新指令覆盖
        if (_currentMoveCoroutine != null)
        {
            StopCoroutine(_currentMoveCoroutine);
            _currentMoveCoroutine = null;
        }

        _pendingInteractItem = null;

        TerrainType currentLayer = GetCurrentTerrainLayer();
        TerrainType targetLayer = GetTerrainTypeAtPoint(targetPos);
        Vector2 validTarget = GetValidStandablePoint(targetPos, targetLayer);

        // 同层：普通直线移动
        if (currentLayer == targetLayer)
        {
            _currentMoveTarget = validTarget;
            _currentState = CatState.Moving;
            return;
        }

        // 跨层：启动分步协程
        _currentMoveCoroutine = StartCoroutine(CrossLayerMoveCoroutine(validTarget, targetLayer));
    }

    /// <summary>
    /// 移动到物品邻近可站立点，到达后自动触发交互
    /// 自动适配跨层场景
    /// </summary>
    public void MoveAndInteract(InteractableItemBase targetItem)
    {
        if (_currentState == CatState.Interacting) return;
        if (targetItem == null) return;

        _pendingInteractItem = targetItem;

        Vector2 itemPos = targetItem.transform.position;
        TerrainType itemLayer = GetTerrainTypeAtPoint(itemPos);
        Vector2 nearestStandPoint = FindNearestStandablePoint(itemPos, itemLayer);
        nearestStandPoint += (nearestStandPoint - itemPos).normalized * interactOffset;

        // 调用统一移动入口，自动处理跨层
        MoveToPosition(nearestStandPoint);
        // 重新赋值交互目标（MoveToPosition会清空待交互项）
        _pendingInteractItem = targetItem;
    }

    /// <summary>
    /// 吐出标记物品，恢复到原始坐标
    /// </summary>
    public void SpitMarkItem()
    {
        if (_currentState == CatState.Interacting) return;
        if (_carriedMarkItem == null) return;

        StartCoroutine(SpitMarkCoroutine());
    }

    /// <summary>
    /// 结束交互锁定，动画结束后调用
    /// </summary>
    public void EndInteract()
    {
        IsInInteractLock = false;
        _currentState = CatState.Idle;
        _pendingInteractItem = null;
    }

    /// <summary>
    /// 当前是否携带标记物品
    /// </summary>
    public bool IsCarryingMarkItem()
    {
        return _carriedMarkItem != null;
    }

    /// <summary>
    /// 获取当前携带的标记物品数据，用于权限校验
    /// </summary>
    public ItemDataSO GetCarriedMarkItemData()
    {
        if (_carriedMarkItem == null) return null;
        return _carriedMarkItem.itemData;
    }
    #endregion

    #region 普通移动逻辑
    private void ProcessNormalMovement()
    {
        Vector2 currentPos = transform.position;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(currentPos, _currentMoveTarget, step);

        if (Vector2.Distance(transform.position, _currentMoveTarget) < 0.01f)
        {
            OnMoveTargetReached();
        }
    }

    private void OnMoveTargetReached()
    {
        if (_pendingInteractItem != null)
        {
            TriggerItemInteraction();
            return;
        }
        _currentState = CatState.Idle;
    }

    private void TriggerItemInteraction()
    {
        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        _pendingInteractItem.TryInteract(this);
    }
    #endregion

    #region 跨层自动路径拆解（本次新增核心）
    /// <summary>
    /// 跨层移动协程：走到起跳点 → 跳跃 → 落地走到终点
    /// </summary>
    private IEnumerator CrossLayerMoveCoroutine(Vector2 finalTarget, TerrainType targetLayer)
    {
        _currentState = CatState.Moving;
        TerrainType currentLayer = GetCurrentTerrainLayer();

        // 第一步：计算当前层的起跳点，移动过去
        Vector2 jumpStartPoint = FindNearestStandablePoint(finalTarget, currentLayer);
        _currentMoveTarget = jumpStartPoint;
        while (Vector2.Distance(transform.position, jumpStartPoint) > 0.01f)
        {
            yield return null; // 等待Update执行普通移动
        }

        // 第二步：计算目标层落点，执行跳跃
        Vector2 jumpEndPoint = FindNearestStandablePoint(finalTarget, targetLayer);
        yield return JumpCoroutine(jumpEndPoint);

        // 第三步：落地后移动到最终目标点
        _currentMoveTarget = finalTarget;
        _currentState = CatState.Moving;
        while (Vector2.Distance(transform.position, finalTarget) > 0.01f)
        {
            yield return null;
        }

        // 到达终点，触发到达回调（自动处理待交互物品）
        OnMoveTargetReached();
        _currentMoveCoroutine = null;
    }
    #endregion

    #region 地形判定核心方法
    /// <summary>
    /// 获取指定坐标的地形类型
    /// 优先级：上层区域 > 阻挡区域
    /// 未被任何地形组件覆盖的位置，默认判定为下层区域
    /// </summary>
    public TerrainType GetTerrainTypeAtPoint(Vector2 point)
    {
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(point);
        bool hasUpper = false;
        bool hasBlocked = false;

        foreach (var col in hitColliders)
        {
            TerrainArea area = col.GetComponent<TerrainArea>();
            if (area == null) continue;

            if (area.areaType == TerrainType.UpperLayer)
                hasUpper = true;
            else if (area.areaType == TerrainType.Blocked)
                hasBlocked = true;
        }

        // 上层优先级最高
        if (hasUpper) return TerrainType.UpperLayer;
        // 其次阻挡
        if (hasBlocked) return TerrainType.Blocked;
        // 无任何标记 = 默认下层
        return TerrainType.LowerLayer;
    }

    /// <summary>
    /// 获取猫咪当前所在地形层级
    /// </summary>
    public TerrainType GetCurrentTerrainLayer()
    {
        return GetTerrainTypeAtPoint(transform.position);
    }

    /// <summary>
    /// 查找距离目标点最近的、指定层的可站立坐标
    /// 从目标点向猫咪当前位置射线检测，遇阻挡停在边缘
    /// </summary>
    private Vector2 FindNearestStandablePoint(Vector2 targetPoint, TerrainType targetLayer)
    {
        Vector2 startPos = transform.position;
        Vector2 direction = (startPos - targetPoint).normalized;
        float maxDistance = Vector2.Distance(startPos, targetPoint);

        RaycastHit2D[] hits = Physics2D.RaycastAll(targetPoint, direction, maxDistance);

        foreach (var hit in hits)
        {
            TerrainArea area = hit.collider.GetComponent<TerrainArea>();
            if (area == null) continue;

            // 目标层为上层时，上层区域可通行
            if (area.areaType == TerrainType.UpperLayer && targetLayer == TerrainType.UpperLayer)
                continue;

            // 遇到纯阻挡，返回阻挡前的合法点
            if (area.areaType == TerrainType.Blocked)
            {
                return hit.point - direction * 0.05f;
            }
        }

        return targetPoint;
    }

    /// <summary>
    /// 校验并返回合法可站立坐标
    /// </summary>
    private Vector2 GetValidStandablePoint(Vector2 targetPos, TerrainType targetLayer)
    {
        TerrainType targetType = GetTerrainTypeAtPoint(targetPos);
        if (targetType == targetLayer || targetType == TerrainType.UpperLayer)
            return targetPos;

        return FindNearestStandablePoint(targetPos, targetLayer);
    }
    #endregion

    #region 跳跃核心逻辑
    /// <summary>
    /// 执行弧形跳跃，可被协程等待
    /// </summary>
    private IEnumerator JumpCoroutine(Vector2 targetPos)
    {
        _currentState = CatState.Jumping;
        Vector2 startPos = transform.position;

        if (_audioSource != null && jumpAudio != null)
        {
            _audioSource.PlayOneShot(jumpAudio);
        }

        float timer = 0f;
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;

            Vector2 horizontalPos = Vector2.Lerp(startPos, targetPos, progress);
            float heightOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            transform.position = new Vector2(horizontalPos.x, horizontalPos.y + heightOffset);
            yield return null;
        }

        transform.position = targetPos;
        _currentState = CatState.Idle;
    }
    #endregion

    #region 标记物品吞吐逻辑
    /// <summary>
    /// 吞下标记物品，由MarkItem交互时调用
    /// </summary>
    public void CarryMarkItem(InteractableItemBase markItem, Vector2 originalPos)
    {
        _carriedMarkItem = markItem;
        _markItemOriginalPos = originalPos;
        markItem.gameObject.SetActive(false);

        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        // 临时占位，后续替换为动画事件回调
        Invoke(nameof(EndInteract), 0.3f);
    }

    private IEnumerator SpitMarkCoroutine()
    {
        IsInInteractLock = true;
        _currentState = CatState.Interacting;

        // 临时占位，后续替换为动画播放
        yield return new WaitForSeconds(0.3f);

        _carriedMarkItem.gameObject.SetActive(true);
        _carriedMarkItem.transform.position = _markItemOriginalPos;

        _carriedMarkItem = null;
        EndInteract();
    }
    #endregion
}