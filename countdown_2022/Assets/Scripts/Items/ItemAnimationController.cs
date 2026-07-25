using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品动画控制器
/// 管理物品自身的交互、出现、消失动画
/// </summary>
[RequireComponent(typeof(Animator))]
public class ItemAnimationController : MonoBehaviour
{
    private Animator _animator;
    private float _globalSpeedScale = 1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
        // 读取全局动画缩放配置
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
        {
            _globalSpeedScale = GameManager.Instance.gameConfig.itemAnimationGlobalScale;
        }
        _animator.speed = _globalSpeedScale;
    }

    /// <summary>
    /// 播放物品交互动画
    /// </summary>
    public void PlayInteract(AnimationClip clip)
    {
        if (_animator == null || clip == null) return;
        _animator.Play(clip.name);
    }

    /// <summary>
    /// 播放物品出现动画（吐出标记时调用）
    /// </summary>
    public void PlayAppear(AnimationClip clip)
    {
        if (_animator == null || clip == null) return;
        _animator.Play(clip.name);
    }

    /// <summary>
    /// 播放物品消失动画（吞下标记时调用）
    /// </summary>
    public void PlayDisappear(AnimationClip clip)
    {
        if (_animator == null || clip == null) return;
        _animator.Play(clip.name);
    }
}