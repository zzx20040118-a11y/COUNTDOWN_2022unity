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
    /// <summary>
    /// 交互锁标记，输入层依靠此属性屏蔽指令
    /// </summary>
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
        // 单例初始化
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
        // 交互锁定、跳跃中不执行普通移动更新
        if (_currentState == CatState.Interacting || _currentState == CatState.Jumping)
            return;

        if (_currentState == CatState.Moving)
        {
            ProcessNormalMovement();
        }
    }

    #region 对外公共接口
    /// <summary>
    /// 普通移动到指定世界坐标
    /// 移动中可被新指令覆盖
    /// </summary>
    public void MoveToPosition(Vector2 targetPos)
    {
        if (_currentState == CatState.Interacting) return;

        // 清除待交互物品，纯移动
        _pendingInteractItem = null;

        // 校验并修正目标点为合法可站立坐标
        Vector2 validTarget = GetValidStandablePoint(targetPos, GetCurrentTerrainLayer());
        _currentMoveTarget = validTarget;
        _currentState = CatState.Moving;
    }

    /// <summary>
    /// 移动到物品邻近可站立点，到达后自动触发交互
    /// </summary>
    public void MoveAndInteract(InteractableItemBase targetItem)
    {
        if (_currentState == CatState.Interacting) return;
        if (targetItem == null) return;

        _pendingInteractItem = targetItem;

        // 计算物品附近最近的可站立坐标
        Vector2 itemPos = targetItem.transform.position;
        Vector2 nearestStandPoint = FindNearestStandablePoint(itemPos, GetCurrentTerrainLayer());
        nearestStandPoint += (nearestStandPoint - itemPos).normalized * interactOffset;

        _currentMoveTarget = nearestStandPoint;
        _currentState = CatState.Moving;
    }

    /// <summary>
    /// 吐出标记物品，恢复到原始坐标
    /// 播放吐出动画期间锁定输入
    /// </summary>
    public void SpitMarkItem()
    {
        if (_currentState == CatState.Interacting) return;
        if (_carriedMarkItem == null) return;

        StartCoroutine(SpitMarkCoroutine());
    }

    /// <summary>
    /// 供外部调用：结束交互锁定状态
    /// 物品交互动画播放完毕后调用此方法解锁
    /// </summary>
    public void EndInteract()
    {
        IsInInteractLock = false;
        _currentState = CatState.Idle;
        _pendingInteractItem = null;
    }
    #endregion

    #region 普通移动逻辑
    private void ProcessNormalMovement()
    {
        Vector2 currentPos = transform.position;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(currentPos, _currentMoveTarget, step);

        // 到达目标点
        if (Vector2.Distance(transform.position, _currentMoveTarget) < 0.01f)
        {
            OnMoveTargetReached();
        }
    }

    private void OnMoveTargetReached()
    {
        // 如果有待交互物品，触发交互
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

    #region 地形判定核心方法
    /// <summary>
    /// 获取指定坐标的地形类型
    /// 上层优先级高于阻挡
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
        if (hasBlocked) return TerrainType.Blocked;
        // 默认下层
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
    /// 从目标点向猫咪当前位置射线检测，遇阻挡则停在边缘
    /// </summary>
    private Vector2 FindNearestStandablePoint(Vector2 targetPoint, TerrainType targetLayer)
    {
        Vector2 startPos = transform.position;
        Vector2 direction = (startPos - targetPoint).normalized;

        // 从目标点向猫咪方向发射射线，寻找第一个可站立点
        float maxDistance = Vector2.Distance(startPos, targetPoint);
        RaycastHit2D[] hits = Physics2D.RaycastAll(targetPoint, direction, maxDistance);

        foreach (var hit in hits)
        {
            TerrainArea area = hit.collider.GetComponent<TerrainArea>();
            if (area == null) continue;

            // 上层区域始终可站立
            if (area.areaType == TerrainType.UpperLayer && targetLayer == TerrainType.UpperLayer)
                continue;

            // 遇到纯阻挡，返回阻挡前的点
            if (area.areaType == TerrainType.Blocked)
            {
                return hit.point - direction * 0.05f;
            }
        }

        // 全程无障碍，目标点本身合法
        return targetPoint;
    }

    /// <summary>
    /// 校验并返回合法可站立坐标
    /// </summary>
    private Vector2 GetValidStandablePoint(Vector2 targetPos, TerrainType targetLayer)
    {
        TerrainType targetType = GetTerrainTypeAtPoint(targetPos);

        // 目标点合法
        if (targetType == targetLayer || targetType == TerrainType.UpperLayer)
            return targetPos;

        // 纯阻挡，找最近可站立点
        return FindNearestStandablePoint(targetPos, targetLayer);
    }
    #endregion

    #region 跨层跳跃逻辑
    /// <summary>
    /// 执行跨层跳跃：从当前位置跳到目标层指定点
    /// 自动计算起跳点与落点
    /// </summary>
    public void JumpToLayerPoint(Vector2 targetPoint, TerrainType targetLayer)
    {
        if (_currentState == CatState.Interacting || _currentState == CatState.Jumping)
            return;

        StartCoroutine(JumpCoroutine(targetPoint));
    }

    private IEnumerator JumpCoroutine(Vector2 targetPos)
    {
        _currentState = CatState.Jumping;
        Vector2 startPos = transform.position;

        // 播放跳跃音效
        if (_audioSource != null && jumpAudio != null)
        {
            _audioSource.PlayOneShot(jumpAudio);
        }

        float timer = 0f;
        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / jumpDuration;

            // 线性插值水平位置
            Vector2 horizontalPos = Vector2.Lerp(startPos, targetPos, progress);
            // 抛物线高度计算（上凸弧形）
            float heightOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            transform.position = new Vector2(horizontalPos.x, horizontalPos.y + heightOffset);
            yield return null;
        }

        // 跳跃结束，修正到精确落点
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

        // 吞咽动画期间锁定
        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        // TODO：对接猫咪动画控制器，播放吞咽动画，动画结束调用EndInteract()
        Invoke(nameof(EndInteract), 0.3f); // 临时占位，替换为动画事件回调
    }

    private IEnumerator SpitMarkCoroutine()
    {
        IsInInteractLock = true;
        _currentState = CatState.Interacting;

        // TODO：对接猫咪动画控制器，播放吐出动画
        yield return new WaitForSeconds(0.3f); // 临时占位，替换为动画时长

        // 恢复物品到原始坐标
        _carriedMarkItem.gameObject.SetActive(true);
        _carriedMarkItem.transform.position = _markItemOriginalPos;

        _carriedMarkItem = null;
        EndInteract();
    }
    #endregion
}