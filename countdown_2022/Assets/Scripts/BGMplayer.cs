using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public AudioSource audioSource;
    private static BackgroundMusic instance;

    void Awake()
    {
        // 单例模式：保证只有一个
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 默认播放
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    // 供外部调用的方法
    public void PlayMusic()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }
}