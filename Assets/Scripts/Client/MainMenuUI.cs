using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MainMenuUI : MonoBehaviour
{
    // 绑定给你的“创建房间”按钮的 OnClick 事件
    public void HostGame()
    {
        NetworkManager.singleton.StartHost();
    }

    // 绑定给你的“加入房间”按钮的 OnClick 事件
    public void JoinGame()
    {
        // 在实际游戏中，这里可以读取玩家在 InputField 输入的 IP 地址
        // NetworkManager.singleton.networkAddress = "192.168.1.100";
        NetworkManager.singleton.StartClient();
    }
}

/*
using Mirror;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI 面板引用")]
    public GameObject panelMainMenu; // 引用主菜单面板
    public GameObject panelJoinGame; // 引用加入游戏(二级)面板

    // ----------------------------------------------------
    // 面板切换逻辑
    // ----------------------------------------------------

    // 点击主菜单的“加入战区”按钮时调用
    public void OpenJoinGamePanel()
    {
        panelMainMenu.SetActive(false); // 关掉主菜单
        panelJoinGame.SetActive(true);  // 打开二级菜单
    }

    // 点击二级菜单的“返回”按钮时调用
    public void BackToMainMenu()
    {
        panelJoinGame.SetActive(false); // 关掉二级菜单
        panelMainMenu.SetActive(true);  // 重新打开主菜单
    }

    // ----------------------------------------------------
    // 网络联机逻辑 (保持不变)
    // ----------------------------------------------------

    public void HostGame()
    {
        NetworkManager.singleton.StartHost();
    }

    public void JoinGame()
    {
        // 未来可以在这里读取 InputField 里的 IP 地址
        NetworkManager.singleton.StartClient();
    }
}
*/