using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    [Header("移动与旋转参数")]
    public float moveSpeed = 30f;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 500f;

    [Header("高度限制 (防止穿地)")]
    public float minY = 5f;
    public float maxY = 50f;

    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    void Start()
    {
        // 记录初始的出生位置和角度，留给 H 键复位使用
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        // 按 H 键回到世界中心
        if (Input.GetKeyDown(KeyCode.H))
        {
            ResetCamera();
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal"); // A/D
        float z = Input.GetAxis("Vertical");   // W/S

        // 计算相机的“水平”前方和右方（忽略 Y 轴倾斜，保证 W 键永远是贴着地面往前飞）
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        // 组合移动向量并应用
        Vector3 moveDir = (forward * z + right * x).normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // Q 键左转，E 键右转
        float qe = 0f;
        if (Input.GetKey(KeyCode.Q)) qe = 1f;
        if (Input.GetKey(KeyCode.E)) qe = -1f;

        // 围绕世界坐标的 Y 轴旋转（Space.World），防止相机自身倾斜导致翻滚
        transform.Rotate(Vector3.up, qe * rotationSpeed * Time.deltaTime, Space.World);
    }

    void HandleZoom()
    {
        // 鼠标滚轮
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 沿着相机镜头自身正前方推进/拉远
            Vector3 zoomDir = transform.forward * scroll * zoomSpeed * Time.deltaTime;
            Vector3 newPos = transform.position + zoomDir;

            // 限制最低和最高高度
            if (newPos.y >= minY && newPos.y <= maxY)
            {
                transform.position = newPos;
            }
        }
    }

    void ResetCamera()
    {
        transform.position = defaultPosition;
        transform.rotation = defaultRotation;
    }
}

/*
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
*/