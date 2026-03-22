using UnityEngine;

public class OrbitalCameraController : MonoBehaviour
{
    [Header("天体物理参数")]
    public float earthRadius = 500f;

    [Header("摄像机控制 (挂在 CameraPivot 上)")]
    public Transform cameraTransform; // 必须把 Main Camera 拖进这里
    public float mouseOrbitSpeed = 5f;
    public float keyboardOrbitSpeed = 100f; // WASD 的移动速度
    public float zoomSpeed = 800f; // 滚轮缩放速度

    [Header("轨道极值锁定 (根据公式计算)")]
    public float minSurfaceDistance = 150f;  // 0.3R 极限近地距离
    public float maxSurfaceDistance = 2800f; // 5.6R 同步轨道距离

    [Header("恒星系统 (24小时模拟)")]
    public Transform sunLight; // 必须把 Directional Light 拖进这里
    public float sunDegreesPerSecond = 5f; // 太阳自转速度 (度/秒)

    private float currentDistance;
    private Vector2 currentRotation;
    private float currentSunAngle = 0f; // 【新增】记录太阳绝对角度

    void Start()
    {
        // 游戏开始时，让摄像机停留在中等轨道高度
        currentDistance = earthRadius + 1000f;
        
        // 记录当前的初始角度
        currentRotation.x = transform.eulerAngles.y;
        currentRotation.y = transform.eulerAngles.x;
    }

    void Update()
    {
        // ==========================================
        // 1. 恒星光照模拟 (绝对物理坐标与23.5度黄赤交角)
        // ==========================================
        if (sunLight != null)
        {
            currentSunAngle += sunDegreesPerSecond * Time.deltaTime;
            // 抛弃容易出错的 Rotate()，使用绝对欧拉角！
            // 23.5f 是真实的地球自转轴倾角，这会让你的晨昏线拥有真实的春夏秋冬变化！
            sunLight.rotation = Quaternion.Euler(23.5f, currentSunAngle, 0f);
        }


        // ==========================================
        // 2. 轨道平移 (WASD 或 鼠标右键)
        // ==========================================
        float rotX = 0f;
        float rotY = 0f;

        // 【操作逻辑 A】：鼠标右键是“抓住地球拖拽”
        if (Input.GetMouseButton(1))
        {
            rotX += Input.GetAxis("Mouse X") * mouseOrbitSpeed;
            rotY -= Input.GetAxis("Mouse Y") * mouseOrbitSpeed;
        }
        
        // 【操作逻辑 B】：WASD 是“摄像机空间平移”，必须和鼠标反过来！
        rotX -= Input.GetAxis("Horizontal") * keyboardOrbitSpeed * Time.deltaTime;
        rotY += Input.GetAxis("Vertical") * keyboardOrbitSpeed * Time.deltaTime;

        if (rotX != 0f || rotY != 0f)
        {
            // 统一合并增量
            currentRotation.x += rotX;
            currentRotation.y += rotY;
            
            // 锁死南北极视角 (-89 到 89度)，防止万向节死锁和眩晕
            currentRotation.y = Mathf.Clamp(currentRotation.y, -89f, 89f);
            
            // 【就是漏了这一句！】将计算好的欧拉角真正应用到 CameraPivot 上
            transform.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
        }
        // ==========================================
        // 3. 轨道高度缩放 (滚轮锁定在极值内)
        // ==========================================
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentDistance -= scroll * zoomSpeed;
            
            // 计算绝对距离极值 = 地球半径 + 轨道高度
            float absoluteMin = earthRadius + minSurfaceDistance;
            float absoluteMax = earthRadius + maxSurfaceDistance;
            
            // 将摄像机距离强行锁死在这个区间内
            currentDistance = Mathf.Clamp(currentDistance, absoluteMin, absoluteMax);
        }

        // 应用推演出的距离到真正的摄像机本体上
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = new Vector3(0, 0, -currentDistance);
        }
    }
}