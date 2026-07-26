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

    [Header("标记物品图标")]
    [Tooltip("标记物品在右上角UI显示的图标")]
    public Sprite itemIcon;

    [Header("标记交互配置")]
    [Tooltip("仅当交互类型包含「需标记」时生效，指定交互所需的对应标记物品")]
    public ItemDataSO requiredMarkItem;

    [Header("直接交互动画")]
    [Tooltip("空嘴直接触发交互时播放的动画")]
    public AnimationClip directCatInteractAnim;
    public AnimationClip directItemAnim;

    [Header("标记交互动画")]
    [Tooltip("携带对应标记触发交互时播放的动画")]
    public AnimationClip markedCatInteractAnim;
    public AnimationClip markedItemAnim;

    [Header("结局反馈动画")]
    public AnimationClip endingFeedbackAnim;
}
