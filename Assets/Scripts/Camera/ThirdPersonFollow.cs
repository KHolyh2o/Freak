using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [Header("연결")]
    public Transform cameraTransform; // 자식 카메라 (T_Camera)
    public Transform target;          // 타겟 (Player_Root)

    [Header("설정")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public float fixedXRotation = 20.0f;

    void LateUpdate()
    {
        if (target == null || cameraTransform == null) return;

        // 1. 위치 이동
        Vector3 desiredPos = target.position + (target.rotation * offset);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPos, smoothSpeed);

        // 2. 회전 (X축 고정, Y축 타겟 추적)
        Quaternion targetRot = Quaternion.Euler(fixedXRotation, target.eulerAngles.y, 0);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, targetRot, smoothSpeed);
    }
}