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

    [Header("标记交互配置")]
    [Tooltip("仅当交互类型包含「需标记」时生效，指定交互所需的对应标记物品")]
    public ItemDataSO requiredMarkItem;

    [Header("动画配置")]
    public AnimationClip catInteractAnim;
    public AnimationClip itemAnim;
    public AnimationClip endingFeedbackAnim;
}