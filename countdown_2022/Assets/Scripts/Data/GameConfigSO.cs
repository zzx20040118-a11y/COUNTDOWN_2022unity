using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGameConfig", menuName = "Game/Game Config")]
public class GameConfigSO : ScriptableObject
{
    [Header("基础规则")]
    [Tooltip("单局游戏总时长，单位：秒")]
    public float gameDuration = 60f;

    [Header("音效配置")]
    public AudioClip timeUpAudio;

    [Header("全局动画速度")]
    [Tooltip("猫咪动画全局播放倍率")]
    public float catAnimationGlobalScale = 1f;

    [Tooltip("所有物品动画全局播放倍率")]
    public float itemAnimationGlobalScale = 1f;
}