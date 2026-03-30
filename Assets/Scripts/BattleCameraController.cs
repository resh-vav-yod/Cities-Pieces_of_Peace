using UnityEngine;
using Mirror;

public class BattleCameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 20f;
    public float edgeScrollSize = 20f; // 鼠标靠边移动触发距离
    public bool useEdgeScroll = true;

    [Header("范围锁定")]
    public Vector2 xLimit = new Vector2(-50, 50);
    public Vector2 zLimit = new Vector2(-50, 50);

    void Update()
    {
        // 只有在战斗场景且没有其他全屏 UI 阻挡时移动
        HandleMovement();
        
        // 按下 ESC 键返回地球场景 (调用 NetworkManager 跳转)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToEarth();
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        // 如果开启鼠标靠边移动
        if (useEdgeScroll)
        {
            if (Input.mousePosition.x >= Screen.width - edgeScrollSize) x = 1;
            if (Input.mousePosition.x <= edgeScrollSize) x = -1;
            if (Input.mousePosition.y >= Screen.height - edgeScrollSize) z = 1;
            if (Input.mousePosition.y <= edgeScrollSize) z = -1;
        }

        // 计算移动向量（Space.World 保证方向不随相机倾斜而改变）
        Vector3 move = new Vector3(x, 0, z) * moveSpeed * Time.deltaTime;
        transform.position += move;

        // 限制移动范围，防止飘出地图
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, xLimit.x, xLimit.y);
        pos.z = Mathf.Clamp(pos.z, zLimit.x, zLimit.y);
        transform.position = pos;
    }

    void ReturnToEarth()
    {
        if (NetworkServer.active)
        {
            // 注意：场景名必须与 Build Settings 中一致
            NetworkManager.singleton.ServerChangeScene("Scene_Earth"); 
        }
    }
}