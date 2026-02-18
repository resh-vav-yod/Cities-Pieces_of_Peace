using UnityEngine;

public class EarthCamera : MonoBehaviour
{
    public Transform target; // 拖入 Sphere
    
    [Header("Settings")]
    public float rotateSpeed = 5.0f; // 鼠标灵敏度
    public float zoomSpeed = 5.0f;   // 滚轮灵敏度
    public float minDistance = 12.0f; // 最近能拉多近 (根据你的球体大小10调整)
    public float maxDistance = 30.0f; // 最远能拉多远

    private float currentDistance;
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    void Start()
    {
        if (target)
        {
            // 初始化距离
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
            currentDistance = Vector3.Distance(transform.position, target.position);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 1. 鼠标右键旋转 (控制角度)
        if (Input.GetMouseButton(1)) 
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            // 限制垂直角度，防止翻跟头
            pitch = Mathf.Clamp(pitch, -85f, 85f);
        }

        // 2. 鼠标滚轮缩放 (控制距离)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentDistance -= scroll * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // 3. 计算最终位置 (球坐标 -> 笛卡尔坐标)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -currentDistance);
        Vector3 position = rotation * negDistance + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}