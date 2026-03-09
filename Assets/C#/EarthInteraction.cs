using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Mirror; // 必须引入 Mirror

public class EarthInteraction : MonoBehaviour
{
    [Header("核心数据图 (需开启 Read/Write, Point 过滤)")]
    public Texture2D colorIdMap; 

    [Header("UI 引用")]
    public GameObject popUpUI;       // 拖入你做的 Panel
    public Text regionNameText;      // 拖入 Text 文本框
    public Button enterBattleBtn;    // 拖入 "进入战场" 按钮

    // 【数据字典】将拾取到的颜色Hex码映射到具体的国家/战区
    // 这里的 "FF0000" 等需要替换为你实际测试点击时 Console 输出的真实色号
    private Dictionary<string, string> regionData = new Dictionary<string, string>()
    {
        { "FCA838", "中国战区" }, 
        { "0000FF", "北美战区" },
        { "00FF00", "欧洲战区" }
    };

    private string currentSelectedRegion = "";

    void Start()
    {
        if (popUpUI != null) popUpUI.SetActive(false); // 初始隐藏UI
    }

    void Update()
    {
        // 鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 必须打在拥有 MeshCollider 的地球上
            if (Physics.Raycast(ray, out hit))
            {
                // 1. 获取并转换 UV 为像素
                Vector2 uv = hit.textureCoord;
                int px = Mathf.FloorToInt(uv.x * colorIdMap.width);
                int py = Mathf.FloorToInt(uv.y * colorIdMap.height);
                Color clickedColor = colorIdMap.GetPixel(px, py);

                // 2. 转换为大写十六进制字符串
                string hexColor = ColorUtility.ToHtmlStringRGB(clickedColor).ToUpper();

                Debug.Log("当前点击的颜色代码是: " + hexColor);

                // 3. 查字典 O(1) 复杂度
                if (regionData.ContainsKey(hexColor))
                {
                    OpenUI(regionData[hexColor]);
                }
                else
                {
                    // 点到了未配置的颜色（比如海洋）
                    CloseUI();
                }
            }
        }
    }

    void OpenUI(string regionName)
    {
        currentSelectedRegion = regionName;
        regionNameText.text = "目标区域: " + regionName;
        popUpUI.SetActive(true);

        // 绑定按钮事件（先清空防止重复绑定）
        enterBattleBtn.onClick.RemoveAllListeners();
        enterBattleBtn.onClick.AddListener(EnterBattleScene);
    }

    void CloseUI()
    {
        popUpUI.SetActive(false);
        currentSelectedRegion = "";
    }

    // ==========================================
    // 🌐 Mirror 网络同步：场景切换核心逻辑
    // ==========================================
    void EnterBattleScene()
    {
        if (string.IsNullOrEmpty(currentSelectedRegion)) return;

        Debug.Log("正在下达战斗指令，目标: " + currentSelectedRegion);

        // ⚠️ 联机游戏铁律：只有 Server (服务器/房主) 才有权切换场景
        // 如果让 Client 自己用 SceneManager.LoadScene，会导致客户端瞬间与服务器断开连接！
        if (NetworkServer.active) 
        {
            // ServerChangeScene 会强制把当前房间里的所有客户端一起拉进新场景
            NetworkManager.singleton.ServerChangeScene("RTS_Battle");
        }
        else
        {
            Debug.LogWarning("你只是客户端 (Client)，无权发起战争！只有房主可以。");
            
            // 进阶提示：如果要允许客户端发起战争，你需要在这里调用一个 [Command] 函数
            // 告诉服务器：“我请求打仗”，然后服务器验证通过后再执行 ServerChangeScene。
        }
    }
}