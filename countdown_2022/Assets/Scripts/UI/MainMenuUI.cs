using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startBtn;
    [SerializeField] private Button exitBtn;

    private void Awake()
    {
        startBtn.onClick.AddListener(OnStartClicked);
        exitBtn.onClick.AddListener(OnExitClicked);
    }

    private void OnStartClicked()
    {
        GameManager.Instance.StartGame();
    }

    private void OnExitClicked()
    {
        GameManager.Instance.ExitGame();
    }

    private void OnDestroy()
    {
        startBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();
    }
}

