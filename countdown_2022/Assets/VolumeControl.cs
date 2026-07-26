using UnityEngine;
using UnityEngine.UI;

public class VolumeControl : MonoBehaviour
{
    public Button btnOn;
    public Button btnOff;
    public AudioSource audioSource;

    private CanvasGroup cgOn;
    private CanvasGroup cgOff;

    void Start()
    {
        // 获取或添加 CanvasGroup 组件
        cgOn = btnOn.GetComponent<CanvasGroup>();
        if (cgOn == null) cgOn = btnOn.gameObject.AddComponent<CanvasGroup>();
        
        cgOff = btnOff.GetComponent<CanvasGroup>();
        if (cgOff == null) cgOff = btnOff.gameObject.AddComponent<CanvasGroup>();

        // 绑定事件
        btnOn.onClick.AddListener(TurnOn);
        btnOff.onClick.AddListener(TurnOff);

        // 默认开启：ON可见，OFF透明
        TurnOn();
    }

    void TurnOn()
    {
        cgOn.alpha = 1f;   // ON 完全不透明（可见）
        cgOff.alpha = 0f;  // OFF 完全透明（看不见）
        if (!audioSource.isPlaying) audioSource.Play();
    }

    void TurnOff()
    {
        cgOn.alpha = 0f;   // ON 透明
        cgOff.alpha = 1f;  // OFF 可见
        audioSource.Stop();
    }
}