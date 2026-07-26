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

    [Header("结局音效")]
    public AudioClip goodEndingAudio;
    public AudioClip badEndingAudio;

    [Header("操作锁时长")]
    public float lockDuration = 3f;

    private bool _canExit = false;
    private AudioSource _audioSource;

    private void Awake()
    {
        // 自动获取/添加音频组件，无需手动挂载
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        // 初始隐藏所有结局画面与提示文字
        goodEndingSprite.gameObject.SetActive(false);
        badEndingSprite.gameObject.SetActive(false);
        if (returnHintText != null)
            returnHintText.gameObject.SetActive(false);

        // 判定结局并显示对应画面、播放对应音效
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
    /// 根据当前积分判定结局，显示对应画面、播放对应音效
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
            PlayEndingAudio(goodEndingAudio);
        }
        else
        {
            badEndingSprite.gameObject.SetActive(true);
            PlayEndingAudio(badEndingAudio);
        }
    }

    /// <summary>
    /// 播放结局音效，空引用自动兜底
    /// </summary>
    private void PlayEndingAudio(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
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
