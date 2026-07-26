using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("按钮点击音效")]
    public AudioClip clickAudio;

    [SerializeField] private Button startBtn;
    [SerializeField] private Button exitBtn;

    private AudioSource _audioSource;

    private void Awake()
    {
        // 自动获取/添加音频组件，无需手动挂载
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        startBtn.onClick.AddListener(OnStartClicked);
        exitBtn.onClick.AddListener(OnExitClicked);
    }

    private void OnStartClicked()
    {
        PlayClickSound();
        GameManager.Instance.StartGame();
    }

    private void OnExitClicked()
    {
        PlayClickSound();
        GameManager.Instance.ExitGame();
    }

    private void OnDestroy()
    {
        startBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 播放按钮点击音效，空引用自动兜底不报错
    /// </summary>
    private void PlayClickSound()
    {
        if (_audioSource != null && clickAudio != null)
        {
            _audioSource.PlayOneShot(clickAudio);
        }
    }
}
