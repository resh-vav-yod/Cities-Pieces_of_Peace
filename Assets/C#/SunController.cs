using UnityEngine;

public class SunController : MonoBehaviour
{
    public Transform target; // 地球
    public float speed = 10.0f;
    
    [Header("Orbit Settings")]
    // 关键点：控制旋转轴。
    // (0, 1, 0) 是赤道旋转。
    // (0.4, 1, 0) 就会产生大约 23.5 度的倾角（模拟真实的地球公转轨道）
    public Vector3 axis = new Vector3(0.4f, 1f, 0f); 

    void Update()
    {
        if (target != null)
        {
            // 使用自定义的 axis 进行旋转
            transform.RotateAround(target.position, axis.normalized, speed * Time.deltaTime);
            transform.LookAt(target);
        }
    }
}