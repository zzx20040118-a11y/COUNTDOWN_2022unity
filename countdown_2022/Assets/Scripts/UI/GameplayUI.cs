using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 玩法场景UI管理器
/// 左上角：倒计时显示
/// 右上角：动态显示当前携带的标记物品图标
/// </summary>
public class GameplayUI : MonoBehaviour
{
    [Header("左上角 - 倒计时")]
    public TextMeshProUGUI timeText;

    [Header("右上角 - 携带标记图标")]
    public Image carryMarkIconImage;

    private void Awake()
    {
        // 初始隐藏图标
        if (carryMarkIconImage != null)
            carryMarkIconImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 刷新倒计时
        if (TimeManager.Instance != null && timeText != null)
        {
            timeText.text = TimeManager.Instance.GetFormattedRemainTime();
        }

        // 刷新携带标记图标
        RefreshCarryMarkIcon();
    }

    /// <summary>
    /// 根据当前携带的标记物品动态更新图标
    /// </summary>
    private void RefreshCarryMarkIcon()
    {
        if (carryMarkIconImage == null) return;
        if (CatController.Instance == null)
        {
            carryMarkIconImage.gameObject.SetActive(false);
            return;
        }

        bool isCarrying = CatController.Instance.IsCarryingMarkItem();
        carryMarkIconImage.gameObject.SetActive(isCarrying);

        if (isCarrying)
        {
            ItemDataSO markData = CatController.Instance.GetCarriedMarkItemData();
            if (markData != null && markData.itemIcon != null)
            {
                carryMarkIconImage.sprite = markData.itemIcon;
            }
        }
    }
}