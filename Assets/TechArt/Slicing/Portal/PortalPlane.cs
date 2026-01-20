using UnityEngine;

public class PortalPlane : MonoBehaviour
{
    [Tooltip("手动顺序，越小越先执行")]
    public int order = 0;

    [Tooltip("旋转角度（度）")]
    public float rotationAngle = 15f;

    // 屏幕空间中心（运行时算）
    [HideInInspector] public Vector2 screenPivot;
}
