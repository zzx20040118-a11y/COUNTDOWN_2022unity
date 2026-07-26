using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatController : MonoBehaviour
{
    public static CatController Instance { get; private set; }

    #region 基础参数
    [Header("移动参数")]
    public float moveSpeed = 3f;

    [Header("跳跃参数")]
    public float jumpHeight = 1.5f;
    public float jumpDuration = 0.6f;
    public AudioClip jumpAudio;

    [Header("交互邻近偏移")]
    public float interactOffset = 0.3f;
    #endregion

    #region 状态与目标
    public bool IsInInteractLock { get; private set; }

    private enum CatState
    {
        Idle,
        Moving,
        Jumping,
        Interacting
    }

    private CatState _currentState;
    private Vector2 _currentMoveTarget;
    private InteractableItemBase _pendingInteractItem;
    private Coroutine _currentMoveCoroutine;
    #endregion

    #region 标记物品携带数据
    private InteractableItemBase _carriedMarkItem;
    private Vector2 _markItemOriginalPos;
    #endregion

    #region 组件引用
    private AudioSource _audioSource;
    private CatAnimationController _animController;
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
        _animController = GetComponent<CatAnimationController>();
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
    public void MoveToPosition(Vector2 targetPos)
    {
        if (_currentState == CatState.Interacting) return;

        if (_currentMoveCoroutine != null)
        {
            StopCoroutine(_currentMoveCoroutine);
            _currentMoveCoroutine = null;
        }

        _pendingInteractItem = null;

        TerrainType currentLayer = GetCurrentStandableLayer();
        TerrainType targetLayer = GetCurrentStandableLayer(targetPos);
        Vector2 validEndPoint = GetClampedPointByObstacle(transform.position, targetPos, currentLayer, targetLayer);

        if (currentLayer == targetLayer)
        {
            _currentMoveTarget = validEndPoint;
            _currentState = CatState.Moving;
            Vector2 moveDir = (validEndPoint - (Vector2)transform.position).normalized;
            _animController?.PlayMove(moveDir);
            return;
        }

        if ((currentLayer == TerrainType.LowerLayer && targetLayer == TerrainType.UpperLayer) ||
            (currentLayer == TerrainType.UpperLayer && targetLayer == TerrainType.LowerLayer))
        {
            _currentMoveCoroutine = StartCoroutine(CrossLayerMoveCoroutine(targetPos, targetLayer));
            return;
        }

        _currentMoveTarget = transform.position;
        _currentState = CatState.Idle;
        _animController?.PlayIdle();
    }

    public void MoveAndInteract(InteractableItemBase targetItem)
    {
        if (_currentState == CatState.Interacting) return;
        if (targetItem == null) return;

        _pendingInteractItem = targetItem;

        Vector2 itemPos = targetItem.transform.position;
        TerrainType itemLayer = GetCurrentStandableLayer(itemPos);
        Vector2 nearestStandPoint = GetClampedPointByObstacle(transform.position, itemPos, GetCurrentStandableLayer(), itemLayer);
        nearestStandPoint += (nearestStandPoint - itemPos).normalized * interactOffset;

        MoveToPosition(nearestStandPoint);
        _pendingInteractItem = targetItem;
    }

    public void SpitMarkItem()
    {
        if (_currentState == CatState.Interacting) return;
        if (_carriedMarkItem == null) return;

        StartCoroutine(SpitMarkCoroutine());
    }

    /// <summary>
    /// 消耗携带的标记：播放吐出动画，不放回物品实体，直接清空携带状态
    /// 用于标记交互场景
    /// </summary>
    public void ConsumeCarriedMark(System.Action onComplete)
    {
        if (_carriedMarkItem == null)
        {
            onComplete?.Invoke();
            return;
        }

        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        StartCoroutine(ConsumeMarkCoroutine(onComplete));
    }

    public void EndInteract()
    {
        IsInInteractLock = false;
        _currentState = CatState.Idle;
        _pendingInteractItem = null;
        _animController?.PlayIdle();
    }

    public bool IsCarryingMarkItem()
    {
        return _carriedMarkItem != null;
    }

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
        Vector2 moveDir = (_currentMoveTarget - currentPos).normalized;
        float step = moveSpeed * Time.deltaTime;
        transform.position = Vector2.MoveTowards(currentPos, _currentMoveTarget, step);

        _animController?.PlayMove(moveDir);

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
        _animController?.PlayIdle();
    }

    private void TriggerItemInteraction()
    {
        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        
        // 动画逻辑完全交由物品侧处理，结束后物品侧回调 EndInteract
        _pendingInteractItem.TryInteract(this);
    }
    #endregion

    #region 跨层跳跃逻辑
    private IEnumerator CrossLayerMoveCoroutine(Vector2 finalTarget, TerrainType targetLayer)
    {
        _currentState = CatState.Moving;
        Vector2 moveDir = (finalTarget - (Vector2)transform.position).normalized;
        _animController?.PlayMove(moveDir);

        TerrainType currentLayer = GetCurrentStandableLayer();

        Vector2 jumpStartPoint = FindLayerEdgeJumpPoint(finalTarget, currentLayer);
        _currentMoveTarget = jumpStartPoint;
        while (Vector2.Distance(transform.position, jumpStartPoint) > 0.01f)
        {
            yield return null;
        }

        Vector2 validLandingPoint = GetValidJumpLandingPoint(finalTarget, targetLayer, jumpStartPoint);
        Vector2 jumpDir = (validLandingPoint - (Vector2)transform.position).normalized;
        yield return JumpCoroutine(validLandingPoint, jumpDir);

        // 落地二次校验：阻挡优先级最高，落在阻挡里自动修正
        if (IsPointInBlocked(transform.position))
        {
            transform.position = GetNearestFreePointFromBlocked(transform.position);
        }

        OnMoveTargetReached();
        _currentMoveCoroutine = null;
    }

    private Vector2 GetValidJumpLandingPoint(Vector2 targetPos, TerrainType targetLayer, Vector2 jumpStartPos)
    {
        // 阻挡优先级最高，跳跃落点仅校验阻挡
        if (!IsPointInBlocked(targetPos))
            return targetPos;

        Vector2 direction = (targetPos - jumpStartPos).normalized;
        float distance = Vector2.Distance(jumpStartPos, targetPos);

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = true;

        RaycastHit2D[] hits = new RaycastHit2D[20];
        int hitCount = Physics2D.Raycast(jumpStartPos, direction, filter, hits, distance);

        float nearestBlockDist = distance;
        for (int i = 0; i < hitCount; i++)
        {
            TerrainArea area = hits[i].collider.GetComponent<TerrainArea>();
            if (area == null) continue;
            // 仅阻挡会影响跳跃落点
            if (area.areaType == TerrainType.Blocked)
            {
                if (hits[i].distance < nearestBlockDist)
                    nearestBlockDist = hits[i].distance;
            }
        }

        Vector2 hitPoint = jumpStartPos + direction * nearestBlockDist;
        return hitPoint - direction * 0.05f;
    }

    private Vector2 FindLayerEdgeJumpPoint(Vector2 targetPoint, TerrainType currentLayer)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = true;

        if (currentLayer == TerrainType.LowerLayer)
        {
            Collider2D[] hits = new Collider2D[10];
            int hitCount = Physics2D.OverlapPoint(targetPoint, filter, hits);
            
            Collider2D upperCollider = null;
            for (int i = 0; i < hitCount; i++)
            {
                TerrainArea area = hits[i].GetComponent<TerrainArea>();
                if (area != null && area.areaType == TerrainType.UpperLayer)
                {
                    upperCollider = hits[i];
                    break;
                }
            }

            if (upperCollider == null)
                return GetClampedPointByObstacle(transform.position, targetPoint, currentLayer, TerrainType.UpperLayer);

            Vector2 edgePoint = upperCollider.ClosestPoint(transform.position);
            Vector2 offsetDir = ((Vector2)transform.position - edgePoint).normalized * 0.05f;
            return edgePoint + offsetDir;
        }
        else
        {
            Collider2D[] hits = new Collider2D[10];
            int hitCount = Physics2D.OverlapPoint(transform.position, filter, hits);
            
            Collider2D upperCollider = null;
            for (int i = 0; i < hitCount; i++)
            {
                TerrainArea area = hits[i].GetComponent<TerrainArea>();
                if (area != null && area.areaType == TerrainType.UpperLayer)
                {
                    upperCollider = hits[i];
                    break;
                }
            }

            if (upperCollider == null)
                return GetClampedPointByObstacle(transform.position, targetPoint, currentLayer, TerrainType.LowerLayer);

            Vector2 edgePoint = upperCollider.ClosestPoint(targetPoint);
            Vector2 offsetDir = ((Vector2)transform.position - edgePoint).normalized * 0.05f;
            return edgePoint + offsetDir;
        }
    }
    #endregion

    #region 地形与阻挡判定
    private bool IsPointInBlocked(Vector2 point)
    {
        // 阻挡优先级最高，独立判定
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = true;

        Collider2D[] hitColliders = new Collider2D[20];
        int hitCount = Physics2D.OverlapPoint(point, filter, hitColliders);

        for (int i = 0; i < hitCount; i++)
        {
            TerrainArea area = hitColliders[i].GetComponent<TerrainArea>();
            if (area != null && area.areaType == TerrainType.Blocked)
                return true;
        }
        return false;
    }

    private Vector2 GetNearestFreePointFromBlocked(Vector2 blockedPoint)
    {
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right,
            new Vector2(1,1).normalized, new Vector2(1,-1).normalized,
            new Vector2(-1,1).normalized, new Vector2(-1,-1).normalized };

        float checkDistance = 1f;
        Vector2 bestPoint = blockedPoint;
        float minDist = float.MaxValue;

        foreach (var dir in dirs)
        {
            Vector2 testPoint = blockedPoint + dir * checkDistance;
            if (!IsPointInBlocked(testPoint))
            {
                float dist = Vector2.Distance(blockedPoint, testPoint);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestPoint = testPoint;
                }
            }
        }
        return bestPoint;
    }

    private TerrainType GetCurrentStandableLayer(Vector2 point)
    {
        // 可站立层判定：阻挡点不参与层级判定
        if (IsPointInBlocked(point))
            return TerrainType.Blocked;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = true;

        Collider2D[] hitColliders = new Collider2D[20];
        int hitCount = Physics2D.OverlapPoint(point, filter, hitColliders);
        
        bool hasUpper = false;

        for (int i = 0; i < hitCount; i++)
        {
            TerrainArea area = hitColliders[i].GetComponent<TerrainArea>();
            if (area == null) continue;
            if (area.areaType == TerrainType.UpperLayer)
                hasUpper = true;
        }

        return hasUpper ? TerrainType.UpperLayer : TerrainType.LowerLayer;
    }

    private TerrainType GetCurrentStandableLayer()
    {
        return GetCurrentStandableLayer(transform.position);
    }

    private Vector2 GetClampedPointByObstacle(Vector2 startPos, Vector2 endPos, TerrainType currentLayer, TerrainType targetLayer)
    {
        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;
        if (distance < 0.001f) return startPos;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.AllLayers);
        filter.useTriggers = true;

        RaycastHit2D[] hits = new RaycastHit2D[20];
        int hitCount = Physics2D.Raycast(startPos, direction.normalized, filter, hits, distance);
        
        float nearestBlockDistance = distance;

        // 优先级：阻挡 > 上层投影
        for (int i = 0; i < hitCount; i++)
        {
            TerrainArea area = hits[i].collider.GetComponent<TerrainArea>();
            if (area == null) continue;

            // 第一优先级：纯阻挡，任何情况都不可通行
            if (area.areaType == TerrainType.Blocked)
            {
                if (hits[i].distance < nearestBlockDistance)
                    nearestBlockDistance = hits[i].distance;
                continue;
            }

            // 第二优先级：下层移动时，上层区域投影不可经过
            if (targetLayer == TerrainType.LowerLayer && area.areaType == TerrainType.UpperLayer)
            {
                if (hits[i].distance < nearestBlockDistance)
                    nearestBlockDistance = hits[i].distance;
            }
        }

        if (nearestBlockDistance >= distance)
            return endPos;
        
        Vector2 hitPoint = startPos + direction.normalized * nearestBlockDistance;
        return hitPoint - direction.normalized * 0.05f;
    }
    #endregion

    #region 跳跃核心逻辑
    /// <summary>
    /// 跳跃协程：位移+循环跳跃动画同步，落地后切回待机
    /// </summary>
    private IEnumerator JumpCoroutine(Vector2 targetPos, Vector2 jumpDir)
    {
        _currentState = CatState.Jumping;
        Vector2 startPos = transform.position;

        // 播放跳跃音效
        if (_audioSource != null && jumpAudio != null)
        {
            _audioSource.PlayOneShot(jumpAudio);
        }

        // 播放循环跳跃动画
        _animController?.PlayJump(jumpDir);

        // 抛物线位移
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

        // 位移结束，停止跳跃动画，切回待机
        _animController?.PlayIdle();

        _currentState = CatState.Idle;
    }
    #endregion

    #region 标记物品吞吐逻辑
    public void CarryMarkItem(InteractableItemBase markItem, Vector2 originalPos)
    {
        _carriedMarkItem = markItem;
        _markItemOriginalPos = originalPos;

        // 标记物品不播自身动画，直接隐藏
        markItem.gameObject.SetActive(false);

        // 播放猫咪吞咽动画，结束后解锁
        IsInInteractLock = true;
        _currentState = CatState.Interacting;
        _animController?.PlaySwallow(EndInteract);
    }

    /// <summary>
    /// 右键吐出：标记物品放回原位
    /// </summary>
    private IEnumerator SpitMarkCoroutine()
    {
        IsInInteractLock = true;
        _currentState = CatState.Interacting;

        // 标记物品直接在原位显示，不播出现动画
        _carriedMarkItem.gameObject.SetActive(true);
        _carriedMarkItem.transform.position = _markItemOriginalPos;

        bool animFinished = false;
        _animController?.PlaySpit(() => animFinished = true);
        yield return new WaitUntil(() => animFinished);

        _carriedMarkItem = null;
        EndInteract();
    }

    /// <summary>
    /// 交互消耗标记：不放回物品，直接清空
    /// </summary>
    private IEnumerator ConsumeMarkCoroutine(System.Action onComplete)
    {
        bool animFinished = false;
        _animController?.PlaySpit(() => animFinished = true);
        yield return new WaitUntil(() => animFinished);

        _carriedMarkItem = null;
        onComplete?.Invoke();
    }
    #endregion
}
