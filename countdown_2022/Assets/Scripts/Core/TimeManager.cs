using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏倒计时管理器
/// 仅在Playing状态运行，时间耗尽自动调用GameManager进入结局
/// 支持暂停、加减时间，预留UI显示接口
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("运行时调试（只读）")]
    [Tooltip("当前剩余时间，单位：秒")]
    public float remainTime;
    [Tooltip("倒计时是否暂停")]
    public bool isPaused;

    private float _totalDuration;
    private AudioSource _audioSource;
    private bool _hasTriggeredEnding;

    private void Awake()
    {
        // 场景内单例：仅当前玩法场景有效
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        _hasTriggeredEnding = false;
    }

    private void Start()
    {
        // 从全局配置读取总时长
        if (GameManager.Instance != null && GameManager.Instance.gameConfig != null)
        {
            _totalDuration = GameManager.Instance.gameConfig.gameDuration;
            remainTime = _totalDuration;
        }
        else
        {
            // 配置缺失兜底默认值
            _totalDuration = 60f;
            remainTime = _totalDuration;
        }
    }

    private void Update()
    {
        // 状态校验：仅游玩状态、未暂停、未触发过结局时执行倒计时
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        if (isPaused || _hasTriggeredEnding) return;

        remainTime -= Time.deltaTime;

        // 时间耗尽，触发结局
        if (remainTime <= 0)
        {
            remainTime = 0;
            OnTimeUp();
        }
    }

    /// <summary>
    /// 时间耗尽回调：播放结束音效，调用GameManager进入结局
    /// </summary>
    private void OnTimeUp()
    {
        _hasTriggeredEnding = true;

        // 播放时间结束音效
        if (GameManager.Instance?.gameConfig?.timeUpAudio != null)
        {
            _audioSource.PlayOneShot(GameManager.Instance.gameConfig.timeUpAudio);
        }

        // 调用GameManager公开方法进入结局，不直接修改状态
        GameManager.Instance.GoToEndingScene();
    }

    #region 对外公共接口
    /// <summary>
    /// 增加剩余时间（不超过总时长上限）
    /// </summary>
    public void AddTime(float seconds)
    {
        if (_hasTriggeredEnding) return;
        remainTime = Mathf.Min(remainTime + seconds, _totalDuration);
    }

    /// <summary>
    /// 减少剩余时间
    /// </summary>
    public void ReduceTime(float seconds)
    {
        if (_hasTriggeredEnding) return;
        remainTime = Mathf.Max(remainTime - seconds, 0);
        if (remainTime <= 0)
        {
            OnTimeUp();
        }
    }

    /// <summary>
    /// 暂停倒计时
    /// </summary>
    public void Pause()
    {
        isPaused = true;
    }

    /// <summary>
    /// 继续倒计时
    /// </summary>
    public void Resume()
    {
        isPaused = false;
    }

    /// <summary>
    /// 重置倒计时为满时长
    /// </summary>
    public void ResetTimer()
    {
        remainTime = _totalDuration;
        isPaused = false;
        _hasTriggeredEnding = false;
    }

    /// <summary>
    /// 获取格式化剩余时间（分:秒）
    /// 直接供UI文本显示调用
    /// </summary>
    public string GetFormattedRemainTime()
    {
        int minutes = Mathf.FloorToInt(remainTime / 60);
        int seconds = Mathf.FloorToInt(remainTime % 60);
        return $"{minutes:00}:{seconds:00}";
    }
    #endregion
}
