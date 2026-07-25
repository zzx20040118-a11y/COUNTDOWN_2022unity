using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("基础信息")]
    public int itemId;
    public ItemCategory category;
    public InteractableType[] interactTypes;

    [Header("动画配置")]
    public AnimationClip catInteractAnim;
    public AnimationClip itemAnim;
    public AnimationClip endingFeedbackAnim;
}