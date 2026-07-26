using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndingSceneUI : MonoBehaviour
{
    [Header("结局画面（SpriteRenderer）")]
    public SpriteRenderer goodEndingSprite;
    public SpriteRenderer badEndingSprite;

    [Header("返回提示文字")]
    public TMP_Text returnHintText;

    [Header("操作锁时长")]
    public float lockDuration = 3f;

    private bool _canExit = false;

    private void Start()
    {
        // 初始隐藏所有结局画面与提示文字
        goodEndingSprite.gameObject.SetActive(false);
        badEndingSprite.gameObject.SetActive(false);
        if (returnHintText != null)
            returnHintText.gameObject.SetActive(false);

        // 判定结局并显示对应画面
        ShowEndingByScore();

        // 启动操作锁协程
        StartCoroutine(ExitLockCoroutine());
    }

    private void Update()
    {
        // 解锁后，鼠标任意键返回主菜单
        if (_canExit)
        {
            if (Input.GetMouseButtonDown(0) || 
                Input.GetMouseButtonDown(1) || 
                Input.GetMouseButtonDown(2))
            {
                GameManager.Instance.BackToMainMenu();
            }
        }
    }

    /// <summary>
    /// 根据当前积分判定结局，显示对应画面
    /// </summary>
    private void ShowEndingByScore()
    {
        int score = GlobalDataManager.Instance.GetCurrentScore();
        int threshold = 0;

        // 读取配置阈值，配置缺失时兜底
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
        {
            threshold = GameManager.Instance.gameConfig.goodEndingScoreThreshold;
        }

        if (score >= threshold)
        {
            goodEndingSprite.gameObject.SetActive(true);
        }
        else
        {
            badEndingSprite.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 操作锁协程：前3秒禁止跳转，结束后显示提示文字
    /// </summary>
    private IEnumerator ExitLockCoroutine()
    {
        _canExit = false;
        yield return new WaitForSeconds(lockDuration);

        _canExit = true;
        // 解锁后显示返回提示
        if (returnHintText != null)
            returnHintText.gameObject.SetActive(true);
    }
}
