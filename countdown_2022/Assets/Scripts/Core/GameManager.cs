using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("全局配置引用")]
    public GameConfigSO gameConfig;
    
    public GameState CurrentState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CurrentState = GameState.MainMenu;
    }

    // 开始游戏 - 主界面Start按钮调用
    public void StartGame()
    {
        GlobalDataManager.Instance.ClearAllGameData();
        CurrentState = GameState.Playing;
        SceneManager.LoadScene("GameplayScene");
    }

    // 进入结局 - 倒计时结束时调用
    public void GoToEndingScene()
    {
        CurrentState = GameState.Ending;
        SceneManager.LoadScene("EndingScene");
    }

    // 返回主界面 - 结局演出结束后调用
    public void BackToMainMenu()
    {
        GlobalDataManager.Instance.ClearAllGameData();
        CurrentState = GameState.MainMenu;
        SceneManager.LoadScene("MainScene");
    }

    // 退出游戏 - 主界面Exit按钮调用
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
