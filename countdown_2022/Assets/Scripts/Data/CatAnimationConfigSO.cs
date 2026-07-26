using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CatAnimConfig", menuName = "Game/Cat Animation Config")]
public class CatAnimationConfigSO : ScriptableObject
{
    [Header("普通状态（空嘴）动画")]
    public AnimationClip normalIdleUp;
    public AnimationClip normalIdleDown;
    public AnimationClip normalIdleLeft;
    public AnimationClip normalIdleRight;
    public AnimationClip normalWalkUp;
    public AnimationClip normalWalkDown;
    public AnimationClip normalWalkLeft;
    public AnimationClip normalWalkRight;

    [Header("标记状态（含物品）动画")]
    public AnimationClip markedIdleUp;
    public AnimationClip markedIdleDown;
    public AnimationClip markedIdleLeft;
    public AnimationClip markedIdleRight;
    public AnimationClip markedWalkUp;
    public AnimationClip markedWalkDown;
    public AnimationClip markedWalkLeft;
    public AnimationClip markedWalkRight;

    [Header("特殊动作动画（左右原生素材）")]
    public AnimationClip jumpLeftAnim;
    public AnimationClip jumpRightAnim;
    public AnimationClip swallowLeftAnim;
    public AnimationClip swallowRightAnim;
    public AnimationClip spitLeftAnim;
    public AnimationClip spitRightAnim;
}
