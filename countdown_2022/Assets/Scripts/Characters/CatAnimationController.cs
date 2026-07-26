using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 猫咪动画控制器
/// 统一管理待机、行走、跳跃、吞咽、吐出、物品交互动画
/// 自动适配全局动画缩放配置
/// </summary>
[RequireComponent(typeof(Animator))]
public class CatAnimationController : MonoBehaviour
{
    [Header("基础动画片段")]
    public AnimationClip idleAnim;
    public AnimationClip moveAnim;
    public AnimationClip jumpAnim;
    public AnimationClip swallowAnim;
    public AnimationClip spitAnim;

    private Animator _animator;
    private float _globalSpeedScale = 1f;

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

    /// <summary>
    /// 切换到待机动画
    /// </summary>
    public void PlayIdle()
    {
        if (_animator == null || idleAnim == null) return;
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnim.name)) return;
        _animator.Play(idleAnim.name);
    }

    /// <summary>
    /// 切换到行走动画
    /// </summary>
    public void PlayMove()
    {
        if (_animator == null || moveAnim == null) return;
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName(moveAnim.name)) return;
        _animator.Play(moveAnim.name);
    }

    /// <summary>
    /// 播放跳跃动画，与跳跃抛物线同步
    /// </summary>
    public void PlayJump()
    {
        if (_animator == null || jumpAnim == null) return;
        _animator.Play(jumpAnim.name);
    }

    /// <summary>
    /// 播放吞咽动画，结束后执行回调
    /// </summary>
    public void PlaySwallow(System.Action onComplete)
    {
        StartCoroutine(PlayAnimCoroutine(swallowAnim, onComplete));
    }

    /// <summary>
    /// 播放吐出动画，结束后执行回调
    /// </summary>
    public void PlaySpit(System.Action onComplete)
    {
        StartCoroutine(PlayAnimCoroutine(spitAnim, onComplete));
    }

    /// <summary>
    /// 播放指定物品交互动画，结束后执行回调
    /// </summary>
    public void PlayItemInteract(AnimationClip animClip, System.Action onComplete)
    {
        StartCoroutine(PlayAnimCoroutine(animClip, onComplete));
    }

    /// <summary>
    /// 通用动画播放协程，精准控制时长
    /// </summary>
    private IEnumerator PlayAnimCoroutine(AnimationClip clip, System.Action onComplete)
    {
        if (_animator == null || clip == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        _animator.Play(clip.name);
        // 按全局缩放计算实际播放时长
        float duration = clip.length / _globalSpeedScale;
        yield return new WaitForSeconds(duration);

        onComplete?.Invoke();
        PlayIdle();
    }
}