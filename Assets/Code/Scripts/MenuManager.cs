using UnityEngine;
using Mirror;
#if UNITY_EDITOR // 只有在编辑器模式下才编译这段
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
    [Header("UI 页面引用")]
    [Tooltip("包含所有主菜单按钮（Start, About, Quit）的父物体")]
    public GameObject mainMenuPage; 
    
    [Tooltip("刚才你隐藏的那个 AboutPanel")]
    public GameObject aboutPage; 

    private void Start()
    {
        // 游戏启动时，确保主菜单显示，关于页面隐藏
        ShowMainMenuPage();
    }

    // --- 被 UI 按钮调用的函数 ---

    // 1. 进入游戏（作为房主启动并切换场景）
    public void ClickedStartGame()
    {
        if (NetworkManager.singleton != null && !NetworkClient.active)
        {
            NetworkManager.singleton.StartHost(); 
        }
    }

    // 2. 从主菜单 -> 简介页面
    public void ClickedShowAbout()
    {
        if (mainMenuPage != null) mainMenuPage.SetActive(false); // 隐藏主菜单
        if (aboutPage != null) aboutPage.SetActive(true);      // 显示简介
    }

    // 3. 从简介页面 -> 返回主菜单（回答你的第二个问题）
    public void ClickedBackToMainMenu()
    {
        ShowMainMenuPage(); // 调下面的辅助函数
    }

    // 4. 关闭程序
    public void ClickedQuitGame()
    {
        #if UNITY_EDITOR
            EditorApplication.isPlaying = false; // 在编辑器里点击，停止运行
        #else
            Application.Quit(); // 打包后点击，真正关闭程序
        #endif
        Debug.Log("尝试退出游戏"); 
    }

    // --- 辅助函数，封装统一逻辑 ---
    private void ShowMainMenuPage()
    {
        if (mainMenuPage != null) mainMenuPage.SetActive(true);
        if (aboutPage != null) aboutPage.SetActive(false);
    }
}