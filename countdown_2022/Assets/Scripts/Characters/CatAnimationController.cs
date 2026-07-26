using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 猫咪动画控制器
/// 行走/待机：四方向原生动画
/// 跳跃/吞咽/吐出：左右原生动画，按方向自动匹配
/// 全部动画资源从 CatAnimationConfigSO 统一读取
/// </summary>
[RequireComponent(typeof(Animator))]
public class CatAnimationController : MonoBehaviour
{
    [Header("动画配置资产")]
    public CatAnimationConfigSO animConfig;

    private Animator _animator;
    private float _globalSpeedScale = 1f;
    private Vector2 _lastMoveDir = Vector2.down; // 默认朝向下方

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        // 读取全局动画缩放配置
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
        {
            _globalSpeedScale = GameManager.Instance.gameConfig.catAnimationGlobalScale;
        }
        _animator.speed = _globalSpeedScale;
    }

    #region 基础动画接口
    /// <summary>
    /// 播放对应状态、对应朝向的待机动画
    /// </summary>
    public void PlayIdle()
    {
        if (animConfig == null) return;
        AnimationClip clip = GetIdleClipByStateAndDir();
        if (clip == null) return;
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name)) return;
        _animator.Play(clip.name);
    }

    /// <summary>
    /// 播放对应状态、对应朝向的行走动画
    /// </summary>
    public void PlayMove(Vector2 moveDir)
    {
        if (animConfig == null) return;
        if (moveDir.magnitude > 0.01f)
            _lastMoveDir = moveDir.normalized;
        
        AnimationClip clip = GetWalkClipByStateAndDir();
        if (clip == null) return;
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName(clip.name)) return;
        _animator.Play(clip.name);
    }
    #endregion

    #region 特殊动作接口（原生左右动画）
    /// <summary>
    /// 播放吞咽动画，按当前朝向自动匹配左右
    /// </summary>
    public void PlaySwallow(System.Action onComplete)
    {
        AnimationClip clip = GetSpecialAnimByDir(animConfig.swallowLeftAnim, animConfig.swallowRightAnim, _lastMoveDir);
        StartCoroutine(PlayAnimCoroutine(clip, onComplete));
    }

    /// <summary>
    /// 播放吐出动画，按当前朝向自动匹配左右
    /// </summary>
    public void PlaySpit(System.Action onComplete)
    {
        AnimationClip clip = GetSpecialAnimByDir(animConfig.spitLeftAnim, animConfig.spitRightAnim, _lastMoveDir);
        StartCoroutine(PlayAnimCoroutine(clip, onComplete));
    }

    /// <summary>
    /// 播放物品交互动画
    /// </summary>
    public void PlayItemInteract(AnimationClip animClip, System.Action onComplete)
    {
        StartCoroutine(PlayAnimCoroutine(animClip, onComplete));
    }

    /// <summary>
    /// 播放跳跃动画，循环播放直到外部手动停止
    /// </summary>
    public void PlayJump(Vector2 jumpDir)
    {
        if (animConfig == null) return;

        AnimationClip clip = GetSpecialAnimByDir(animConfig.jumpLeftAnim, animConfig.jumpRightAnim, jumpDir);
        if (clip == null) return;

        _animator.Play(clip.name);
    }
    #endregion

    #region 内部工具：方向匹配动画
    /// <summary>
    /// 通用：根据方向从左右动画中选对应片段
    /// 水平方向为主时匹配左右；上下方向为主时默认使用左向素材
    /// </summary>
    private AnimationClip GetSpecialAnimByDir(AnimationClip leftClip, AnimationClip rightClip, Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > 0.01f)
        {
            return dir.x > 0 ? rightClip : leftClip;
        }
        // 上下方向默认返回左向素材，可按需调整
        return leftClip;
    }

    /// <summary>
    /// 根据当前携带状态 + 最后朝向，取对应待机动画
    /// </summary>
    private AnimationClip GetIdleClipByStateAndDir()
    {
        bool isMarked = CatController.Instance != null && CatController.Instance.IsCarryingMarkItem();
        FaceDir dir = GetFaceDir(_lastMoveDir);

        if (isMarked)
        {
            return dir switch
            {
                FaceDir.Up => animConfig.markedIdleUp,
                FaceDir.Down => animConfig.markedIdleDown,
                FaceDir.Left => animConfig.markedIdleLeft,
                FaceDir.Right => animConfig.markedIdleRight,
                _ => animConfig.markedIdleDown
            };
        }
        else
        {
            return dir switch
            {
                FaceDir.Up => animConfig.normalIdleUp,
                FaceDir.Down => animConfig.normalIdleDown,
                FaceDir.Left => animConfig.normalIdleLeft,
                FaceDir.Right => animConfig.normalIdleRight,
                _ => animConfig.normalIdleDown
            };
        }
    }

    /// <summary>
    /// 根据当前携带状态 + 移动朝向，取对应行走动画
    /// </summary>
    private AnimationClip GetWalkClipByStateAndDir()
    {
        bool isMarked = CatController.Instance != null && CatController.Instance.IsCarryingMarkItem();
        FaceDir dir = GetFaceDir(_lastMoveDir);

        if (isMarked)
        {
            return dir switch
            {
                FaceDir.Up => animConfig.markedWalkUp,
                FaceDir.Down => animConfig.markedWalkDown,
                FaceDir.Left => animConfig.markedWalkLeft,
                FaceDir.Right => animConfig.markedWalkRight,
                _ => animConfig.markedWalkDown
            };
        }
        else
        {
            return dir switch
            {
                FaceDir.Up => animConfig.normalWalkUp,
                FaceDir.Down => animConfig.normalWalkDown,
                FaceDir.Left => animConfig.normalWalkLeft,
                FaceDir.Right => animConfig.normalWalkRight,
                _ => animConfig.normalWalkDown
            };
        }
    }

    private enum FaceDir { Up, Down, Left, Right }
    private FaceDir GetFaceDir(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? FaceDir.Right : FaceDir.Left;
        }
        else
        {
            return dir.y > 0 ? FaceDir.Up : FaceDir.Down;
        }
    }
    #endregion

    #region 通用动画播放协程
    /// <summary>
    /// 通用单次动画播放协程，结束自动切回待机
    /// </summary>
    private IEnumerator PlayAnimCoroutine(AnimationClip clip, System.Action onComplete)
    {
        if (_animator == null || clip == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        _animator.Play(clip.name);
        float duration = clip.length / _globalSpeedScale;
        yield return new WaitForSeconds(duration);

        onComplete?.Invoke();
        PlayIdle();
    }
    #endregion
}
